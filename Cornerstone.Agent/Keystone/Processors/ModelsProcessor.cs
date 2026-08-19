#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Agent.Hardware;
using Cornerstone.Agent.Keystone.Channels;
using Cornerstone.Agent.Keystone.State;
using Cornerstone.Compare;
using Cornerstone.Extensions;
using Cornerstone.Logging;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using LLama;
using LLama.Common;
using LLama.Native;
using RuntimeInformation = System.Runtime.InteropServices.RuntimeInformation;

#endregion

namespace Cornerstone.Agent.Keystone.Processors;

/// <summary>
/// Discovers GGUF models and owns load/unload of LLama weights.
/// Selection is deferred: <see cref="OnSelectModel"/> only updates desired path;
/// <see cref="EnsureLoadedAsync"/> performs unload/load at request time.
/// </summary>
[SourceReflection]
[DependencyInjected]
public partial class ModelsProcessor : AppProcessor
{
	#region Fields

	private readonly HardwareInformationService _hardware;
	private double _isRefreshing;
	private static readonly GenericEqualityComparer<ModelInfo> _modelInfoComparer;
	private readonly ModelWeightsRuntime _weights;
	private static bool _nativeLibConfigured;

	/// <summary>
	/// Serializes load/unload so concurrent Unload bus messages cannot race EnsureLoaded.
	/// </summary>
	private readonly SemaphoreSlim _weightsGate = new(1, 1);

	/// <summary>
	/// Brief pause after native dispose so CUDA/Vulkan can reclaim VRAM before the next mmap load.
	/// Not a substitute for correct dispose order — only a settle window after free.
	/// </summary>
	private static readonly TimeSpan UnloadSettle = TimeSpan.FromMilliseconds(250);

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public ModelsProcessor(AppBus bus, AppState state, ModelWeightsRuntime weights) : base(bus, state)
	{
		_hardware = new HardwareInformationService(state.RuntimeInformation);
		_weights = weights;
		_weights.SetEnsureLoaded(EnsureLoadedAsync);
	}

	static ModelsProcessor()
	{
		_modelInfoComparer = new GenericEqualityComparer<ModelInfo>(
			(x, y) => (x != null) && (y != null) && (x.FilePath == y.FilePath),
			x => x.FilePath.GetStableHashCode()
		);
	}

	#endregion

	#region Properties

	public HardwareInformationService Hardware => _hardware;

	public bool IsVisionModelLoaded => _weights.LlavaWeights is not null;

	#endregion

	#region Methods

	/// <summary>
	/// Ensures weights for <paramref name="desiredPath"/> are in memory.
	/// Unloads a different loaded model first. No-op if already loaded.
	/// </summary>
	public async Task<bool> EnsureLoadedAsync(string desiredPath, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(desiredPath))
		{
			Log("No model selected.", LogLevel.Error);
			return false;
		}

		if (!File.Exists(desiredPath))
		{
			Log($"Model file not found: {desiredPath}", LogLevel.Error);
			return false;
		}

		if (string.Equals(State.ModelState.LoadedModelPath, desiredPath, StringComparison.OrdinalIgnoreCase)
			&& (_weights.Weights != null))
		{
			return true;
		}

		await _weightsGate.WaitAsync(cancellationToken).ConfigureAwait(false);
		State.ModelState.IsModelLoading = true;
		State.ModelState.LoadingPercent = 0;

		var hadPrevious = false;
		try
		{
			// Re-check under the gate (another waiter may have loaded us).
			if (string.Equals(State.ModelState.LoadedModelPath, desiredPath, StringComparison.OrdinalIgnoreCase)
				&& (_weights.Weights != null))
			{
				return true;
			}

			cancellationToken.ThrowIfCancellationRequested();

			if (_weights.Weights != null)
			{
				hadPrevious = true;
				Log($"Unloading previous model: {State.ModelState.LoadedModelPath}", LogLevel.Information);
				await UnloadModelCoreAsync(settle: true, cancellationToken).ConfigureAwait(false);
			}

			cancellationToken.ThrowIfCancellationRequested();

			return await LoadWeightsCoreAsync(desiredPath, cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			Log("Model load cancelled.", LogLevel.Warning);
			await UnloadModelCoreAsync(settle: false, CancellationToken.None).ConfigureAwait(false);
			return false;
		}
		catch (Exception ex)
		{
			// After a switch, first load attempt can fail while the driver still holds VRAM.
			if (hadPrevious)
			{
				Log($"Load after switch failed ({ex.Message}); settling and retrying once…", LogLevel.Warning);
				await SettleAfterUnloadAsync(cancellationToken).ConfigureAwait(false);
				try
				{
					return await LoadWeightsCoreAsync(desiredPath, cancellationToken).ConfigureAwait(false);
				}
				catch (Exception retryEx)
				{
					Log($"Error loading model (retry): {retryEx.Message}", LogLevel.Error);
					await UnloadModelCoreAsync(settle: false, CancellationToken.None).ConfigureAwait(false);
					return false;
				}
			}

			Log($"Error loading model: {ex.Message}", LogLevel.Error);
			await UnloadModelCoreAsync(settle: false, CancellationToken.None).ConfigureAwait(false);
			return false;
		}
		finally
		{
			OnLoadProgress(1);
			State.ModelState.IsModelLoading = false;
			_weightsGate.Release();
		}
	}

	public MtmdWeights GetLoadedLlavaWeights()
	{
		return _weights.LlavaWeights;
	}

	public LLamaWeights GetLoadedWeights()
	{
		return _weights.Weights;
	}

	/// <summary>
	/// Reads native context window size from loaded weights metadata; fallback 4096.
	/// </summary>
	public uint GetModelNativeContextSize()
	{
		if (_weights.Weights == null)
		{
			return 4096;
		}

		try
		{
			var metadata = _weights.Weights.Metadata;
			var key = metadata.Keys.FirstOrDefault(k => k.Contains("context_length", StringComparison.OrdinalIgnoreCase));
			if ((key != null) && metadata.TryGetValue(key, out var value)
				&& uint.TryParse(value, out var nativeSize) && (nativeSize > 0))
			{
				return nativeSize;
			}
		}
		catch
		{
			// ignore
		}

		return 4096;
	}

	public override void InitializeLifecycle()
	{
		Bus.Models.SubscribeToRefreshModels(OnRefreshModels);
		Bus.Models.SubscribeToSelectModel(OnSelectModel);
		Bus.Models.SubscribeToUnloadModel(OnUnloadModel);
		base.InitializeLifecycle();
	}

	public override void LoadLifecycle()
	{
		_hardware.LoadLifecycle();
		Log(_hardware.StatusMessage, LogLevel.Information);
		OnRefreshModels();
		base.LoadLifecycle();
	}

	public override void UninitializeLifecycle()
	{
		Bus.Models.UnsubscribeToRefreshModels(OnRefreshModels);
		Bus.Models.UnsubscribeToSelectModel(OnSelectModel);
		Bus.Models.UnsubscribeToUnloadModel(OnUnloadModel);
		// Best-effort sync unload on shutdown
		UnloadModelAsync().GetAwaiter().GetResult();
		_weightsGate.Dispose();
		base.UninitializeLifecycle();
	}

	public async Task UnloadModelAsync()
	{
		await _weightsGate.WaitAsync().ConfigureAwait(false);
		try
		{
			await UnloadModelCoreAsync(settle: true, CancellationToken.None).ConfigureAwait(false);
		}
		finally
		{
			_weightsGate.Release();
		}
	}

	/// <summary>
	/// Loads GGUF weights (caller holds <see cref="_weightsGate"/>). Previous model must already be unloaded.
	/// </summary>
	private async Task<bool> LoadWeightsCoreAsync(string desiredPath, CancellationToken cancellationToken)
	{
		var model = FindModel(desiredPath) ?? ModelInfo.Create(desiredPath);
		if (!model.IsValidGguf)
		{
			Log($"Invalid GGUF: {desiredPath}", LogLevel.Error);
			return false;
		}

		_hardware.LoadLifecycle();
		ConfigureNativeLibrary(_hardware.SelectedBackend);

		Log(
			$"Loading model '{model.ModelName}' from '{model.FilePath}' backend={_hardware.SelectedBackend}",
			LogLevel.Information);

		var parameters = new ModelParams(model.FilePath)
		{
			ContextSize = 32768,
			GpuLayerCount = 99
		};

		IProgress<float> progress = new Progress<float>(OnLoadProgress);
		var weights = await LLamaWeights
			.LoadFromFileAsync(parameters, cancellationToken, progress)
			.ConfigureAwait(false);

		MtmdWeights llavaWeights = null;
		if (model.IsVisionModel && !string.IsNullOrEmpty(model.MmprojPath) && File.Exists(model.MmprojPath))
		{
			try
			{
				llavaWeights = MtmdWeights.LoadFromFile(model.MmprojPath, weights, MtmdContextParams.Default());
			}
			catch (Exception mmEx)
			{
				Log($"WARNING: mmproj load failed (vision disabled): {mmEx.Message}", LogLevel.Warning);
			}
		}

		_weights.Weights = weights;
		_weights.LlavaWeights = llavaWeights;
		State.ModelState.LoadedModelPath = model.FilePath;
		SetActiveModel(model.FilePath);

		OnLoadProgress(1);
		Log($"Model loaded: {model.FilePath}", LogLevel.Information);
		return true;
	}

	private static async Task SettleAfterUnloadAsync(CancellationToken cancellationToken)
	{
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
		GC.WaitForPendingFinalizers();
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

		// Give the GPU driver a beat to actually free device memory after Dispose.
		try
		{
			await Task.Delay(UnloadSettle, cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			// ignore settle cancel — caller handles cancellation
		}
	}

	/// <summary>
	/// Disposes session + weights. Caller must hold <see cref="_weightsGate"/> (or be single-threaded shutdown).
	/// </summary>
	private async Task UnloadModelCoreAsync(bool settle, CancellationToken cancellationToken)
	{
		// Always drop session first (even if weights already null) so we never keep a live context.
		try
		{
			_weights.NotifyUnloading();
		}
		catch (Exception ex)
		{
			Log($"BeforeWeightsUnload failed: {ex.Message}", LogLevel.Warning);
		}

		if (_weights.Weights == null)
		{
			ClearLoadedState();
			return;
		}

		var previous = State.ModelState.LoadedModelPath;
		var llava = _weights.LlavaWeights;
		var weights = _weights.Weights;
		_weights.LlavaWeights = null;
		_weights.Weights = null;

		await Task.Run(
			() =>
			{
				try
				{
					llava?.Dispose();
				}
				catch
				{
					// best-effort
				}

				try
				{
					weights.Dispose();
				}
				catch
				{
					// best-effort
				}
			},
			cancellationToken).ConfigureAwait(false);

		ClearLoadedState();
		Log($"Model unloaded: {previous}", LogLevel.Information);

		if (settle)
		{
			Log($"Settling after unload ({UnloadSettle.TotalMilliseconds:0} ms)…", LogLevel.Debug);
			await SettleAfterUnloadAsync(cancellationToken).ConfigureAwait(false);
		}
	}

	private void ClearLoadedState()
	{
		foreach (var model in State.ModelState.Models)
		{
			if (model.IsActive)
			{
				model.IsActive = false;
			}
		}

		State.ModelState.LoadedModelPath = null;
	}

	private void ConfigureNativeLibrary(ExecutionBackend selectedBackend)
	{
		if (_nativeLibConfigured)
		{
			return;
		}

		try
		{
			var baseDir = AppContext.BaseDirectory;

			var arch = RuntimeInformation.ProcessArchitecture switch
			{
				Architecture.X86 => "win-x86",
				Architecture.Arm64 => "win-arm64",
				_ => "win-x64"
			};

			var bestSubDir = selectedBackend switch
			{
				ExecutionBackend.Cuda => "cuda12",
				ExecutionBackend.Vulkan => "vulkan",
				_ => DetectBestCpuVariant()
			};

			var runtimeNativeDir = Path.Combine(baseDir, "runtimes", arch, "native");
			var targetDir = Path.Combine(runtimeNativeDir, bestSubDir);

			Log($"ConfigureNativeLibrary arch={arch} backend={selectedBackend} dir={targetDir}", LogLevel.Debug);

			if (!Directory.Exists(targetDir))
			{
				targetDir = Path.Combine(runtimeNativeDir, "noavx");
			}
			if (!Directory.Exists(targetDir))
			{
				targetDir = Directory.Exists(runtimeNativeDir) ? runtimeNativeDir : baseDir;
			}

			if (!SetDllDirectory(targetDir))
			{
				Log($"SetDllDirectory FAILED: Win32 error {Marshal.GetLastWin32Error()}", LogLevel.Warning);
			}

			PreloadNativeDlls(targetDir);

			var config = NativeLibraryConfig.All
				.WithLogCallback((_, message) => Log(message?.TrimEnd(), LogLevel.Debug))
				.WithSearchDirectory(targetDir);

			switch (selectedBackend)
			{
				case ExecutionBackend.Cuda:
					config.WithCuda();
					break;
				case ExecutionBackend.Vulkan:
					config.WithVulkan();
					break;
				default:
					config.WithCuda(false).WithVulkan(false);
					break;
			}

			config.WithAutoFallback(false)
				.WithSearchDirectory(AppContext.BaseDirectory);

			_nativeLibConfigured = true;
			Log("Native library configured.", LogLevel.Information);
		}
		catch (Exception ex)
		{
			Log($"NativeLibraryConfig setup FAILED: {ex}", LogLevel.Error);
		}
	}

	private static string DetectBestCpuVariant()
	{
		try
		{
			if (Avx512F.IsSupported)
			{
				return "avx512";
			}
			if (Avx2.IsSupported)
			{
				return "avx2";
			}
			if (Avx.IsSupported)
			{
				return "avx";
			}
		}
		catch
		{
			// ARM64 or other arch
		}
		return "noavx";
	}

	private ModelInfo FindModel(string filePath)
	{
		return State.ModelState.Models.FirstOrDefault(m =>
			string.Equals(m.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
	}

	private void Log(string message, LogLevel level)
	{
		if (string.IsNullOrEmpty(message))
		{
			return;
		}

		Bus.Logging.Log(message, level);
		State.ModelState.RefreshStatusMessage = message;
		State.ModelState.ModelIngress.Append(message);
		State.ModelState.ModelIngress.Append(Environment.NewLine);
	}

	private void OnLoadProgress(float percent)
	{
		State.ModelState.LoadingPercent = percent;
	}

	private void OnRefreshModels()
	{
		OnRefreshModels(default);
	}

	private void OnRefreshModels(ModelsChannel.RefreshModelsMessage message)
	{
		Task.Run(RefreshModels);
	}

	private void OnSelectModel(ModelsChannel.SelectModelMessage message)
	{
		SelectModel(message.FilePath);
	}

	private void OnUnloadModel(ModelsChannel.UnloadModelMessage message)
	{
		// Awaited fire-and-forget is fine for UI; gate serializes against EnsureLoaded.
		_ = UnloadModelAsync();
	}

	private void PreloadNativeDlls(string nativeDir)
	{
		string[] dllsInOrder =
		[
			"ggml-base.dll",
			"ggml.dll",
			"ggml-cpu.dll",
			"ggml-cuda.dll",
			"ggml-vulkan.dll",
			"llama.dll"
		];

		foreach (var dll in dllsInOrder)
		{
			var fullPath = Path.Combine(nativeDir, dll);
			if (!File.Exists(fullPath))
			{
				continue;
			}

			try
			{
				NativeLibrary.Load(fullPath);
			}
			catch (Exception ex)
			{
				Log($"Pre-load FAILED for {dll}: {ex.Message}", LogLevel.Debug);
			}
		}
	}

	private void RefreshModels()
	{
		if (Interlocked.CompareExchange(ref _isRefreshing, 1, 0) != 0)
		{
			return;
		}

		try
		{
			State.ModelState.IsRefreshingModels = true;

			if (!Directory.Exists(State.Settings.ModelDirectory))
			{
				Log($"Model directory not found: {State.Settings.ModelDirectory}", LogLevel.Error);
				return;
			}

			Log("Scanning for GGUF models...", LogLevel.Information);

			var options = new EnumerationOptions
			{
				RecurseSubdirectories = State.Settings.RecurseModelDirectory,
				IgnoreInaccessible = true,
				MaxRecursionDepth = 20,
				ReturnSpecialDirectories = false
			};

			var files = new List<string>(200);
			foreach (var file in Directory.EnumerateFiles(State.Settings.ModelDirectory, "*.gguf", options))
			{
				files.Add(file);

				if (files.Count >= 200)
				{
					Log($"Maximum files reached at {files.Count}.", LogLevel.Warning);
					break;
				}
			}

			var ggufCandidates = files
				.Select(x => new FileInfo(x))
				.Where(f =>
					f.Name.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase)
					&& !f.Name.StartsWith("mmproj-", StringComparison.OrdinalIgnoreCase)
				)
				.ToList();

			var models = new List<ModelInfo>();
			foreach (var filePath in ggufCandidates)
			{
				var model = ModelInfo.Create(filePath.FullName);
				if (!model.IsValidGguf)
				{
					continue;
				}
				models.Add(model);
			}

			// Preserve IsActive on the currently loaded path after reconcile
			var loadedPath = State.ModelState.LoadedModelPath;
			State.ModelState.Models.ReconcileList(models, _modelInfoComparer);
			if (!string.IsNullOrEmpty(loadedPath))
			{
				SetActiveModel(loadedPath);
			}

			RestoreSelectionFromSettings();

			Log($"Successfully loaded {models.Count} model(s).", LogLevel.Information);
			Bus.Models.ModelsUpdated();
		}
		catch (UnauthorizedAccessException ex)
		{
			Log($"Access denied: {ex.Message}", LogLevel.Error);
		}
		catch (Exception ex)
		{
			Log($"Error loading models: {ex.Message}", LogLevel.Error);
		}
		finally
		{
			Interlocked.Exchange(ref _isRefreshing, 0);
			State.ModelState.IsRefreshingModels = false;
		}
	}

	private void RestoreSelectionFromSettings()
	{
		var preferred = State.Settings.SelectedModel;
		var match = State.ModelState.Models.FirstOrDefault(m =>
				string.Equals(m.FilePath, preferred, StringComparison.OrdinalIgnoreCase))
			?? State.ModelState.Models.FirstOrDefault();

		if (match != null)
		{
			// Desired only — do not load
			State.ModelState.SelectedModelPath = match.FilePath;
			State.Settings.SelectedModel = match.FilePath;
		}
		else
		{
			State.ModelState.SelectedModelPath = null;
		}
	}

	private void SelectModel(string filePath)
	{
		if (State.ModelState.IsExecuting || State.ModelState.IsModelLoading)
		{
			Log("Cannot change model while busy.", LogLevel.Warning);
			return;
		}

		if (string.IsNullOrWhiteSpace(filePath))
		{
			return;
		}

		// Prefer known list entry; allow path that exists on disk even if not yet scanned
		var model = FindModel(filePath);
		if ((model == null) && !File.Exists(filePath))
		{
			Log($"Cannot select model (not found): {filePath}", LogLevel.Warning);
			return;
		}

		State.ModelState.SelectedModelPath = filePath;
		State.Settings.SelectedModel = filePath;

		var label = model?.ModelName ?? Path.GetFileNameWithoutExtension(filePath);
		if (State.ModelState.HasPendingModelSwitch)
		{
			Log($"Model selected (loads on next request): {label}", LogLevel.Information);
		}
		else
		{
			Log($"Model selected (already loaded): {label}", LogLevel.Information);
		}
	}

	private void SetActiveModel(string filePath)
	{
		foreach (var model in State.ModelState.Models)
		{
			model.IsActive = string.Equals(model.FilePath, filePath, StringComparison.OrdinalIgnoreCase);
		}
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetDllDirectory(string lpPathName);

	#endregion
}
