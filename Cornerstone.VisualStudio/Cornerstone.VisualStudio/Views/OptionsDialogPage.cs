#region References

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Cornerstone.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Serilog.Events;

#endregion

namespace Cornerstone.VisualStudio.Views;

/// <summary>
/// Tools → Options → Cornerstone page.
/// </summary>
/// <remarks>
/// Uses a standard <see cref="DialogPage"/> (property surface) so VS 2026 modern Settings
/// can host the values inline, instead of a custom WPF <see cref="UIElementDialogPage"/>
/// that only appears behind a nested "General" / legacy dialog link.
/// Persistence goes through <see cref="ICornerstoneSettings"/> (same store as the rest of the extension).
/// </remarks>
[ClassInterface(ClassInterfaceType.AutoDual)]
[ComVisible(true)]
[Guid("3093ca7c-c764-4547-a7ae-12055b139bdf")]
public class OptionsDialogPage : DialogPage
{
	#region Fields

	private AvaloniaDesignerView _designerView = AvaloniaDesignerView.Split;
	private Orientation _designerSplitOrientation = Orientation.Vertical;
	private bool _designerSplitSwapped;
	private string _zoomLevel = "100%";
	private LogEventLevel _minimumLogVerbosity = LogEventLevel.Information;
	private bool _showPreviewHostRunningInTab;

	#endregion

	#region Properties

	[Category("Designer")]
	[DisplayName("Default document view")]
	[Description("Initial view mode when opening an AXAML / Avalonia XAML document.")]
	public AvaloniaDesignerView DesignerView
	{
		get => _designerView;
		set => _designerView = value;
	}

	[Category("Designer")]
	[DisplayName("Split orientation")]
	[Description("Default split orientation for preview and source panes.")]
	public Orientation DesignerSplitOrientation
	{
		get => _designerSplitOrientation;
		set => _designerSplitOrientation = value;
	}

	[Category("Designer")]
	[DisplayName("Swap preview and XAML panes")]
	[Description("When true, the preview and XAML panes start swapped.")]
	public bool DesignerSplitSwapped
	{
		get => _designerSplitSwapped;
		set => _designerSplitSwapped = value;
	}

	[Category("Designer")]
	[DisplayName("Default zoom level")]
	[Description("Default zoom for the designer preview (for example 100%).")]
	public string ZoomLevel
	{
		get => _zoomLevel;
		set => _zoomLevel = string.IsNullOrWhiteSpace(value) ? "100%" : value.Trim();
	}

	[Category("Diagnostics")]
	[DisplayName("Minimum log verbosity")]
	[Description("Minimum Serilog level written to the Cornerstone output.")]
	public LogEventLevel MinimumLogVerbosity
	{
		get => _minimumLogVerbosity;
		set => _minimumLogVerbosity = value;
	}

	[Category("Diagnostics")]
	[DisplayName("Show previewer-running prefix on tabs")]
	[Description("When enabled, open AXAML tabs are prefixed with • while the design host process is running (for example •MainView.axaml). Off by default.")]
	public bool ShowPreviewHostRunningInTab
	{
		get => _showPreviewHostRunningInTab;
		set => _showPreviewHostRunningInTab = value;
	}

	#endregion

	#region Methods

	public override void LoadSettingsFromStorage()
	{
		try
		{
			var settings = GetSettings();
			if (settings is null)
			{
				return;
			}

			settings.Load();
			_designerView = settings.DesignerView;
			_designerSplitOrientation = settings.DesignerSplitOrientation;
			_designerSplitSwapped = settings.DesignerSplitSwapped;
			_zoomLevel = settings.ZoomLevel;
			_minimumLogVerbosity = settings.MinimumLogVerbosity;
			_showPreviewHostRunningInTab = settings.ShowPreviewHostRunningInTab;
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to load Cornerstone options page");
		}
	}

	public override void SaveSettingsToStorage()
	{
		try
		{
			var settings = GetSettings();
			if (settings is null)
			{
				return;
			}

			settings.DesignerView = _designerView;
			settings.DesignerSplitOrientation = _designerSplitOrientation;
			settings.DesignerSplitSwapped = _designerSplitSwapped;
			settings.ZoomLevel = _zoomLevel;
			settings.MinimumLogVerbosity = _minimumLogVerbosity;
			settings.ShowPreviewHostRunningInTab = _showPreviewHostRunningInTab;
			settings.Save();
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to save Cornerstone options page");
		}
	}

	private ICornerstoneSettings GetSettings()
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (Site is not null)
		{
			var fromSite = Site.GetMefService<ICornerstoneSettings>();
			if (fromSite is not null)
			{
				return fromSite;
			}
		}

		if (GetService(typeof(SVsServiceProvider)) is System.IServiceProvider sp)
		{
			return sp.GetMefService<ICornerstoneSettings>();
		}

		return null;
	}

	#endregion
}