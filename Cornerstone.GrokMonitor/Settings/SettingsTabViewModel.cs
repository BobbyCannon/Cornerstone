#region References

using Cornerstone.Data;
using Cornerstone.GrokMonitor.Keystone;
using Cornerstone.GrokMonitor.Keystone.State;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using IDispatcher = Cornerstone.Presentation.IDispatcher;

#endregion

namespace Cornerstone.GrokMonitor.Settings;

/// <summary>
/// Shell settings page (theme, session heat). Bound to <see cref="AppSettings" />.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
public partial class SettingsTabViewModel : DispatchableViewModel, IShellTab
{
	#region Constructors

	public SettingsTabViewModel(AppState state, IDispatcher dispatcher)
	{
		State = state;
		DisplayName = "settings";
		_ = dispatcher;

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

	public AppSettings Settings => State.Settings;

	public AppState State { get; }

	#endregion
}