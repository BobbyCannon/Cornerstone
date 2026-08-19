#region References

using Cornerstone.Avalonia.Themes;
using Cornerstone.Data;
using Cornerstone.GrokMonitor.Keystone.State;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.GrokMonitor.Settings;

/// <summary>
/// Shell settings page (theme, session heat). Projects <see cref="AppSettings" /> via AppDispatcher.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
public partial class SettingsTabViewModel : DispatchableViewModel<AppSettings>, IShellTab, IAppSettings, IUpdateable<IAppSettings>
{
	#region Constructors

	public SettingsTabViewModel(AppSettings settings)
		: base(settings)
	{
		DisplayName = "settings";
		AutoUpdateModel = true;

		// Fixed sample series for the theme-color preview chart (not live usage data).
		ColorSampleChartData = new SeriesDataProvider(12);
		ColorSampleChartData.AddRange(
		[
			12, 18, 15, 28, 22, 35, 30, 42, 38, 48, 44, 55
		]);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Sample line series so accent color changes are visible immediately.
	/// </summary>
	public SeriesDataProvider ColorSampleChartData { get; }

	/// <summary>
	/// Tab header text.
	/// </summary>
	[Notify]
	public partial string DisplayName { get; set; }

	/// <summary>
	/// Optional tooltip (homes use path; settings has none).
	/// </summary>
	public string Path => string.Empty;

	/// <summary>
	/// Fixed progress sample for theme-color feedback (0–100).
	/// </summary>
	public double SampleProgressValue => 68;

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool SessionTokenHeatEnabled { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial long SessionTokenHeatHotTokens { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial long SessionTokenHeatSoftTokens { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial ThemeColor ThemeColor { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial ThemeDensity ThemeDensity { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial ThemeMode ThemeMode { get; set; }

	#endregion
}