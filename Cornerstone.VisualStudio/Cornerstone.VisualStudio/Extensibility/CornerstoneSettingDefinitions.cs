#region References

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Settings;
using Serilog.Events;

#endregion

namespace Cornerstone.VisualStudio.Extensibility;

#pragma warning disable VSEXTPREVIEW_SETTINGS // Settings API is preview / experimental.

/// <summary>
/// Modern Visual Studio Settings contributions for Cornerstone (VS 2026 Settings UI).
/// </summary>
/// <remarks>
/// Ids form keys like "cornerstone.designerView". Defaults match
/// <see cref="Services.CornerstoneSettings"/> historical defaults.
/// </remarks>
internal static class CornerstoneSettingDefinitions
{
	#region Categories

	[VisualStudioContribution]
	internal static SettingCategory CornerstoneCategory { get; } = new(
		"cornerstone",
		"Cornerstone")
	{
		Description = "Avalonia designer and previewer options for the Cornerstone extension."
		// Observer codegen disabled: generated ExperimentalAttribute is not accessible on net472 hybrid builds.
	};

	#endregion

	#region Designer

	[VisualStudioContribution]
	internal static Setting.Enum DesignerView { get; } = new(
		"designerView",
		"Default document view",
		CornerstoneCategory,
		[
			new EnumSettingEntry("Split", "Split"),
			new EnumSettingEntry("Design", "Design"),
			new EnumSettingEntry("Source", "Source")
		],
		defaultValue: "Split")
	{
		Description = "Initial view mode when opening an AXAML / Avalonia XAML document."
	};

	[VisualStudioContribution]
	internal static Setting.Enum DesignerSplitOrientation { get; } = new(
		"designerSplitOrientation",
		"Split orientation",
		CornerstoneCategory,
		[
			new EnumSettingEntry("Horizontal", "Horizontal"),
			new EnumSettingEntry("Vertical", "Vertical")
		],
		defaultValue: "Vertical")
	{
		Description = "Default split orientation for preview and source panes."
	};

	[VisualStudioContribution]
	internal static Setting.Boolean DesignerSplitSwapped { get; } = new(
		"designerSplitSwapped",
		"Swap preview and XAML panes",
		CornerstoneCategory,
		defaultValue: false)
	{
		Description = "When true, the preview and XAML panes start swapped."
	};

	// Values must be compile-time constants for VisualStudioContribution evaluation (no LINQ / helpers).
	[VisualStudioContribution]
	internal static Setting.Enum ZoomLevel { get; } = new(
		"zoomLevel",
		"Default zoom level",
		CornerstoneCategory,
		[
			new EnumSettingEntry("800%", "800%"),
			new EnumSettingEntry("400%", "400%"),
			new EnumSettingEntry("200%", "200%"),
			new EnumSettingEntry("150%", "150%"),
			new EnumSettingEntry("100%", "100%"),
			new EnumSettingEntry("66.67%", "66.67%"),
			new EnumSettingEntry("50%", "50%"),
			new EnumSettingEntry("33.33%", "33.33%"),
			new EnumSettingEntry("25%", "25%"),
			new EnumSettingEntry("12.5%", "12.5%")
		],
		defaultValue: "100%")
	{
		Description = "Default zoom for the designer preview."
	};

	#endregion

	#region Diagnostics

	[VisualStudioContribution]
	internal static Setting.Enum MinimumLogVerbosity { get; } = new(
		"minimumLogVerbosity",
		"Minimum log verbosity",
		CornerstoneCategory,
		[
			new EnumSettingEntry(nameof(LogEventLevel.Verbose), "Verbose"),
			new EnumSettingEntry(nameof(LogEventLevel.Debug), "Debug"),
			new EnumSettingEntry(nameof(LogEventLevel.Information), "Information"),
			new EnumSettingEntry(nameof(LogEventLevel.Warning), "Warning"),
			new EnumSettingEntry(nameof(LogEventLevel.Error), "Error"),
			new EnumSettingEntry(nameof(LogEventLevel.Fatal), "Fatal")
		],
		defaultValue: nameof(LogEventLevel.Information))
	{
		Description = "Minimum Serilog level written to the Cornerstone output pane."
	};

	[VisualStudioContribution]
	internal static Setting.Boolean ShowPreviewHostRunningInTab { get; } = new(
		"showPreviewHostRunningInTab",
		"Show previewer-running prefix on tabs",
		CornerstoneCategory,
		defaultValue: false)
	{
		Description =
			"When enabled, open AXAML tabs are prefixed with • while the design host process is running (for example •MainView.axaml)."
	};

	#endregion
}

#pragma warning restore VSEXTPREVIEW_SETTINGS