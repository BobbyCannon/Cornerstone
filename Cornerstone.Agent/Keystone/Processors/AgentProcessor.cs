#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Logging;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using LLama;
using LLama.Common;
using LLama.Sampling;
using LLama.Transformers;

#endregion

namespace Cornerstone.Agent.Keystone.Processors;

/// <summary>
/// Owns chat session / inference. Loads weights via <see cref="ModelWeightsRuntime"/>
/// (EnsureLoaded → rebuild session if model changed → infer).
/// </summary>
[SourceReflection]
[DependencyInjected]
public partial class AgentProcessor : AppProcessor
{
	#region Constants

	private const string DefaultSystemPrompt =
		"""
		You are Cornerstone Agent, a helpful local assistant.

		Be clear and direct. Use Markdown when it helps (code fences with language tags, lists, headings).
		""";

	#endregion

	#region Fields

	private static readonly Regex StripSpecialTokensRegex;
	private LLamaWeights _cachedWeights;
	private LLamaContext _context;
	private InteractiveExecutor _executor;
	private readonly SemaphoreSlim _inferenceLock;
	private bool _isThinking;
	private ChatHistory _messageHistory;
	private readonly ModelWeightsRuntime _weights;
	private string _sessionModelPath;
	private bool _supportsNativeTemplate;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public AgentProcessor(AppBus bus, AppState state, ModelWeightsRuntime weights)
		: base(bus, state)
	{
		_weights = weights;
		_inferenceLock = new SemaphoreSlim(1, 1);
		_messageHistory = new ChatHistory();
	}

	static AgentProcessor()
	{
		StripSpecialTokensRegex = new(
			"""
			# Chat / control tokens (Qwen, Llama, Gemma, etc.)
			<\|(?:im_start|im_end|im_user|im_assistant|user|assistant|system|final|chat|human|gpt|tool|function|observation|endoftext|eot_id|start_header_id|end_header_id)[^>]*\|>|
			<\|[^>]+?\>|
			\[/?INST\]?|
			<</?SYS>>?|
			<s>|</s>|<eos>|
			<\|end_of_text\|>
			""",
			RegexOptions.IgnorePatternWhitespace
			| RegexOptions.Compiled
			| RegexOptions.IgnoreCase
			| RegexOptions.Multiline
		);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Request pipeline: load → session → user turn → infer (stream) → assistant history.
	/// Tools / multi-step tool loops are deferred.
	/// </summary>
	public async Task ProcessAsync(string prompt, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(prompt))
		{
			return;
		}

		if (!await _inferenceLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
		{
			Log("Agent is already busy.", LogLevel.Warning);
			return;
		}

		State.ModelState.IsExecuting = true;
		_isThinking = false;

		try
		{
			// --- Phase: validate selection ---
			var desiredPath = State.ModelState.SelectedModelPath;
			if (string.IsNullOrEmpty(desiredPath))
			{
				Log("No model selected. Choose a model before sending a prompt.", LogLevel.Error);
				return;
			}

			var loadedBefore = State.ModelState.LoadedModelPath;
			var sessionBefore = _sessionModelPath;

			// --- Phase: Load (request-time unload/load if needed) ---
			Log(
				string.IsNullOrEmpty(loadedBefore)
					? $"[Load] Ensuring model: {Label(desiredPath)}"
					: string.Equals(loadedBefore, desiredPath, StringComparison.OrdinalIgnoreCase)
						? $"[Load] Already desired path loaded: {Label(desiredPath)}"
						: $"[Load] Switch requested: {Label(loadedBefore)} → {Label(desiredPath)}",
				LogLevel.Information);

			var loaded = await _weights.EnsureLoadedAsync(desiredPath, cancellationToken).ConfigureAwait(false);
			if (!loaded)
			{
				Log("[Load] Failed.", LogLevel.Error);
				return;
			}

			cancellationToken.ThrowIfCancellationRequested();

			var loadedAfter = State.ModelState.LoadedModelPath;
			if (string.Equals(loadedBefore, loadedAfter, StringComparison.OrdinalIgnoreCase)
				&& !string.IsNullOrEmpty(loadedBefore))
			{
				Log($"[Load] Reused weights in memory: {Label(loadedAfter)}", LogLevel.Information);
			}
			else
			{
				Log(
					string.IsNullOrEmpty(loadedBefore)
						? $"[Load] Loaded: {Label(loadedAfter)}"
						: $"[Load] Unloaded {Label(loadedBefore)}; loaded {Label(loadedAfter)}",
					LogLevel.Information);
			}

			// --- Phase: Session (rebuild context when weights change) ---
			if (!EnsureSessionForLoadedModel(out var sessionRebound))
			{
				Log("[Session] Failed to initialize chat session for the loaded model.", LogLevel.Error);
				return;
			}

			Log(
				sessionRebound
					? $"[Session] Rebound to {Label(_sessionModelPath)} (was {Label(sessionBefore)}; history={_messageHistory.Messages.Count}; nativeTemplate={_supportsNativeTemplate})"
					: $"[Session] Reused executor on {Label(_sessionModelPath)} (history={_messageHistory.Messages.Count}; nativeTemplate={_supportsNativeTemplate})",
				LogLevel.Information);

			// --- Phase: User turn ---
			_messageHistory.AddMessage(AuthorRole.User, prompt);
			AppendUserTurnToOutput(prompt, _sessionModelPath);

			// --- Phase: Infer ---
			var assistantText = await InferAsync(cancellationToken).ConfigureAwait(false);
			if (!string.IsNullOrEmpty(assistantText))
			{
				_messageHistory.AddMessage(AuthorRole.Assistant, assistantText);
			}

			AppendTurnFooter();
			Log($"[Infer] Done. historyMessages={_messageHistory.Messages.Count}", LogLevel.Information);
		}
		catch (OperationCanceledException)
		{
			Log("[Request] Cancelled.", LogLevel.Warning);
			AppendCancelledToOutput();
		}
		catch (Exception ex)
		{
			Log($"[Request] Agent error: {ex.Message}", LogLevel.Error);
			State.ModelState.OutputIngress.Append($"{Environment.NewLine}**Error:** {ex.Message}{Environment.NewLine}{Environment.NewLine}");
		}
		finally
		{
			_isThinking = false;
			State.ModelState.IsExecuting = false;
			_inferenceLock.Release();
		}
	}

	/// <summary>
	/// Clears chat history and disposes the current context. Does not unload weights.
	/// </summary>
	public void ResetSession()
	{
		DisposeContext();
		_messageHistory = new ChatHistory();
		_sessionModelPath = null;
		_supportsNativeTemplate = false;
		Log("Chat history cleared.", LogLevel.Information);
	}

	public override void InitializeLifecycle()
	{
		_weights.Unloading += OnBeforeWeightsUnload;
		base.InitializeLifecycle();
	}

	public override void UninitializeLifecycle()
	{
		_weights.Unloading -= OnBeforeWeightsUnload;

		DisposeContext();
		_inferenceLock.Dispose();
		base.UninitializeLifecycle();
	}

	/// <summary>
	/// Rebuilds LLama context/executor when the loaded model differs from the session model.
	/// Preserves <see cref="_messageHistory"/> so prior turns feed into the new model.
	/// </summary>
	/// <param name="rebound"> True when context/executor were recreated. </param>
	internal bool EnsureSessionForLoadedModel(out bool rebound)
	{
		rebound = false;
		var weights = _weights.Weights;
		var loadedPath = State.ModelState.LoadedModelPath;

		if ((weights == null) || string.IsNullOrEmpty(loadedPath))
		{
			return false;
		}

		if (string.Equals(_sessionModelPath, loadedPath, StringComparison.OrdinalIgnoreCase)
			&& (_executor != null)
			&& ReferenceEquals(_cachedWeights, weights))
		{
			return true;
		}

		DisposeContext();

		_cachedWeights = weights;
		_context = weights.CreateContext(new ModelParams(loadedPath)
		{
			ContextSize = 32768,
			GpuLayerCount = -1,
			FlashAttention = true
		});

		_executor = _weights.LlavaWeights != null
			? new InteractiveExecutor(_context, _weights.LlavaWeights)
			: new InteractiveExecutor(_context);

		// Keep existing message history (including prior turns). Seed system only if empty.
		if (_messageHistory.Messages.Count == 0)
		{
			_messageHistory.AddMessage(AuthorRole.System, DefaultSystemPrompt);
		}

		_supportsNativeTemplate = DetectNativeTemplate(weights);
		_sessionModelPath = loadedPath;
		rebound = true;
		return true;
	}

	private void AppendCancelledToOutput()
	{
		var output = State.ModelState.OutputIngress;
		output.Append("_Request cancelled._");
		output.Append(Environment.NewLine);
		output.Append(Environment.NewLine);
	}

	private void AppendTurnFooter()
	{
		var output = State.ModelState.OutputIngress;
		output.Append(Environment.NewLine);
		output.Append(Environment.NewLine);
	}

	private void AppendUserTurnToOutput(string prompt, string modelPath)
	{
		var output = State.ModelState.OutputIngress;
		output.Append("### You");
		output.Append(Environment.NewLine);
		output.Append(Environment.NewLine);
		output.Append(prompt.Trim());
		output.Append(Environment.NewLine);
		output.Append(Environment.NewLine);
		output.Append($"_model: {Label(modelPath)}_");
		output.Append(Environment.NewLine);
		output.Append(Environment.NewLine);
		output.Append("### Assistant");
		output.Append(Environment.NewLine);
		output.Append(Environment.NewLine);
	}

	private static IReadOnlyList<string> BuildAntiPrompts()
	{
		return
		[
			"<|eot_id|>", "<|im_end|>", "<|end|>", "<|end_of_turn|>", "<eos>", "</s>",
			"[/INST]", "<end_of_turn>", "<start_of_turn>"
		];
	}

	/// <summary>
	/// Legacy User/Assistant lines when the GGUF has no usable embedded chat template.
	/// </summary>
	private string BuildLegacyPrompt()
	{
		var sb = new StringBuilder(4096);

		foreach (var msg in _messageHistory.Messages)
		{
			switch (msg.AuthorRole)
			{
				case AuthorRole.System:
					sb.AppendLine(msg.Content);
					sb.AppendLine();
					break;
				case AuthorRole.User:
					sb.AppendLine($"User: {msg.Content}");
					break;
				case AuthorRole.Assistant:
					sb.AppendLine($"Assistant: {msg.Content}");
					break;
			}
		}

		sb.AppendLine("Assistant:");
		return sb.ToString();
	}

	/// <summary>
	/// Uses the model's embedded Jinja/chat template when available (ChatML, Gemma, Llama-3, …).
	/// </summary>
	private string BuildNativeTemplatePrompt()
	{
		var template = new LLamaTemplate(_cachedWeights!, false)
		{
			AddAssistant = true
		};

		foreach (var msg in _messageHistory.Messages)
		{
			var role = msg.AuthorRole switch
			{
				AuthorRole.System => "system",
				AuthorRole.User => "user",
				AuthorRole.Assistant => "assistant",
				_ => "user"
			};

			template.Add(role, msg.Content);
		}

		return PromptTemplateTransformer.ToModelPrompt(template);
	}

	private bool DetectNativeTemplate(LLamaWeights weights)
	{
		try
		{
			var template = new LLamaTemplate(weights, false);
			template.Add("system", "You are a helpful assistant.");
			template.Add("user", "Hello");
			template.AddAssistant = true;

			var result = PromptTemplateTransformer.ToModelPrompt(template);
			var looksLikeRealTemplate =
				result.Contains("<start_of_turn>")
				|| result.Contains("<|im_start|>")
				|| result.Contains("###")
				|| (result.Length > 30);

			return looksLikeRealTemplate;
		}
		catch (Exception ex)
		{
			Log($"[Session] No native template: {ex.Message}", LogLevel.Debug);
			return false;
		}
	}

	private void DisposeContext()
	{
		_executor = null;
		_cachedWeights = null;
		try
		{
			_context?.Dispose();
		}
		catch (Exception ex)
		{
			Log($"Context dispose warning: {ex.Message}", LogLevel.Warning);
		}

		_context = null;
		// Keep _messageHistory; only clear path so next Ensure rebuilds context
		_sessionModelPath = null;
	}

	/// <summary>
	/// Streams one assistant completion for the current <see cref="_messageHistory"/> (user turn already appended).
	/// Returns cleaned assistant text for history (excludes think blocks).
	/// </summary>
	private async Task<string> InferAsync(CancellationToken cancellationToken)
	{
		if (_executor == null)
		{
			Log("[Infer] No executor.", LogLevel.Error);
			return string.Empty;
		}

		var fullPrompt = _supportsNativeTemplate
			? BuildNativeTemplatePrompt()
			: BuildLegacyPrompt();

		Log(
			$"[Infer] Starting (template={(_supportsNativeTemplate ? "native" : "legacy")}; promptChars={fullPrompt.Length})",
			LogLevel.Information);

		var inferenceParams = new InferenceParams
		{
			MaxTokens = 16384,
			AntiPrompts = BuildAntiPrompts(),
			SamplingPipeline = new DefaultSamplingPipeline
			{
				Temperature = 0.1f,
				TopP = 0.90f,
				TopK = 40,
				RepeatPenalty = 1.22f
			},
			DecodeSpecialTokens = true,
			TokensKeep = 256
		};

		var assistantClean = new StringBuilder(1024);
		var output = State.ModelState.OutputIngress;
		var reasoning = State.ModelState.ReasoningIngress;
		var raw = State.ModelState.RawIngress;
		var tokenCount = 0;

		await foreach (var partial in _executor.InferAsync(fullPrompt, inferenceParams, cancellationToken).ConfigureAwait(false))
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (string.IsNullOrEmpty(partial))
			{
				continue;
			}

			raw.Append(partial);
			tokenCount++;

			// Route model "thinking" blocks away from the main answer surface.
			if (!_isThinking && partial.Contains("<think>", StringComparison.OrdinalIgnoreCase))
			{
				_isThinking = true;
				var afterOpen = StripThinkOpen(partial);
				if (!string.IsNullOrEmpty(afterOpen))
				{
					reasoning.Append(afterOpen);
				}
				continue;
			}

			if (_isThinking)
			{
				if (partial.Contains("</think>", StringComparison.OrdinalIgnoreCase))
				{
					var beforeClose = StripThinkClose(partial);
					if (!string.IsNullOrEmpty(beforeClose))
					{
						reasoning.Append(beforeClose);
					}

					_isThinking = false;
					// Remainder after </think> may be answer text
					var afterClose = AfterThinkClose(partial);
					if (!string.IsNullOrEmpty(afterClose))
					{
						var cleanTail = CleanToken(afterClose);
						if (!string.IsNullOrEmpty(cleanTail))
						{
							output.Append(cleanTail);
							assistantClean.Append(cleanTail);
						}
					}
				}
				else
				{
					reasoning.Append(partial);
				}

				continue;
			}

			var clean = CleanToken(partial);
			if (string.IsNullOrEmpty(clean))
			{
				continue;
			}

			output.Append(clean);
			assistantClean.Append(clean);
		}

		Log($"[Infer] Stream finished ({tokenCount} partials).", LogLevel.Information);
		return assistantClean.ToString().Trim();
	}

	private static string AfterThinkClose(string partial)
	{
		var idx = partial.IndexOf("</think>", StringComparison.OrdinalIgnoreCase);
		if (idx < 0)
		{
			return string.Empty;
		}

		return partial[(idx + "</think>".Length)..];
	}

	private static string CleanToken(string token)
	{
		if (string.IsNullOrEmpty(token))
		{
			return string.Empty;
		}

		return StripSpecialTokensRegex.Replace(token, string.Empty);
	}

	private static string Label(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return "(none)";
		}

		return Path.GetFileNameWithoutExtension(path);
	}

	private void Log(string message, LogLevel level)
	{
		Bus.Logging.Log(message, level);
		State.ModelState.SystemIngress.Append(message);
		State.ModelState.SystemIngress.Append(Environment.NewLine);
	}

	/// <summary>
	/// Called immediately before native weights dispose.
	/// </summary>
	private void OnBeforeWeightsUnload()
	{
		if ((_context == null) && (_executor == null) && (_cachedWeights == null))
		{
			return;
		}

		Log($"[Session] Releasing context before weights unload (was {Label(_sessionModelPath)}).", LogLevel.Information);
		DisposeContext();
	}

	private static string StripThinkClose(string partial)
	{
		var idx = partial.IndexOf("</think>", StringComparison.OrdinalIgnoreCase);
		if (idx < 0)
		{
			return partial;
		}

		return partial[..idx];
	}

	private static string StripThinkOpen(string partial)
	{
		var idx = partial.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);
		if (idx < 0)
		{
			return partial;
		}

		return partial[(idx + "<think>".Length)..];
	}

	#endregion
}
