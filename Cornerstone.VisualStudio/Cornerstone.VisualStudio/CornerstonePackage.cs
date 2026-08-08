#region References

using System;
using System.ComponentModel;
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
[InstalledProductRegistration("#110", "#112", "1.2.7", IconResourceID = 400)]
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[ProvideEditorExtension(typeof(EditorFactory), $".{CornerstoneConstants.Axaml}", 100, NameResourceID = 113, EditorFactoryNotify = true, ProjectGuid = VSConstants.UICONTEXT.CSharpProject_string, DefaultName = CornerstoneConstants.PackageName)]
[ProvideEditorExtension(typeof(EditorFactory), $".{CornerstoneConstants.Xaml}", 0x40, NameResourceID = 113, EditorFactoryNotify = true, ProjectGuid = VSConstants.UICONTEXT.CSharpProject_string, DefaultName = CornerstoneConstants.PackageName)]
[ProvideEditorFactory(typeof(EditorFactory), 113, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
[ProvideEditorLogicalView(typeof(EditorFactory), LogicalViewID.Designer)]
[ProvideXmlEditorChooserDesignerView(CornerstoneConstants.PackageName,
	CornerstoneConstants.Xaml,
	LogicalViewID.Designer,
	10001,
	Namespace = "https://github.com/avaloniaui",
	MatchExtensionAndNamespace = false,
	CodeLogicalViewEditor = typeof(EditorFactory),
	DesignerLogicalViewEditor = typeof(EditorFactory),
	DebuggingLogicalViewEditor = typeof(EditorFactory),
	TextLogicalViewEditor = typeof(EditorFactory))]
[ProvideXmlEditorChooserDesignerView(CornerstoneConstants.PackageName,
	CornerstoneConstants.Axaml,
	LogicalViewID.Designer,
	10000,
	Namespace = "https://github.com/avaloniaui",
	MatchExtensionAndNamespace = false,
	CodeLogicalViewEditor = typeof(EditorFactory),
	DesignerLogicalViewEditor = typeof(EditorFactory),
	DebuggingLogicalViewEditor = typeof(EditorFactory),
	TextLogicalViewEditor = typeof(EditorFactory))]
// Options: modern VisualStudio.Extensibility Settings (see CornerstoneSettingDefinitions).
// Legacy ProvideOptionPage / UIElementDialogPage removed — VS 2026 only shows them as "not migrated".
// TEMP: Code Cleanup UI disabled for release — re-enable with CodeCleanupUiEnabled and vsct groups.
// [ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideBindingPath]
internal sealed class CornerstonePackage : AsyncPackage
{
	#region Fields

	private LoggingLevelSwitch _levelSwitch;
	private ICornerstoneSettings _settings;

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

		var dte = (DTE) await GetServiceAsync(typeof(DTE));
		SolutionService = new SolutionService(dte);

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

		Log.Logger.Information("Cornerstone initialized");
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

	#endregion
}