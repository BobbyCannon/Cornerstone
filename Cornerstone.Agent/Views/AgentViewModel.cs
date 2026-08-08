#region References

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Agent.Keystone;
using Cornerstone.Agent.Keystone.Processors;
using Cornerstone.Agent.Keystone.State;
using Cornerstone.Avalonia.Text;
using Cornerstone.Compare;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Agent.Views;

[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
[SourceReflection]
[DependencyInjected]
public partial class AgentViewModel : DispatchableViewModel
{
	#region Fields

	public static readonly string AssemblyName;
	private static readonly GenericEqualityComparer<ModelInfo> ModelInfoComparer;
	private readonly AgentProcessor _agent;
	private CancellationTokenSource _cancellationTokenSource;

	/// <summary>
	/// Prevents re-entrant Execute (e.g. Enter keybinding firing twice) from canceling an in-flight load.
	/// </summary>
	private int _executeGate;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public AgentViewModel(AppBus bus, AppState state, AgentProcessor agentProcessor)
	{
		_agent = agentProcessor;

		Bus = bus;
		State = state;
		Models = [];

		LogForOutput = new TextEditorViewModel();
		LogForModel = new TextEditorViewModel();
		LogForReasoning = new TextEditorViewModel();
		LogForRaw = new TextEditorViewModel();
		LogForSystem = new TextEditorViewModel();

		TrackIngress(state.ModelState.ModelIngress, LogForModel.Append);
		TrackIngress(state.ModelState.OutputIngress, LogForOutput.Append);
		TrackIngress(state.ModelState.ReasoningIngress, LogForReasoning.Append);
		TrackIngress(state.ModelState.RawIngress, LogForRaw.Append);
		TrackIngress(state.ModelState.SystemIngress, LogForSystem.Append);
		TrackCollection(state.ModelState.Models, Models, ModelInfoComparer, CollectionReconcileMode.List);

		// Busy flags drive CanSelectModel (combo IsEnabled).
		TrackProperties(state.ModelState)
			.MapOneWay<bool, bool>(nameof(ModelState.IsExecuting), nameof(IsExecuting), static v => v)
			.MapOneWay<bool, bool>(nameof(ModelState.IsModelLoading), nameof(IsModelLoading), static v => v);

		// Settings.SelectedModel (string path) ↔ SelectedModel (ModelInfo).
		// Names can differ (e.g. FavoriteModel) without changing this shape — only the model property name.
		TrackProperties(state.Settings)
			.MapTwoWay<string, ModelInfo>(
				nameof(AppSettings.SelectedModel),
				nameof(SelectedModel),
				ResolveModel,
				model => model?.FilePath);
	}

	static AgentViewModel()
	{
		AssemblyName = typeof(AgentViewModel).ToAssemblyName();
		ModelInfoComparer = new GenericEqualityComparer<ModelInfo>(
			(x, y) => (x != null) && (y != null) && (x.FilePath == y.FilePath),
			x => x.FilePath.GetStableHashCode()
		);
	}

	#endregion

	#region Properties

	public AppBus Bus { get; }

	/// <summary>
	/// Prompt box enabled when not load/inference busy.
	/// </summary>
	public bool CanEditPrompt => !IsPromptBusy && !IsExecuting && !IsModelLoading;

	/// <summary>
	/// Selection locked while load or inference is running.
	/// </summary>
	public bool CanSelectModel => !IsExecuting && !IsModelLoading;

	[AlsoNotify(nameof(CanSelectModel), nameof(CanEditPrompt), nameof(PromptPlaceholder))]
	public partial bool IsExecuting { get; set; }

	[AlsoNotify(nameof(CanSelectModel), nameof(CanEditPrompt), nameof(PromptPlaceholder))]
	public partial bool IsModelLoading { get; set; }

	/// <summary>
	/// True from send until request fully finishes (covers UI before ModelState.IsExecuting projects).
	/// </summary>
	[AlsoNotify(nameof(CanEditPrompt), nameof(PromptPlaceholder))]
	public partial bool IsPromptBusy { get; set; }

	/// <summary>
	/// Hint text for the prompt box (changes while processing).
	/// </summary>
	public string PromptPlaceholder =>
		(IsPromptBusy || IsExecuting || IsModelLoading)
			? "Processing…"
			: "Send a message to the model…";

	public TextEditorViewModel LogForModel { get; }

	public TextEditorViewModel LogForOutput { get; }

	public TextEditorViewModel LogForRaw { get; }

	public TextEditorViewModel LogForReasoning { get; }

	public TextEditorViewModel LogForSystem { get; }

	public PresentationList<ModelInfo> Models { get; }

	public partial string PromptInput { get; set; }

	/// <summary>
	/// Desired model (user selection). Load is deferred until the next request.
	/// Two-way mapped to <see cref="AppSettings.SelectedModel" /> (path string).
	/// </summary>
	public partial ModelInfo SelectedModel { get; set; }

	public AppState State { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Abort the current load/inference. Does not start a new request.
	/// </summary>
	[RelayCommand]
	public void Cancel()
	{
		_cancellationTokenSource?.Cancel();
	}

	public async Task ExecuteAsync(string prompt)
	{
		if (string.IsNullOrWhiteSpace(prompt))
		{
			return;
		}

		// Double-Enter / re-entrant command: do NOT Cancel() here — that aborts LLama mid-load
		// with "llama_model_load_from_file_impl: cancelled model load".
		if (Interlocked.CompareExchange(ref _executeGate, 1, 0) != 0)
		{
			return;
		}

		// Snapshot then clear immediately so the UI shows "we're working" (not still holding the prompt).
		var request = prompt.Trim();
		PromptInput = string.Empty;
		IsPromptBusy = true;

		var cts = new CancellationTokenSource();
		_cancellationTokenSource = cts;

		try
		{
			await _agent.ProcessAsync(request, cts.Token).ConfigureAwait(true);
		}
		finally
		{
			if (ReferenceEquals(_cancellationTokenSource, cts))
			{
				_cancellationTokenSource = null;
			}

			try
			{
				cts.Dispose();
			}
			catch
			{
				// already disposed
			}

			IsPromptBusy = false;
			Interlocked.Exchange(ref _executeGate, 0);
		}
	}

	[RelayCommand]
	public Task ExecutePromptAsync()
	{
		return ExecuteAsync(PromptInput);
	}

	/// <summary>
	/// Publish selection intent — processors apply busy guards and sync ModelState path.
	/// Settings path is also kept in sync via <see cref="DispatchableViewModel.TrackProperties" /> two-way map.
	/// </summary>
	[RelayCommand]
	public void SelectModel(ModelInfo model)
	{
		if ((model == null) || string.IsNullOrEmpty(model.FilePath))
		{
			return;
		}

		if (!CanSelectModel)
		{
			return;
		}

		SelectedModel = model;
		Bus.Models.SelectModel(model.FilePath);
	}

	protected override void OnPropertyChanged<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		// Combo binding may set SelectedModel directly; keep processor policy + ModelState path in sync.
		if ((propertyName == nameof(SelectedModel))
			&& (SelectedModel != null)
			&& CanSelectModel
			&& !string.Equals(SelectedModel.FilePath, State.Settings.SelectedModel, StringComparison.OrdinalIgnoreCase))
		{
			Bus.Models.SelectModel(SelectedModel.FilePath);
		}

		base.OnPropertyChanged(propertyName, oldValue, newValue);
	}

	private ModelInfo ResolveModel(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}

		return Models.FirstOrDefault(m => string.Equals(m.FilePath, path, StringComparison.OrdinalIgnoreCase))
			?? State.ModelState.Models.FirstOrDefault(m => string.Equals(m.FilePath, path, StringComparison.OrdinalIgnoreCase));
	}

	#endregion
}