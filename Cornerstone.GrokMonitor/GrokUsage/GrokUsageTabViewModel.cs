#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using Avalonia.Controls;
using Avalonia.Media;
using Cornerstone.Avalonia.TreeDataGrid;
using Cornerstone.Avalonia.TreeDataGrid.Columns;
using Cornerstone.Avalonia.TreeDataGrid.Selection;
using Cornerstone.Data;
using Cornerstone.GrokMonitor.GrokUsage.Models;
using Cornerstone.GrokMonitor.GrokUsage.Services;
using Cornerstone.GrokMonitor.GrokUsage.State;
using Cornerstone.GrokMonitor.GrokUsage.ViewModels;
using Cornerstone.GrokMonitor.Keystone;
using Cornerstone.GrokMonitor.Keystone.State;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using IDispatcher = Cornerstone.Presentation.IDispatcher;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage;

/// <summary>
/// Dashboard page for one Grok home (Personal or Work).
/// Projects that home from <see cref="AppState.GrokUsage" /> via AppDispatcher; publishes refresh/period intent only.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[ProjectFrom<IGrokHomeUsage>]
public partial class GrokUsageTabViewModel : DispatchableViewModel, IShellTab, IGrokHomeUsage
{
	#region Fields

	private readonly AppBus _bus;
	private int _dailyTokenDayCount;
	private int _dailyUsageDayCount;
	private readonly IDateTimeProvider _dateTimeProvider;
	private readonly GrokUsageState _grokUsage;
	private readonly GrokHomeUsageState _home;
	private bool _isDesignSample;
	private readonly AppSettings _settings;

	#endregion

	#region Constructors

	public GrokUsageTabViewModel(
		AppBus bus,
		GrokHomeUsageState home,
		GrokUsageState grokUsage,
		AppSettings settings,
		IDispatcher dispatcher,
		IDateTimeProvider dateTimeProvider)
	{
		_bus = bus;
		_home = home ?? throw new ArgumentNullException(nameof(home));
		_grokUsage = grokUsage ?? throw new ArgumentNullException(nameof(grokUsage));
		_settings = settings ?? throw new ArgumentNullException(nameof(settings));
		_dateTimeProvider = dateTimeProvider ?? DateTimeProvider.RealTime;
		HomeId = home.Id;

		Sessions = new PresentationList<GrokSessionRowViewModel>(dispatcher);
		AvailablePeriods = new PresentationList<GrokUsagePeriodViewModel>(dispatcher);

		SessionsSource = new FlatTreeDataGridSource<GrokSessionRowViewModel>(Sessions)
		{
			Columns =
			{
				new TextColumn<GrokSessionRowViewModel, string>("Title", x => x.Title, new GridLength(2, GridUnitType.Star)),
				new TextColumn<GrokSessionRowViewModel, string>("Directory", x => x.WorkingDirectory, new GridLength(1, GridUnitType.Star)),
				new TextColumn<GrokSessionRowViewModel, string>("Model", x => x.CurrentModelId, new GridLength(1, GridUnitType.Star)),
				CompactPercentColumn("Usage", x => GrokUsageAnalytics.FormatAllocatedUsagePercent(x.UsagePercent, x.HasAllocatedUsage), x => x.UsagePercent, 100),
				CompactCountColumn("Total", x => GrokUsageAnalytics.FormatCompactTokens(x.TotalTokens), x => x.TotalTokens, 100),
				CompactCountColumn("Prompt", x => GrokUsageAnalytics.FormatCompactTokens(x.PromptTokens), x => x.PromptTokens, 100),
				CompactCountColumn("Cached", x => GrokUsageAnalytics.FormatCompactTokens(x.CachedPromptTokens), x => x.CachedPromptTokens, 100),
				CompactCountColumn("Completion", x => GrokUsageAnalytics.FormatCompactTokens(x.CompletionTokens), x => x.CompletionTokens, 100),
				CompactCountColumn("Reasoning", x => GrokUsageAnalytics.FormatCompactTokens(x.ReasoningTokens), x => x.ReasoningTokens, 100),
				CompactCountColumn("Inf", x => GrokUsageAnalytics.FormatCompactTokens(x.InferenceCount), x => x.InferenceCount, 80),
				CompactCountColumn("Msgs", x => GrokUsageAnalytics.FormatCompactTokens(x.MessageCount), x => x.MessageCount, 90)
			}
		};

		SessionsSelection = new TreeDataGridRowSelectionModel<GrokSessionRowViewModel>(SessionsSource) { SingleSelect = true };
		SessionsSource.Selection = SessionsSelection;
		SessionsSelection.SelectionChanged += SessionsSelectionOnSelectionChanged;

		// Sized to real day count on each project (see FillDailyChart); start with 2 for empty state.
		DailyTokensChartData = new SeriesDataProvider(2);
		DailyTokensChartCaption = "No daily token data";
		DailyTokenTotalChartData = new SeriesDataProvider(2);
		DailyTokenTotalChartCaption = "No token total data";
		DailyUsageChartData = new SeriesDataProvider(2);
		DailyUsageChartCaption = "No daily usage data";
		DailyUsageTotalChartData = new SeriesDataProvider(2);
		DailyUsageTotalChartCaption = "No usage total data";

		Path = string.Empty;
		DisplayName = string.Empty;
		ProgressText = string.Empty;
		ErrorText = string.Empty;
		LastError = string.Empty;
		SubscriptionTier = string.Empty;
		PeriodType = string.Empty;
		AnalyticsNote = string.Empty;
		UsageRateSource = string.Empty;
		UsageExhaustionText = string.Empty;
		PeriodRemainingText = string.Empty;
		PaceLabel = string.Empty;
		PaceLabelToolTip = string.Empty;
		LinearPaceToolTip = string.Empty;
		UsageToolTip = string.Empty;
		TokenBurn24hToolTip = string.Empty;
		TokenBurnPeriodToolTip = string.Empty;
		UsagePercentPerHourToolTip = string.Empty;
		UsageEtaToolTip = string.Empty;
		OnDemandToolTip = string.Empty;
		StatusText = "Loading usage…";
		TokenTotalsPeriodLabel = string.Empty;
		ViewAsOfText = string.Empty;
		IsViewLive = true;
		ViewClockProgress = 1;
		HasViewClock = false;
		IsReplayPlaying = false;

		// Tab headers bind before the tab is attached, so AppDispatcher has not
		// applied TrackProperties yet. Seed identity from the home State.
		DisplayName = home.DisplayName ?? string.Empty;
		Path = home.Path ?? string.Empty;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Billing periods for the period dropdown (selected home).
	/// </summary>
	public PresentationList<GrokUsagePeriodViewModel> AvailablePeriods { get; }

	/// <summary>
	/// Caption under the cumulative tokens chart (date span, first day → running total).
	/// </summary>
	[Notify]
	public partial string DailyTokenTotalChartCaption { get; set; }

	/// <summary>
	/// Cumulative tokens (running sum of daily totals) for the analytics window.
	/// </summary>
	[Notify]
	public partial SeriesDataProvider DailyTokenTotalChartData { get; set; }

	/// <summary>
	/// Caption under the daily tokens chart (date span, peak, latest).
	/// </summary>
	[Notify]
	public partial string DailyTokensChartCaption { get; set; }

	/// <summary>
	/// Daily total tokens for the analytics window (series length matches real days, max 14).
	/// Replaced when the day count changes so the chart rescales horizontally.
	/// </summary>
	[Notify]
	public partial SeriesDataProvider DailyTokensChartData { get; set; }

	/// <summary>
	/// Caption under the daily credit chart (peak/latest gain in percentage points).
	/// </summary>
	[Notify]
	public partial string DailyUsageChartCaption { get; set; }

	/// <summary>
	/// Daily credit gain (percentage points) for the analytics window.
	/// </summary>
	[Notify]
	public partial SeriesDataProvider DailyUsageChartData { get; set; }

	/// <summary>
	/// Caption under the cumulative credit % chart (start → now on 0–100%).
	/// </summary>
	[Notify]
	public partial string DailyUsageTotalChartCaption { get; set; }

	/// <summary>
	/// Cumulative credit usage % (end-of-day) for the billing period (0–100 axis).
	/// </summary>
	[Notify]
	public partial SeriesDataProvider DailyUsageTotalChartData { get; set; }

	[Notify]
	public partial bool HasAnalytics { get; set; }

	[Notify]
	public partial bool HasOnDemandCap { get; set; }

	/// <summary>
	/// True when a period range exists so the view-clock scrubber can be shown.
	/// </summary>
	[Notify]
	public partial bool HasViewClock { get; set; }

	/// <summary>
	/// Fixed Grok home this dashboard projects (does not change after construction).
	/// </summary>
	public Guid HomeId { get; }

	/// <summary>
	/// True while period replay is advancing the view clock.
	/// </summary>
	[Notify]
	public partial bool IsReplayPlaying { get; set; }

	/// <summary>
	/// True when the period scrubber is pinned to live wall time.
	/// </summary>
	[Notify]
	public partial bool IsViewLive { get; set; }

	[Notify]
	public partial string LastError { get; set; }

	/// <summary>
	/// Tooltip for the linear pace percent line.
	/// </summary>
	[Notify]
	public partial string LinearPaceToolTip { get; set; }

	/// <summary>
	/// Tooltip for on-demand spend vs cap.
	/// </summary>
	[Notify]
	public partial string OnDemandToolTip { get; set; }

	/// <summary>
	/// On-demand used as percent of cap (0–100); 0 when no cap.
	/// </summary>
	[Notify]
	public partial double OnDemandUsagePercent { get; set; }

	/// <summary>
	/// Ahead of pace / On pace / Behind pace for credit allowance.
	/// </summary>
	[Notify]
	public partial string PaceLabel { get; set; }

	/// <summary>
	/// Tooltip for <see cref="PaceLabel" /> (used vs calendar-linear target).
	/// </summary>
	[Notify]
	public partial string PaceLabelToolTip { get; set; }

	/// <summary>
	/// Humanized remaining time until period end.
	/// </summary>
	[Notify]
	public partial string PeriodRemainingText { get; set; }

	[Notify]
	public partial string PeriodType { get; set; }

	/// <summary>
	/// Selected billing/usage period; setting publishes SelectPeriod (when not projecting).
	/// </summary>
	[Notify]
	public partial GrokUsagePeriodViewModel SelectedPeriod { get; set; }

	/// <summary>
	/// Currently selected session row in the Sessions grid (context menu / open commands).
	/// </summary>
	[Notify]
	public partial GrokSessionRowViewModel SelectedSession { get; set; }

	[Notify]
	public partial bool SessionTokenHeatEnabled { get; set; }

	[Notify]
	public partial long SessionTokenHeatHotTokens { get; set; }

	[Notify]
	public partial long SessionTokenHeatSoftTokens { get; set; }

	/// <summary>
	/// Sessions for the selected home.
	/// </summary>
	public PresentationList<GrokSessionRowViewModel> Sessions { get; }

	/// <summary>
	/// Row selection for the Sessions TreeDataGrid.
	/// </summary>
	public TreeDataGridRowSelectionModel<GrokSessionRowViewModel> SessionsSelection { get; }

	/// <summary>
	/// Flat TreeDataGrid source for the Sessions list.
	/// </summary>
	public FlatTreeDataGridSource<GrokSessionRowViewModel> SessionsSource { get; }

	[Notify]
	public partial string StatusText { get; set; }

	/// <summary>
	/// Tooltip for token burn (24h).
	/// </summary>
	[Notify]
	public partial string TokenBurn24hToolTip { get; set; }

	/// <summary>
	/// Tooltip for token burn (period).
	/// </summary>
	[Notify]
	public partial string TokenBurnPeriodToolTip { get; set; }

	/// <summary>
	/// Subtitle under token totals (selected period date range).
	/// </summary>
	[Notify]
	public partial string TokenTotalsPeriodLabel { get; set; }

	/// <summary>
	/// Tooltip for credit exhaustion ETA.
	/// </summary>
	[Notify]
	public partial string UsageEtaToolTip { get; set; }

	/// <summary>
	/// Linear ETA text (includes Estimate · linear prefix when known).
	/// </summary>
	[Notify]
	public partial string UsageExhaustionText { get; set; }

	/// <summary>
	/// Tooltip for credits percent per hour.
	/// </summary>
	[Notify]
	public partial string UsagePercentPerHourToolTip { get; set; }

	/// <summary>
	/// How credit rate was derived (billing history / period average).
	/// </summary>
	/// <summary>
	/// Tooltip for weekly credit usage percent.
	/// </summary>
	[Notify]
	public partial string UsageToolTip { get; set; }

	/// <summary>
	/// Scrubber position 0…1 over [period start, live max]. Setting publishes SetViewAsOf / SetViewLive.
	/// </summary>
	[Notify]
	public partial DateTimeOffset ViewAsOf { get; set; }

	/// <summary>
	/// Humanized view clock (As of … / Live).
	/// </summary>
	[Notify]
	public partial string ViewAsOfText { get; set; }

	[Notify]
	public partial double ViewClockProgress { get; set; }

	#endregion

	#region Methods

	public bool CanOpenEvents(object parameter)
	{
		return parameter is GrokSessionRowViewModel session
			&& !string.IsNullOrEmpty(session.EventsPath)
			&& File.Exists(session.EventsPath);
	}

	public bool CanOpenSessionFolder(object parameter)
	{
		return parameter is GrokSessionRowViewModel session
			&& !string.IsNullOrEmpty(session.SessionDirectory)
			&& Directory.Exists(session.SessionDirectory);
	}

	public bool CanOpenSummary(object parameter)
	{
		return parameter is GrokSessionRowViewModel session
			&& !string.IsNullOrEmpty(session.SummaryPath)
			&& File.Exists(session.SummaryPath);
	}

	public bool CanOpenWorkingDirectory(object parameter)
	{
		return parameter is GrokSessionRowViewModel session
			&& !string.IsNullOrEmpty(session.WorkingDirectory)
			&& Directory.Exists(session.WorkingDirectory);
	}

	public bool CanToggleReplay()
	{
		return !_isDesignSample && HasViewClock;
	}

	public override void InitializeLifecycle()
	{
		WireDispatchTracks();
		ApplyModelChanges();
		base.InitializeLifecycle();
	}

	[RelayCommand(CanExecuteMethod = nameof(CanOpenEvents))]
	public void OpenEvents(object parameter)
	{
		if (parameter is not GrokSessionRowViewModel session || !CanOpenEvents(session))
		{
			return;
		}

		OpenPathInShell(session.EventsPath);
	}

	[RelayCommand(CanExecuteMethod = nameof(CanOpenSessionFolder))]
	public void OpenSessionFolder(object parameter)
	{
		if (parameter is not GrokSessionRowViewModel session || !CanOpenSessionFolder(session))
		{
			return;
		}

		OpenPathInShell(session.SessionDirectory);
	}

	[RelayCommand(CanExecuteMethod = nameof(CanOpenSummary))]
	public void OpenSummary(object parameter)
	{
		if (parameter is not GrokSessionRowViewModel session || !CanOpenSummary(session))
		{
			return;
		}

		OpenPathInShell(session.SummaryPath);
	}

	[RelayCommand(CanExecuteMethod = nameof(CanOpenWorkingDirectory))]
	public void OpenWorkingDirectory(object parameter)
	{
		if (parameter is not GrokSessionRowViewModel session || !CanOpenWorkingDirectory(session))
		{
			return;
		}

		OpenPathInShell(session.WorkingDirectory);
	}

	/// <summary>
	/// Pause period replay (no-op when not playing).
	/// </summary>
	[RelayCommand]
	public void PauseReplay()
	{
		if (!_isDesignSample)
		{
			_bus.GrokUsage.StopReplay(HomeId);
		}
	}

	[RelayCommand]
	public void Refresh()
	{
		if (HomeId != Guid.Empty)
		{
			_bus.GrokUsage.RefreshHome(HomeId);
		}
		else
		{
			_bus.GrokUsage.RefreshAll();
		}
	}

	[RelayCommand]
	public void RefreshAll()
	{
		_bus.GrokUsage.RefreshAll();
	}

	/// <summary>
	/// User changed period selection in the UI.
	/// Compares against home state (not <see cref="SelectedPeriod" />), because the ComboBox
	/// already assigned SelectedPeriod before <see cref="OnPropertyChanged{T}" /> runs.
	/// </summary>
	public void SelectPeriod(GrokUsagePeriodViewModel period)
	{
		if (_isDesignSample || (period == null) || (HomeId == Guid.Empty))
		{
			return;
		}

		if ((SelectedPeriodStart == period.PeriodStart)
			&& (SelectedPeriodEnd == period.PeriodEnd))
		{
			return;
		}

		_bus.GrokUsage.SelectPeriod(HomeId, period.PeriodStart, period.PeriodEnd);
	}

	/// <summary>
	/// Start or pause period replay. From live end, restarts at period start.
	/// </summary>
	[RelayCommand(CanExecuteMethod = nameof(CanToggleReplay))]
	public void ToggleReplay()
	{
		if (_isDesignSample)
		{
			return;
		}

		if (IsReplayPlaying)
		{
			_bus.GrokUsage.StopReplay(HomeId);
			return;
		}

		_bus.GrokUsage.StartReplay(HomeId);
	}

	public override void UninitializeLifecycle()
	{
		if (!_isDesignSample)
		{
			_bus.GrokUsage.StopReplay(HomeId);
		}

		base.UninitializeLifecycle();
	}

	protected override void OnPropertyChanged<T>(string propertyName, T oldValue, T newValue)
	{
		base.OnPropertyChanged(propertyName, oldValue, newValue);

		// CanToggleReplay depends on HasViewClock; Avalonia only re-queries after CanExecuteChanged.
		if (propertyName == nameof(HasViewClock))
		{
			(ToggleReplayCommand as RelayCommand)?.Refresh();
		}
	}

	private void ClearAnalyticsProjection()
	{
		HasAnalytics = false;
		HasCreditUsage = false;
		HasUsageEstimate = false;
		HasOnDemandCap = false;
		TokenBurnPerHourLast24h = 0;
		TokenBurnPerHourPeriod = 0;
		UsagePercentPerHour = 0;
		LinearPacePercent = 0;
		OnDemandUsagePercent = 0;
		AnalyticsNote = string.Empty;
		UsageRateSource = string.Empty;
		UsageExhaustionText = string.Empty;
		PeriodRemainingText = string.Empty;
		PaceLabel = string.Empty;
		PaceLabelToolTip = string.Empty;
		LinearPaceToolTip = string.Empty;
		UsageToolTip = string.Empty;
		TokenBurn24hToolTip = string.Empty;
		TokenBurnPeriodToolTip = string.Empty;
		UsagePercentPerHourToolTip = string.Empty;
		UsageEtaToolTip = string.Empty;
		OnDemandToolTip = string.Empty;
		_dailyTokenDayCount = 0;
		_dailyUsageDayCount = 0;
		FillDailyTokensChart([]);
		FillDailyUsageChart([]);
	}

	/// <summary>
	/// Compact K/M/B display with numeric sort on the underlying count.
	/// </summary>
	private static TextColumn<GrokSessionRowViewModel, string> CompactCountColumn(
		string header,
		Expression<Func<GrokSessionRowViewModel, string>> display,
		Func<GrokSessionRowViewModel, long> sortKey,
		int widthPixels)
	{
		return new TextColumn<GrokSessionRowViewModel, string>(
			header,
			display,
			new GridLength(widthPixels, GridUnitType.Pixel),
			options: new TextColumnOptions<GrokSessionRowViewModel>
			{
				CompareAscending = (a, b) => sortKey(a).CompareTo(sortKey(b)),
				CompareDescending = (a, b) => sortKey(b).CompareTo(sortKey(a)),
				TextAlignment = TextAlignment.Right
			});
	}

	private static TextColumn<GrokSessionRowViewModel, string> CompactPercentColumn(
		string header,
		Expression<Func<GrokSessionRowViewModel, string>> display,
		Func<GrokSessionRowViewModel, double> sortKey,
		int widthPixels)
	{
		return new TextColumn<GrokSessionRowViewModel, string>(
			header,
			display,
			new GridLength(widthPixels, GridUnitType.Pixel),
			options: new TextColumnOptions<GrokSessionRowViewModel>
			{
				CompareAscending = (a, b) => sortKey(a).CompareTo(sortKey(b)),
				CompareDescending = (a, b) => sortKey(b).CompareTo(sortKey(a)),
				TextAlignment = TextAlignment.Right
			});
	}

	private void WireDispatchTracks()
	{
		TrackProperties(_grokUsage)
			.MapOneWay(nameof(GrokUsageState.LastError), nameof(LastError), (string v) => v ?? string.Empty);

		TrackProperties(_settings)
			.MapOneWay(nameof(AppSettings.SessionTokenHeatEnabled))
			.MapOneWay(nameof(AppSettings.SessionTokenHeatSoftTokens))
			.MapOneWay(nameof(AppSettings.SessionTokenHeatHotTokens));

		var home = _home;

		// Shared IGrokHomeUsage scalars (get-only → one-way). PeriodType is display-formatted.
		TrackProperties<IGrokHomeUsage>(home, this)
			.MapOneWay<string, string>(nameof(GrokHomeUsageState.PeriodType), nameof(PeriodType), GrokUsageAnalytics.FormatPeriodTypeDisplay);

		TrackCollection(
			home.Sessions,
			Sessions,
			(item, dest) => string.Equals(item.SessionId, dest.SessionId, StringComparison.Ordinal),
			_ => new GrokSessionRowViewModel(),
			(dest, item) => dest.UpdateWith(item),
			_ => { });
		TrackCollection(
			home.AvailablePeriods,
			AvailablePeriods,
			(item, dest) => (item.PeriodStart == dest.PeriodStart) && (item.PeriodEnd == dest.PeriodEnd),
			_ => new GrokUsagePeriodViewModel(),
			(dest, item) =>
			{
				dest.UpdateWith(item);
				dest.DisplayName = GrokUsageAnalytics.FormatPeriodDisplayName(
					item.PeriodStart,
					item.PeriodEnd,
					item.IsCurrent,
					item.PeriodType);
			},
			_ => { });
		TrackBinding(home.DailyTokenTotals, () => FillDailyTokensChart(home.DailyTokenTotals));
		TrackBinding(home.DailyUsageTotals, () => FillDailyUsageChart(home.DailyUsageTotals));

		// Derived presentation (status, selected period, view clock, tooltips).
		// Must be last so property/list/chart tracks apply first in the same tick.
		TrackDerived(ProjectDerived);

		if (!_isDesignSample)
		{
			TrackIntent(nameof(SelectedPeriod), () => SelectPeriod(SelectedPeriod));
			TrackIntent(nameof(ViewClockProgress), OnViewClockProgressChanged);
		}
	}

	private void FillDailyTokensChart(IReadOnlyList<DailyTokenTotal> days)
	{
		// Snapshot SpeedyList so series build is stable.
		var snapshot = SnapshotList(days);
		_dailyTokenDayCount = snapshot.Count;
		var values = GrokUsageAnalytics.BuildDailyChartSeries(snapshot);

		// Runs inside ApplyModelChanges (AppDispatcher / UI). No extra IDispatcher.Dispatch.
		SeriesPresentation.Publish(
			values,
			DailyTokensChartData,
			series => DailyTokensChartData = series);
		DailyTokensChartCaption = GrokUsageAnalytics.BuildDailyChartCaption(snapshot);

		var cumulative = GrokUsageAnalytics.BuildDailyTokenTotalChartSeries(snapshot);
		SeriesPresentation.Publish(
			cumulative,
			DailyTokenTotalChartData,
			series => DailyTokenTotalChartData = series);
		DailyTokenTotalChartCaption = GrokUsageAnalytics.BuildDailyTokenTotalChartCaption(snapshot);
	}

	private void FillDailyUsageChart(IReadOnlyList<DailyUsageTotal> days)
	{
		var snapshot = SnapshotList(days);
		_dailyUsageDayCount = snapshot.Count;
		var dailyUsage = GrokUsageAnalytics.BuildDailyUsageChartSeries(snapshot);
		SeriesPresentation.Publish(
			dailyUsage,
			DailyUsageChartData,
			series => DailyUsageChartData = series);
		DailyUsageChartCaption = GrokUsageAnalytics.BuildDailyUsageChartCaption(snapshot);

		var cumulative = GrokUsageAnalytics.BuildDailyUsageTotalChartSeries(snapshot);
		SeriesPresentation.Publish(
			cumulative,
			DailyUsageTotalChartData,
			series => DailyUsageTotalChartData = series);
		DailyUsageTotalChartCaption = GrokUsageAnalytics.BuildDailyUsageTotalChartCaption(snapshot);
	}

	private static string FormatPaceLabel(double creditPercent, double linearPacePercent)
	{
		if (linearPacePercent <= 0)
		{
			return string.Empty;
		}

		const double margin = 5.0;
		if (creditPercent > (linearPacePercent + margin))
		{
			return "Ahead of pace";
		}

		if (creditPercent < (linearPacePercent - margin))
		{
			return "Behind pace";
		}

		return "On pace";
	}

	/// <summary>
	/// Explains Ahead / On / Behind relative to calendar-linear spend (within ±5%).
	/// </summary>
	private static string FormatPaceToolTip(double creditPercent, double linearPacePercent, string paceLabel)
	{
		if (linearPacePercent <= 0)
		{
			return "Pace compares usage to a straight-line burn across the period. "
				+ "Unavailable until a period length is known.";
		}

		var delta = creditPercent - linearPacePercent;
		var comparison = delta >= 0
			? $"{delta:0.#} percentage points above"
			: $"{Math.Abs(delta):0.#} percentage points below";

		var meaning = paceLabel switch
		{
			"Ahead of pace" =>
				"You are using the allowance faster than an even daily burn. "
				+ "At this rate you may run out before the period ends.",
			"Behind pace" =>
				"You are using the allowance slower than an even daily burn. "
				+ "You still have headroom relative to calendar time.",
			_ =>
				"Usage is within about 5 percentage points of an even daily burn."
		};

		return $"{paceLabel}: used {creditPercent:0.#}% vs linear target {linearPacePercent:0.#}% "
			+ $"({comparison}). {meaning}";
	}

	private static string FormatPeriodRemaining(DateTimeOffset periodEnd, DateTimeOffset asOf)
	{
		if (periodEnd == default)
		{
			return string.Empty;
		}

		var remaining = periodEnd - asOf;
		if (remaining.TotalSeconds <= 0)
		{
			return "Period ended";
		}

		if (remaining.TotalDays >= 1)
		{
			return $"Resets in {remaining.TotalDays:0.#}d";
		}

		if (remaining.TotalHours >= 1)
		{
			return $"Resets in {remaining.TotalHours:0.#}h";
		}

		return $"Resets in {Math.Max(1, remaining.TotalMinutes):0}m";
	}

	private static string FormatSpeed(double speed)
	{
		if (speed <= 0)
		{
			return "1×";
		}

		return speed == Math.Floor(speed)
			? $"{speed:0}×"
			: $"{speed:0.#}×";
	}

	private string FormatUsageExhaustion()
	{
		if (UsagePercent >= 100)
		{
			return "Usage exhausted for this period";
		}

		if (HasUsageEstimate && (EstimatedUsageExhaustionAt != default))
		{
			var source = string.IsNullOrEmpty(UsageRateSource)
				? string.Empty
				: $" · {UsageRateSource}";
			return $"Estimate · linear · {EstimatedUsageExhaustionAt:u}{source}";
		}

		if (!string.IsNullOrEmpty(AnalyticsNote))
		{
			return AnalyticsNote;
		}

		return "Insufficient data for estimate";
	}

	/// <summary>
	/// Slider moved: map 0…1 into the selected period and set the GrokUsage view clock.
	/// </summary>
	private void OnViewClockProgressChanged()
	{
		_bus.GrokUsage.StopReplay(HomeId);

		if (!TryGetProjectedViewClockRange(out var start, out var max))
		{
			return;
		}

		var progress = Math.Clamp(ViewClockProgress, 0, 1);
		if (progress >= 0.999)
		{
			_bus.GrokUsage.SetViewLive(HomeId);
			return;
		}

		var ticks = start.UtcTicks + (long) ((max.UtcTicks - start.UtcTicks) * progress);
		var asOf = new DateTimeOffset(ticks, TimeSpan.Zero);
		_bus.GrokUsage.SetViewAsOf(HomeId, asOf);
	}

	private static void OpenPathInShell(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}

		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = path,
				UseShellExecute = true
			});
		}
		catch
		{
			// Best-effort open in shell / explorer
		}
	}

	private void ProjectAnalytics()
	{
		HasOnDemandCap = OnDemandCap > 0;
		OnDemandUsagePercent = HasOnDemandCap
			? Math.Min(100, Math.Max(0, (100.0 * OnDemandUsed) / OnDemandCap))
			: 0;

		var viewNow = IsViewLive || (ViewAsOf == default)
			? new DateTimeOffset(DateTime.SpecifyKind(_dateTimeProvider.UtcNow, DateTimeKind.Utc))
			: ViewAsOf;
		PeriodRemainingText = FormatPeriodRemaining(PeriodEnd, viewNow);
		PaceLabel = FormatPaceLabel(UsagePercent, LinearPacePercent);
		UsageExhaustionText = FormatUsageExhaustion();
		ProjectAnalyticsToolTips();

		HasAnalytics = HasBilling
			|| (_dailyTokenDayCount > 0)
			|| (_dailyUsageDayCount > 0)
			|| (TokenBurnPerHourPeriod > 0)
			|| (TokenBurnPerHourLast24h > 0);
	}

	private void ProjectAnalyticsToolTips()
	{
		var used = UsagePercent;
		var linear = LinearPacePercent;

		UsageToolTip =
			"Share of your included weekly allowance already used (from Grok billing). "
			+ "100% means the plan pool is exhausted until the period resets.";

		LinearPaceToolTip = linear <= 0
			? "Linear pace needs a known billing period. It is the calendar share of the period that has elapsed, as a percent of 100."
			: $"If usage were even over the whole period, you would be at about {linear:0.#}% by now. "
			+ "This is only a time-based baseline — not how fast you actually used the allowance.";

		PaceLabelToolTip = FormatPaceToolTip(used, linear, PaceLabel);

		TokenBurn24hToolTip =
			$"Average subscription tokens per hour over the last 24 hours ({TokenBurnPerHourLast24h:N0}/h). "
			+ "Counts grok-* model inferences only.";

		TokenBurnPeriodToolTip =
			$"Average subscription tokens per hour so far in the selected period ({TokenBurnPerHourPeriod:N0}/h). "
			+ "Total period tokens divided by hours elapsed in the period.";

		var rateSource = string.IsNullOrEmpty(UsageRateSource)
			? "when enough billing history exists"
			: UsageRateSource;
		UsagePercentPerHourToolTip =
			$"Allowance used per hour ({UsagePercentPerHour:0.###} %/h), from {rateSource}. "
			+ "Used to project when you might hit 100% if the rate stays similar.";

		UsageEtaToolTip =
			"Projected time you would hit 100% usage at the current %/h rate. "
			+ "Linear estimate only — not a guarantee. Quiet hours or a burst will move it.";

		OnDemandToolTip = HasOnDemandCap
			? "On-demand spend against your configured cap after the included weekly allowance. "
			+ $"{OnDemandUsed:0.##} used of {OnDemandCap:0.##} cap."
			: "No on-demand cap was reported in billing snapshots for this home.";
	}

	private void ProjectDerived()
	{
		if (!HomeExists && string.IsNullOrEmpty(DisplayName) && string.IsNullOrEmpty(Path))
		{
			ClearAnalyticsProjection();
			ProjectViewClock();
			StatusText = "Grok home not found.";
			return;
		}

		ProjectSelectedPeriod();
		ProjectAnalytics();
		ProjectViewClock();
		ProjectStatus();
	}

	private void ProjectSelectedPeriod()
	{
		GrokUsagePeriodViewModel match = null;
		foreach (var period in AvailablePeriods)
		{
			if ((period.PeriodStart == SelectedPeriodStart)
				&& (period.PeriodEnd == SelectedPeriodEnd))
			{
				match = period;
				break;
			}
		}

		if ((match == null) && (AvailablePeriods.Count > 0))
		{
			foreach (var period in AvailablePeriods)
			{
				if (period.IsCurrent)
				{
					match = period;
					break;
				}
			}

			match ??= AvailablePeriods[0];
		}

		SelectedPeriod = match;
		TokenTotalsPeriodLabel = match == null
			? string.Empty
			: string.IsNullOrEmpty(match.DisplayName)
				? "Selected period"
				: match.DisplayName;
	}

	private void ProjectStatus()
	{
		if (IsBusy)
		{
			return;
		}

		if (!string.IsNullOrEmpty(ErrorText))
		{
			StatusText = ErrorText;
			return;
		}

		if (LastRefreshedAt != default)
		{
			var clockNote = IsViewLive
				? "Live"
				: $"As of {ViewAsOf.ToLocalTime():g}";
			if (IsReplayPlaying)
			{
				clockNote += $" · replay {FormatSpeed(GrokUsageState.ReplaySpeed)}";
			}

			StatusText = $"Updated {LastRefreshedAt:u} · {Sessions.Count} session(s) · {clockNote}";
			return;
		}

		StatusText = "Ready";
	}

	private void ProjectViewClock()
	{
		if ((PeriodStart == default)
			|| (PeriodEnd == default)
			|| (PeriodEnd <= PeriodStart))
		{
			HasViewClock = false;
			ViewClockProgress = 1;
			ViewAsOfText = string.Empty;
			return;
		}

		if (!TryGetProjectedViewClockRange(out var start, out var max))
		{
			HasViewClock = false;
			ViewClockProgress = 1;
			ViewAsOfText = IsViewLive ? "Live" : string.Empty;
			return;
		}

		HasViewClock = true;
		if (IsViewLive || (ViewAsOf == default))
		{
			ViewClockProgress = 1;
			ViewAsOfText = IsReplayPlaying
				? $"Live · {FormatSpeed(GrokUsageState.ReplaySpeed)}"
				: "Live";
			return;
		}

		var asOf = ViewAsOf;
		if (asOf < start)
		{
			asOf = start;
		}

		if (asOf > max)
		{
			asOf = max;
		}

		var span = max.UtcTicks - start.UtcTicks;
		ViewClockProgress = span <= 0
			? 1
			: Math.Clamp((double) (asOf.UtcTicks - start.UtcTicks) / span, 0, 1);
		var asOfLabel = $"As of {asOf.ToLocalTime():g}";
		ViewAsOfText = IsReplayPlaying
			? $"{asOfLabel} · {FormatSpeed(GrokUsageState.ReplaySpeed)}"
			: asOfLabel;
	}

	private void SessionsSelectionOnSelectionChanged(
		object sender,
		TreeSelectionModelSelectionChangedEventArgs<GrokSessionRowViewModel> e)
	{
		SelectedSession = SessionsSelection.SelectedItem;
	}

	private static IReadOnlyList<T> SnapshotList<T>(
		IReadOnlyList<T> source)
	{
		if ((source == null) || (source.Count == 0))
		{
			return [];
		}

		var copy = new T[source.Count];
		for (var i = 0; i < source.Count; i++)
		{
			copy[i] = source[i];
		}

		return copy;
	}

	private bool TryGetProjectedViewClockRange(out DateTimeOffset start, out DateTimeOffset max)
	{
		start = ViewClockStart;
		max = ViewClockMax;
		return (start != default) && (max != default) && (max > start);
	}

	#endregion
}