#region References

using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using Cornerstone.VisualStudio.Commands;
using Cornerstone.VisualStudio.Extensibility;
using Cornerstone.VisualStudio.Services;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using Serilog.Core;
using Task = System.Threading.Tasks.Task;

#endregion

namespace Cornerstone.VisualStudio;

[Guid(CornerstoneConstants.PackageGuidString)]
[InstalledProductRegistration("#110", "#112", "1.5.277.17577", IconResourceID = 400)]
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
// Priority must beat the XML editor (0x2710 / 10000). .axaml is Avalonia. .cxaml is Cornerstone.
// .xaml stays with the Visual Studio XAML editor.
[ProvideEditorExtension(typeof(EditorFactory), $".{CornerstoneConstants.Cxaml}", 0x2712, NameResourceID = 113, EditorFactoryNotify = true, ProjectGuid = VSConstants.UICONTEXT.CSharpProject_string, DefaultName = CornerstoneConstants.PackageName)]
[ProvideEditorExtension(typeof(EditorFactory), $".{CornerstoneConstants.Axaml}", 0x2712, NameResourceID = 113, EditorFactoryNotify = true, ProjectGuid = VSConstants.UICONTEXT.CSharpProject_string, DefaultName = CornerstoneConstants.PackageName)]
[ProvideEditorFactory(typeof(EditorFactory), 113, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
[ProvideEditorLogicalView(typeof(EditorFactory), LogicalViewID.Designer)]
// Each chooser view needs its own registry name. Reusing "Cornerstone" left only .cxaml in the hive.
// Match extension and namespace so an Avalonia xmlns on a .xaml file does not select this designer.
[ProvideXmlEditorChooserDesignerView("Cornerstone AXAML",
	CornerstoneConstants.Axaml,
	LogicalViewID.Designer,
	10000,
	Namespace = "https://github.com/avaloniaui",
	MatchExtensionAndNamespace = true,
	CodeLogicalViewEditor = typeof(EditorFactory),
	DesignerLogicalViewEditor = typeof(EditorFactory),
	DebuggingLogicalViewEditor = typeof(EditorFactory),
	TextLogicalViewEditor = typeof(EditorFactory))]
[ProvideXmlEditorChooserDesignerView("Cornerstone CXAML",
	CornerstoneConstants.Cxaml,
	LogicalViewID.Designer,
	10000,
	Namespace = "https://github.com/BobbyCannon/Cornerstone",
	MatchExtensionAndNamespace = true,
	CodeLogicalViewEditor = typeof(EditorFactory),
	DesignerLogicalViewEditor = typeof(EditorFactory),
	DebuggingLogicalViewEditor = typeof(EditorFactory),
	TextLogicalViewEditor = typeof(EditorFactory))]
// Options: modern VisualStudio.Extensibility Settings (see CornerstoneSettingDefinitions).
// Legacy ProvideOptionPage / UIElementDialogPage removed — VS 2026 only shows them as "not migrated".
// Code Cleanup commands stay commented out in the vsct. This resource is the Cornerstone tool window entry.
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideToolWindow(typeof(Views.CornerstoneProcessesWindow), Style = VsDockStyle.Tabbed, Orientation = ToolWindowOrientation.Right, MultiInstances = false)]
[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideBindingPath]
internal sealed class CornerstonePackage : AsyncPackage
{
	#region Fields

	private LoggingLevelSwitch _levelSwitch;
	private CompletionMetadataWarmup _metadataWarmup;
	private ICornerstoneSettings _settings;
	private StopBuildOnFirstFailureService _stopBuildOnFirstFailure;

	#endregion

	#region Properties

	public static SolutionService SolutionService { get; private set; }

	#endregion

	#region Methods

	protected override async Task InitializeAsync(
		CancellationToken cancellationToken,
		IProgress<ServiceProgressData> progress)
	{
		await base.InitializeAsync(cancellationToken, progress);
		await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

		InitializeLogging();
		RegisterEditorFactory(new EditorFactory(this));
		Log.Logger.Debug("Editor factory registered ({FactoryGuid})",
			CornerstoneConstants.CornerstoneFactoryEditorGuidString);

		var dte = (DTE) await GetServiceAsync(typeof(DTE));
		SolutionService = new SolutionService(dte);
		_metadataWarmup = new CompletionMetadataWarmup();
		_metadataWarmup.Start(dte);

		// TEMP: Code Cleanup UI disabled for release (see CornerstoneConstants.CodeCleanupUiEnabled).
		if (CornerstoneConstants.CodeCleanupUiEnabled)
		{
			try
			{
				await CodeCleanupCommands.InitializeAsync(this);
			}
			catch (Exception ex)
			{
				// Menu/command registration must never prevent the designer/previewer from loading.
				Log.Error(ex, "Code Cleanup command registration failed");
			}
		}

		try
		{
			await CornerstoneSettingsBridge.StartAsync(this, cancellationToken);
		}
		catch (Exception ex)
		{
			// Settings bridge is non-critical; designer must still load.
			Log.Error(ex, "Cornerstone modern Settings bridge failed to start");
		}

		try
		{
			_stopBuildOnFirstFailure = new StopBuildOnFirstFailureService(dte, _settings);
			_stopBuildOnFirstFailure.Start();
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Stop-on-first-build-failure listener failed to start");
		}

		try
		{
			var commands = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
			if (commands != null)
			{
				var commandId = new CommandID(new Guid("B7E6A0F1-4C2D-4A8E-9F31-0D5C8A2E7B10"), 0x0102);
				commands.AddCommand(new MenuCommand(ShowProcesses, commandId));
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Cornerstone tool window command failed to register");
		}

		Log.Logger.Information("Cornerstone v{Version:l} initialized", CornerstoneConstants.PackageVersion);
		_ = JoinableTaskFactory.RunAsync(() => CornerstoneStatusBarButton.InjectAsync(this));
	}

	private void ShowProcesses(object sender, EventArgs e)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		CornerstoneStatusBarButton.ShowProcesses(this);
	}

	private void InitializeLogging()
	{
		const string format = "{Timestamp:HH:mm:ss.fff} [{Level}] {Pid} {Message}{NewLine}{Exception}";
		var output = this.GetService<IVsOutputWindow, SVsOutputWindow>();
		_settings = this.GetMefService<ICornerstoneSettings>();
		_levelSwitch = new LoggingLevelSwitch { MinimumLevel = _settings.MinimumLogVerbosity };
		_settings.PropertyChanged += OnSettingsOnPropertyChanged;

		var sink = new OutputPaneEventSink(output, format);
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.ControlledBy(_levelSwitch)
			.WriteTo.Sink(sink, levelSwitch: _levelSwitch)
			.WriteTo.Trace(outputTemplate: format)
			.CreateLogger();
	}

	private void OnSettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(_settings.MinimumLogVerbosity))
		{
			_levelSwitch.MinimumLevel = _settings.MinimumLogVerbosity;
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			try
			{
				EditorHostSession.Shutdown();
			}
			catch (Exception ex)
			{
				Log.Debug(ex, "Editor host shutdown failed");
			}

			try
			{
				PreviewerProcess.ShutdownAll();
			}
			catch (Exception ex)
			{
				Log.Debug(ex, "Previewer shutdown failed");
			}
		}

		base.Dispose(disposing);
	}

	#endregion
}