#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Cornerstone.Avalonia.TreeDataGrid;
using Cornerstone.Avalonia.TreeDataGrid.Columns;
using Cornerstone.Avalonia.TreeDataGrid.Selection;
using Cornerstone.Compare;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.GrokMonitor.GrokUsage.Models;
using Cornerstone.GrokMonitor.GrokUsage.State;
using Cornerstone.GrokMonitor.Keystone;
using Cornerstone.GrokMonitor.Keystone.State;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using IDispatcher = Cornerstone.Presentation.IDispatcher;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage;

/// <summary>
/// Dashboard page for one Grok home (Personal or Work).
/// Projects that home from <see cref="AppState.GrokUsage" /> via AppDispatcher; publishes refresh/period intent only.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
public partial class GrokUsageTabViewModel : DispatchableViewModel, IShellTab
{
	#region Constants

	/// <summary> Simulated-time multiplier while replaying (wall seconds × this). </summary>
	private const double ReplaySpeed = 1000;

	/// <summary> UI updates per second while replaying (soft reproject cadence). </summary>
	private const double ReplayTicksPerSecond = 10;

	#endregion

	#region Fields

	private static readonly GenericEqualityComparer<GrokUsagePeriodState> PeriodComparer;
	private static readonly GenericEqualityComparer<GrokSessionUsageState> SessionComparer;

	private readonly AppBus _bus;
	private bool _isDesignSample;
	private bool _isProjectingPeriod;
	private bool _isProjectingViewClock;
	private bool _needsProjectionSeed = true;
	private DispatcherTimer _replayTimer;
	private readonly Stopwatch _replayWallClock = new();
	private readonly AppState _state;
	private bool _tracksWired;

	#endregion

	#region Constructors

	public GrokUsageTabViewModel(
		AppBus bus,
		AppState state,
		IDispatcher dispatcher,
		Guid homeId)
	{
		_bus = bus;
		_state = state;
		HomeId = homeId;

		Sessions = new PresentationList<GrokSessionUsageState>(dispatcher);
		AvailablePeriods = new PresentationList<GrokUsagePeriodState>(dispatcher);

		SessionsSource = new FlatTreeDataGridSource<GrokSessionUsageState>(Sessions)
		{
			Columns =
			{
				new TextColumn<GrokSessionUsageState, string>("Title", x => x.Title, new GridLength(2, GridUnitType.Star)),
				new TextColumn<GrokSessionUsageState, string>("Directory", x => x.WorkingDirectory, new GridLength(1, GridUnitType.Star)),
				new TextColumn<GrokSessionUsageState, string>("Model", x => x.CurrentModelId, new GridLength(1, GridUnitType.Star)),
				CompactCountColumn("Msgs", x => GrokUsageAnalytics.FormatCompactTokens(x.MessageCount), x => x.MessageCount, 70),
				CompactCountColumn("Inf", x => GrokUsageAnalytics.FormatCompactTokens(x.InferenceCount), x => x.InferenceCount, 50),
				CompactCountColumn("Prompt", x => GrokUsageAnalytics.FormatCompactTokens(x.PromptTokens), x => x.PromptTokens, 80),
				CompactCountColumn("Cached", x => GrokUsageAnalytics.FormatCompactTokens(x.CachedPromptTokens), x => x.CachedPromptTokens, 80),
				CompactCountColumn("Completion", x => GrokUsageAnalytics.FormatCompactTokens(x.CompletionTokens), x => x.CompletionTokens, 90),
				CompactCountColumn("Reasoning", x => GrokUsageAnalytics.FormatCompactTokens(x.ReasoningTokens), x => x.ReasoningTokens, 90),
				CompactCountColumn("Total", x => GrokUsageAnalytics.FormatCompactTokens(x.TotalTokens), x => x.TotalTokens, 80),
				CompactPercentColumn("Usage", x => GrokUsageAnalytics.FormatAllocatedUsagePercent(x.UsagePercent, x.HasAllocatedUsage), x => x.UsagePercent, 80),
				new TextColumn<GrokSessionUsageState, string>("Last", x => x.LastInferenceAtStr, new GridLength(180, GridUnitType.Pixel))
			}
		};

		SessionsSelection = new TreeDataGridRowSelectionModel<GrokSessionUsageState>(SessionsSource) { SingleSelect = true };
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

		// Seed tab title from state when the home already exists.
		var existing = _state.GrokUsage.FindById(homeId);
		if (existing != null)
		{
			DisplayName = existing.DisplayName ?? string.Empty;
			Path = existing.Path ?? string.Empty;
		}
	}

	static GrokUsageTabViewModel()
	{
		PeriodComparer = new GenericEqualityComparer<GrokUsagePeriodState>(
			(x, y) => (x != null) && (y != null)
				&& (x.PeriodStart == y.PeriodStart)
				&& (x.PeriodEnd == y.PeriodEnd),
			x => HashCode.Combine(x?.PeriodStart, x?.PeriodEnd));
		SessionComparer = new GenericEqualityComparer<GrokSessionUsageState>(
			(x, y) => (x != null) && (y != null)
				&& string.Equals(x.SessionId, y.SessionId, StringComparison.Ordinal),
			x => x?.SessionId?.GetHashCode(StringComparison.Ordinal) ?? 0);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Note when credit ETA cannot be formed.
	/// </summary>
	[Notify]
	public partial string AnalyticsNote { get; set; }

	/// <summary>
	/// Billing periods for the period dropdown (selected home).
	/// </summary>
	public PresentationList<GrokUsagePeriodState> AvailablePeriods { get; }

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
	public partial string DisplayName { get; set; }

	[Notify]
	public partial string ErrorText { get; set; }

	[Notify]
	public partial long GrandTotalCachedPromptTokens { get; set; }

	[Notify]
	public partial long GrandTotalCompletionTokens { get; set; }

	[Notify]
	public partial long GrandTotalPromptTokens { get; set; }

	[Notify]
	public partial long GrandTotalReasoningTokens { get; set; }

	[Notify]
	public partial long GrandTotalTokens { get; set; }

	[Notify]
	public partial bool HasAnalytics { get; set; }

	[Notify]
	public partial bool HasBilling { get; set; }

	/// <summary>
	/// True when credit-allowance percent is reported (SuperGrok-style). Hides usage gauges/charts when false.
	/// </summary>
	[Notify]
	public partial bool HasCreditUsage { get; set; }

	[Notify]
	public partial bool HasOnDemandCap { get; set; }

	[Notify]
	public partial bool HasUsageEstimate { get; set; }

	/// <summary>
	/// True when a period range exists so the view-clock scrubber can be shown.
	/// </summary>
	[Notify]
	public partial bool HasViewClock { get; set; }

	[Notify]
	public partial bool HomeExists { get; set; }

	/// <summary>
	/// Fixed Grok home this dashboard projects (does not change after construction).
	/// </summary>
	public Guid HomeId { get; }

	[Notify]
	public partial bool IsBusy { get; set; }

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

	[Notify]
	public partial DateTimeOffset LastRefreshedAt { get; set; }

	[Notify]
	public partial double LinearPacePercent { get; set; }

	/// <summary>
	/// Tooltip for the linear pace percent line.
	/// </summary>
	[Notify]
	public partial string LinearPaceToolTip { get; set; }

	[Notify]
	public partial double OnDemandCap { get; set; }

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

	[Notify]
	public partial double OnDemandUsed { get; set; }

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

	[Notify]
	public partial string Path { get; set; }

	[Notify]
	public partial DateTimeOffset PeriodEnd { get; set; }

	/// <summary>
	/// Humanized remaining time until period end.
	/// </summary>
	[Notify]
	public partial string PeriodRemainingText { get; set; }

	[Notify]
	public partial DateTimeOffset PeriodStart { get; set; }

	[Notify]
	public partial string PeriodType { get; set; }

	[Notify]
	public partial double PrepaidBalance { get; set; }

	[Notify]
	public partial string ProgressText { get; set; }

	/// <summary>
	/// Selected billing/usage period; setting publishes SelectPeriod (when not projecting).
	/// </summary>
	[Notify]
	public partial GrokUsagePeriodState SelectedPeriod { get; set; }

	/// <summary>
	/// Currently selected session row in the Sessions grid (context menu / open commands).
	/// </summary>
	[Notify]
	public partial GrokSessionUsageState SelectedSession { get; set; }

	/// <summary>
	/// Sessions for the selected home.
	/// </summary>
	public PresentationList<GrokSessionUsageState> Sessions { get; }

	/// <summary>
	/// Row selection for the Sessions TreeDataGrid.
	/// </summary>
	public TreeDataGridRowSelectionModel<GrokSessionUsageState> SessionsSelection { get; }

	/// <summary>
	/// Flat TreeDataGrid source for the Sessions list.
	/// </summary>
	public FlatTreeDataGridSource<GrokSessionUsageState> SessionsSource { get; }

	/// <summary>
	/// Shell settings (session heat thresholds, theme). Used by Sessions row heat bindings.
	/// </summary>
	public AppSettings Settings => _state.Settings;

	[Notify]
	public partial string StatusText { get; set; }

	[Notify]
	public partial string SubscriptionTier { get; set; }

	/// <summary>
	/// Tooltip for token burn (24h).
	/// </summary>
	[Notify]
	public partial string TokenBurn24hToolTip { get; set; }

	[Notify]
	public partial double TokenBurnPerHourLast24h { get; set; }

	[Notify]
	public partial double TokenBurnPerHourPeriod { get; set; }

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

	[Notify]
	public partial double UsagePercent { get; set; }

	[Notify]
	public partial double UsagePercentPerHour { get; set; }

	/// <summary>
	/// Tooltip for credits percent per hour.
	/// </summary>
	[Notify]
	public partial string UsagePercentPerHourToolTip { get; set; }

	/// <summary>
	/// How credit rate was derived (billing history / period average).
	/// </summary>
	[Notify]
	public partial string UsageRateSource { get; set; }

	/// <summary>
	/// Tooltip for weekly credit usage percent.
	/// </summary>
	[Notify]
	public partial string UsageToolTip { get; set; }

	/// <summary>
	/// Humanized view clock (As of … / Live).
	/// </summary>
	[Notify]
	public partial string ViewAsOfText { get; set; }

	/// <summary>
	/// Scrubber position 0…1 over [period start, live max]. Setting publishes SetViewAsOf / SetViewLive.
	/// </summary>
	[Notify]
	public partial double ViewClockProgress { get; set; }

	#endregion

	#region Methods

	public override void ApplyModelChanges()
	{
		base.ApplyModelChanges();
		ProjectHome();
		_needsProjectionSeed = false;
	}

	public bool CanOpenEvents(object parameter)
	{
		return parameter is GrokSessionUsageState session
			&& !string.IsNullOrEmpty(session.EventsPath)
			&& File.Exists(session.EventsPath);
	}

	public bool CanOpenSessionFolder(object parameter)
	{
		return parameter is GrokSessionUsageState session
			&& !string.IsNullOrEmpty(session.SessionDirectory)
			&& Directory.Exists(session.SessionDirectory);
	}

	public bool CanOpenSummary(object parameter)
	{
		return parameter is GrokSessionUsageState session
			&& !string.IsNullOrEmpty(session.SummaryPath)
			&& File.Exists(session.SummaryPath);
	}

	public bool CanOpenWorkingDirectory(object parameter)
	{
		return parameter is GrokSessionUsageState session
			&& !string.IsNullOrEmpty(session.WorkingDirectory)
			&& Directory.Exists(session.WorkingDirectory);
	}

	public bool CanToggleReplay()
	{
		return !_isDesignSample && HasViewClock && !IsBusy;
	}

	/// <summary>
	/// Design-time / preview sample: one home with billing, analytics, and sessions.
	/// Skips disk refresh in <see cref="InitializeLifecycle" /> so sample data is not wiped.
	/// </summary>
	public static GrokUsageTabViewModel CreateDesignSample(AppBus bus, AppState state, IDispatcher dispatcher)
	{
		var now = DateTimeOffset.UtcNow;
		var periodStart = now.Date.AddDays(-((int) now.DayOfWeek + 6) % 7); // Monday of current week (UTC date)
		var periodEnd = periodStart.AddDays(7);
		var periodStartOffset = new DateTimeOffset(periodStart, TimeSpan.Zero);
		var periodEndOffset = new DateTimeOffset(periodEnd, TimeSpan.Zero);

		var homeId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
		var home = CreatePrimaryHomeSample(homeId, now, periodStartOffset, periodEndOffset);

		state.GrokUsage.Homes.Clear();
		state.GrokUsage.Homes.Add(home);
		state.GrokUsage.SelectedHomeId = home.Id;
		state.GrokUsage.LastError = string.Empty;

		var sample = new GrokUsageTabViewModel(bus, state, dispatcher, homeId);
		sample._isDesignSample = true;

		// Populate presentation fields immediately for the previewer.
		sample.ProjectHome();
		sample._needsProjectionSeed = false;
		return sample;
	}

	public override bool HasModelChanges()
	{
		if (_needsProjectionSeed || base.HasModelChanges())
		{
			return true;
		}

		return HomeHasPending();
	}

	public override void InitializeLifecycle()
	{
		EnsureTracks();
		if (!_isDesignSample)
		{
			_needsProjectionSeed = true;
		}

		base.InitializeLifecycle();
	}

	[RelayCommand(CanExecuteMethod = nameof(CanOpenEvents))]
	public void OpenEvents(object parameter)
	{
		if (parameter is not GrokSessionUsageState session || !CanOpenEvents(session))
		{
			return;
		}

		OpenPathInShell(session.EventsPath);
	}

	[RelayCommand(CanExecuteMethod = nameof(CanOpenSessionFolder))]
	public void OpenSessionFolder(object parameter)
	{
		if (parameter is not GrokSessionUsageState session || !CanOpenSessionFolder(session))
		{
			return;
		}

		OpenPathInShell(session.SessionDirectory);
	}

	[RelayCommand(CanExecuteMethod = nameof(CanOpenSummary))]
	public void OpenSummary(object parameter)
	{
		if (parameter is not GrokSessionUsageState session || !CanOpenSummary(session))
		{
			return;
		}

		OpenPathInShell(session.SummaryPath);
	}

	[RelayCommand(CanExecuteMethod = nameof(CanOpenWorkingDirectory))]
	public void OpenWorkingDirectory(object parameter)
	{
		if (parameter is not GrokSessionUsageState session || !CanOpenWorkingDirectory(session))
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
		StopReplay();
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

		_needsProjectionSeed = true;
	}

	[RelayCommand]
	public void RefreshAll()
	{
		_bus.GrokUsage.RefreshAll();
		_needsProjectionSeed = true;
	}

	/// <summary>
	/// User changed period selection in the UI.
	/// Compares against home state (not <see cref="SelectedPeriod" />), because the ComboBox
	/// already assigned SelectedPeriod before <see cref="OnPropertyChanged{T}" /> runs.
	/// </summary>
	public void SelectPeriod(GrokUsagePeriodState period)
	{
		if (_isDesignSample || _isProjectingPeriod || (period == null) || (HomeId == Guid.Empty))
		{
			return;
		}

		var home = _state.GrokUsage.FindById(HomeId);
		if ((home != null)
			&& (home.SelectedPeriodStart == period.PeriodStart)
			&& (home.SelectedPeriodEnd == period.PeriodEnd))
		{
			return;
		}

		StopReplay();
		_bus.GrokUsage.SelectPeriod(HomeId, period.PeriodStart, period.PeriodEnd);
		_needsProjectionSeed = true;
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
			StopReplay();
			return;
		}

		if (!TryGetViewClockRange(out var start, out var max))
		{
			return;
		}

		// Replay a full pass when already at the live end.
		if (IsViewLive || (ViewClockProgress >= 0.999))
		{
			_bus.GrokUsage.SetViewAsOf(start);
			_needsProjectionSeed = true;
		}

		IsReplayPlaying = true;
		_replayWallClock.Restart();
		EnsureReplayTimer().Start();
		_needsProjectionSeed = true;
	}

	public override void UninitializeLifecycle()
	{
		StopReplay();
		base.UninitializeLifecycle();
	}

	protected override void OnPropertyChanged<T>(string propertyName, T oldValue, T newValue)
	{
		base.OnPropertyChanged(propertyName, oldValue, newValue);

		if ((propertyName == nameof(SelectedPeriod)) && newValue is GrokUsagePeriodState period)
		{
			SelectPeriod(period);
		}

		if ((propertyName == nameof(ViewClockProgress)) && !_isProjectingViewClock && !_isDesignSample)
		{
			OnViewClockProgressChanged();
		}

		// CanToggleReplay depends on HasViewClock / IsBusy; Avalonia only re-queries after CanExecuteChanged.
		if ((propertyName == nameof(HasViewClock)) || (propertyName == nameof(IsBusy)))
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
		FillDailyTokensChart([]);
		FillDailyUsageChart([]);
	}

	/// <summary>
	/// Compact K/M/B display with numeric sort on the underlying count.
	/// </summary>
	private static TextColumn<GrokSessionUsageState, string> CompactCountColumn(
		string header,
		Expression<Func<GrokSessionUsageState, string>> display,
		Func<GrokSessionUsageState, long> sortKey,
		int widthPixels)
	{
		return new TextColumn<GrokSessionUsageState, string>(
			header,
			display,
			new GridLength(widthPixels, GridUnitType.Pixel),
			options: new TextColumnOptions<GrokSessionUsageState>
			{
				CompareAscending = (a, b) => sortKey(a).CompareTo(sortKey(b)),
				CompareDescending = (a, b) => sortKey(b).CompareTo(sortKey(a)),
				TextAlignment = TextAlignment.Right
			});
	}

	private static TextColumn<GrokSessionUsageState, string> CompactPercentColumn(
		string header,
		Expression<Func<GrokSessionUsageState, string>> display,
		Func<GrokSessionUsageState, double> sortKey,
		int widthPixels)
	{
		return new TextColumn<GrokSessionUsageState, string>(
			header,
			display,
			new GridLength(widthPixels, GridUnitType.Pixel),
			options: new TextColumnOptions<GrokSessionUsageState>
			{
				CompareAscending = (a, b) => sortKey(a).CompareTo(sortKey(b)),
				CompareDescending = (a, b) => sortKey(b).CompareTo(sortKey(a)),
				TextAlignment = TextAlignment.Right
			});
	}

	private static GrokHomeUsageState CreatePrimaryHomeSample(
		Guid id,
		DateTimeOffset now,
		DateTimeOffset periodStart,
		DateTimeOffset periodEnd)
	{
		var home = new GrokHomeUsageState(id)
		{
			DisplayName = GrokPaths.PrimaryHomeDisplayName,
			Path = @"C:\Users\Ada\.grok",
			HomeExists = true,
			IsBusy = false,
			ProgressText = string.Empty,
			ErrorText = string.Empty,
			HasBilling = true,
			HasCreditUsage = true,
			SubscriptionTier = "SuperGrok",
			UsagePercent = 62.4,
			PeriodType = "weekly",
			PeriodStart = periodStart,
			PeriodEnd = periodEnd,
			OnDemandCap = 50,
			OnDemandUsed = 12.75,
			PrepaidBalance = 0,
			GrandTotalPromptTokens = 1_284_500,
			GrandTotalCachedPromptTokens = 412_200,
			GrandTotalCompletionTokens = 318_750,
			GrandTotalReasoningTokens = 96_400,
			GrandTotalTokens = 1_603_250,
			LastRefreshedAt = now.AddMinutes(-4),
			TokenBurnPerHourLast24h = 48_200,
			TokenBurnPerHourPeriod = 31_500,
			UsagePercentPerHour = 0.42,
			LinearPacePercent = 55.0,
			HasUsageEstimate = true,
			UsageRateSource = "billing history",
			EstimatedUsageExhaustionAt = now.AddHours(90),
			AnalyticsNote = string.Empty,
			SelectedPeriodStart = periodStart,
			SelectedPeriodEnd = periodEnd
		};

		var prevStart = periodStart.AddDays(-7);
		var prevEnd = periodStart;
		home.AvailablePeriods.Load(
		[
			new GrokUsagePeriodState
			{
				PeriodStart = periodStart,
				PeriodEnd = periodEnd,
				PeriodType = "weekly",
				IsCurrent = true,
				DisplayName = GrokUsageAnalytics.FormatPeriodDisplayName(periodStart, periodEnd, true)
			},
			new GrokUsagePeriodState
			{
				PeriodStart = prevStart,
				PeriodEnd = prevEnd,
				PeriodType = "weekly",
				IsCurrent = false,
				DisplayName = GrokUsageAnalytics.FormatPeriodDisplayName(prevStart, prevEnd, false)
			}
		]);

		// 7 local days of token totals for the current week.
		var today = DateTime.Today;
		var daily = new DailyTokenTotal[7];
		long[] dayTokens =
		[
			51_300, 67_900, 142_000, 155_600, 88_200, 72_400, 61_800
		];
		for (var i = 0; i < daily.Length; i++)
		{
			daily[i] = new DailyTokenTotal
			{
				Day = today.AddDays(i - (daily.Length - 1)),
				TotalTokens = dayTokens[i]
			};
		}

		home.DailyTokenTotals.Load(daily);

		// Usage per day: quiet start, one heavy day (~+27 pts), then small burns.
		double[] usageEnds = [42, 48, 75, 82, 88, 91, 62.4];
		double[] usageDeltas = [7, 6, 27, 7, 6, 3, 2];
		var dailyUsage = new DailyUsageTotal[7];
		for (var i = 0; i < dailyUsage.Length; i++)
		{
			dailyUsage[i] = new DailyUsageTotal
			{
				Day = today.AddDays(i - (dailyUsage.Length - 1)),
				EndOfDayPercent = usageEnds[i],
				DailyDelta = usageDeltas[i],
				HasSnapshot = true
			};
		}

		home.DailyUsageTotals.Load(dailyUsage);

		home.Sessions.Load(
		[
			new GrokSessionUsageState
			{
				SessionId = "sess-personal-001",
				Title = "Wire Grok usage dashboard",
				WorkingDirectory = @"C:\Workspaces\MyApp",
				CurrentModelId = "grok-4",
				MessageCount = 48,
				InferenceCount = 36,
				PromptTokens = 520_100,
				CachedPromptTokens = 180_400,
				CompletionTokens = 112_300,
				ReasoningTokens = 41_200,
				TotalTokens = 632_400,
				HasAllocatedUsage = true,
				UsagePercent = GrokUsageAnalytics.AllocateSessionUsagePercent(632_400, 1_603_250, 62.4),
				FirstInferenceAt = now.AddDays(-3).AddHours(-2),
				LastInferenceAt = now.AddMinutes(-12),
				SessionDirectory = @"C:\Users\Ada\.grok\sessions\sess-personal-001",
				SummaryPath = @"C:\Users\Ada\.grok\sessions\sess-personal-001\summary.json",
				EventsPath = @"C:\Users\Ada\.grok\sessions\sess-personal-001\events.jsonl"
			},
			new GrokSessionUsageState
			{
				SessionId = "sess-personal-002",
				Title = "Refactor TreeDataGrid selection",
				WorkingDirectory = @"C:\Workspaces\MyApp\Client",
				CurrentModelId = "grok-4",
				MessageCount = 22,
				InferenceCount = 18,
				PromptTokens = 310_200,
				CachedPromptTokens = 95_100,
				CompletionTokens = 78_400,
				ReasoningTokens = 22_800,
				TotalTokens = 388_600,
				HasAllocatedUsage = true,
				UsagePercent = GrokUsageAnalytics.AllocateSessionUsagePercent(388_600, 1_603_250, 62.4),
				FirstInferenceAt = now.AddDays(-5),
				LastInferenceAt = now.AddHours(-6),
				SessionDirectory = @"C:\Users\Ada\.grok\sessions\sess-personal-002",
				SummaryPath = @"C:\Users\Ada\.grok\sessions\sess-personal-002\summary.json",
				EventsPath = @"C:\Users\Ada\.grok\sessions\sess-personal-002\events.jsonl"
			},
			new GrokSessionUsageState
			{
				SessionId = "sess-personal-003",
				Title = "Design review notes",
				WorkingDirectory = @"C:\Workspaces\Docs",
				CurrentModelId = "grok-3-mini",
				MessageCount = 9,
				InferenceCount = 7,
				PromptTokens = 84_500,
				CachedPromptTokens = 12_200,
				CompletionTokens = 31_050,
				ReasoningTokens = 0,
				TotalTokens = 115_550,
				HasAllocatedUsage = true,
				UsagePercent = GrokUsageAnalytics.AllocateSessionUsagePercent(115_550, 1_603_250, 62.4),
				FirstInferenceAt = now.AddDays(-8),
				LastInferenceAt = now.AddDays(-1).AddHours(-3),
				SessionDirectory = @"C:\Users\Ada\.grok\sessions\sess-personal-003",
				SummaryPath = @"C:\Users\Ada\.grok\sessions\sess-personal-003\summary.json",
				EventsPath = @"C:\Users\Ada\.grok\sessions\sess-personal-003\events.jsonl"
			},
			new GrokSessionUsageState
			{
				// Summary-only session: no inference_done rows yet (expected zeros).
				SessionId = "sess-personal-004",
				Title = "Untitled session",
				WorkingDirectory = @"C:\Workspaces\Scratch",
				CurrentModelId = string.Empty,
				MessageCount = 2,
				InferenceCount = 0,
				PromptTokens = 0,
				CachedPromptTokens = 0,
				CompletionTokens = 0,
				ReasoningTokens = 0,
				TotalTokens = 0,
				HasAllocatedUsage = true,
				UsagePercent = 0,
				FirstInferenceAt = default,
				LastInferenceAt = default,
				SessionDirectory = @"C:\Users\Ada\.grok\sessions\sess-personal-004",
				SummaryPath = @"C:\Users\Ada\.grok\sessions\sess-personal-004\summary.json",
				EventsPath = string.Empty
			},
			new GrokSessionUsageState
			{
				SessionId = "sess-personal-005",
				Title = "PowerShell host diagnostics",
				WorkingDirectory = @"C:\Workspaces\MyApp",
				CurrentModelId = "grok-4",
				MessageCount = 15,
				InferenceCount = 11,
				PromptTokens = 198_700,
				CachedPromptTokens = 64_300,
				CompletionTokens = 45_200,
				ReasoningTokens = 18_400,
				TotalTokens = 243_900,
				HasAllocatedUsage = true,
				UsagePercent = GrokUsageAnalytics.AllocateSessionUsagePercent(243_900, 1_603_250, 62.4),
				FirstInferenceAt = now.AddDays(-2),
				LastInferenceAt = now.AddHours(-1),
				SessionDirectory = @"C:\Users\Ada\.grok\sessions\sess-personal-005",
				SummaryPath = @"C:\Users\Ada\.grok\sessions\sess-personal-005\summary.json",
				EventsPath = @"C:\Users\Ada\.grok\sessions\sess-personal-005\events.jsonl"
			}
		]);

		return home;
	}

	private DispatcherTimer EnsureReplayTimer()
	{
		if (_replayTimer != null)
		{
			return _replayTimer;
		}

		_replayTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(1.0 / ReplayTicksPerSecond)
		};
		_replayTimer.Tick += ReplayTimerOnTick;
		return _replayTimer;
	}

	private void EnsureTracks()
	{
		if (_tracksWired)
		{
			return;
		}

		TrackProperties(_state.GrokUsage)
			.MapOneWay(nameof(GrokUsageState.LastError), nameof(LastError), (string v) => v ?? string.Empty)
			.MapOneWay(nameof(GrokUsageState.IsViewLive), nameof(IsViewLive), (bool v) => v);

		_tracksWired = true;
	}

	private void FillDailyTokensChart(IReadOnlyList<DailyTokenTotal> days)
	{
		// Snapshot SpeedyList so series build is stable.
		var snapshot = SnapshotList(days);
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

	private static string FormatUsageExhaustion(GrokHomeUsageState home)
	{
		if (home.UsagePercent >= 100)
		{
			return "Usage exhausted for this period";
		}

		if (home.HasUsageEstimate && (home.EstimatedUsageExhaustionAt != default))
		{
			var source = string.IsNullOrEmpty(home.UsageRateSource)
				? string.Empty
				: $" · {home.UsageRateSource}";
			return $"Estimate · linear · {home.EstimatedUsageExhaustionAt:u}{source}";
		}

		if (!string.IsNullOrEmpty(home.AnalyticsNote))
		{
			return home.AnalyticsNote;
		}

		return "Insufficient data for estimate";
	}

	private bool HomeHasPending()
	{
		var home = _state.GrokUsage.FindById(HomeId);
		if (home == null)
		{
			return false;
		}

		if (home.Sessions.HasPending
			|| home.DailyTokenTotals.HasPending
			|| home.DailyUsageTotals.HasPending
			|| home.AvailablePeriods.HasPending)
		{
			return true;
		}

		return home is ITrackPropertyChanges trackable && trackable.HasChanges();
	}

	/// <summary>
	/// Slider moved: map 0…1 into the selected period and set the GrokUsage view clock.
	/// </summary>
	private void OnViewClockProgressChanged()
	{
		// Manual scrub takes over from auto-play.
		StopReplay();

		if (!TryGetViewClockRange(out var start, out var max))
		{
			return;
		}

		var progress = Math.Clamp(ViewClockProgress, 0, 1);
		if (progress >= 0.999)
		{
			_bus.GrokUsage.SetViewLive();
			_needsProjectionSeed = true;
			return;
		}

		var ticks = start.UtcTicks + (long) ((max.UtcTicks - start.UtcTicks) * progress);
		var asOf = new DateTimeOffset(ticks, TimeSpan.Zero);
		_bus.GrokUsage.SetViewAsOf(asOf);
		_needsProjectionSeed = true;
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

	private void ProjectAnalytics(GrokHomeUsageState home)
	{
		TokenBurnPerHourLast24h = home.TokenBurnPerHourLast24h;
		TokenBurnPerHourPeriod = home.TokenBurnPerHourPeriod;
		UsagePercentPerHour = home.UsagePercentPerHour;
		LinearPacePercent = home.LinearPacePercent;
		HasUsageEstimate = home.HasUsageEstimate;
		UsageRateSource = home.UsageRateSource ?? string.Empty;
		AnalyticsNote = home.AnalyticsNote ?? string.Empty;

		HasOnDemandCap = home.OnDemandCap > 0;
		OnDemandUsagePercent = HasOnDemandCap
			? Math.Min(100, Math.Max(0, (100.0 * home.OnDemandUsed) / home.OnDemandCap))
			: 0;

		var viewNow = _state.GrokUsage.IsViewLive || (_state.GrokUsage.ViewAsOf == default)
			? DateTimeOffset.UtcNow
			: _state.GrokUsage.ViewAsOf;
		PeriodRemainingText = FormatPeriodRemaining(home.PeriodEnd, viewNow);
		PaceLabel = FormatPaceLabel(home.UsagePercent, home.LinearPacePercent);
		UsageExhaustionText = FormatUsageExhaustion(home);
		ProjectAnalyticsToolTips(home);

		HasAnalytics = home.HasBilling
			|| (home.DailyTokenTotals.Count > 0)
			|| (home.DailyUsageTotals.Count > 0)
			|| (home.TokenBurnPerHourPeriod > 0)
			|| (home.TokenBurnPerHourLast24h > 0);

		FillDailyTokensChart(home.DailyTokenTotals);
		FillDailyUsageChart(home.DailyUsageTotals);
	}

	private void ProjectAnalyticsToolTips(GrokHomeUsageState home)
	{
		var used = home.UsagePercent;
		var linear = home.LinearPacePercent;

		UsageToolTip =
			"Share of your included weekly allowance already used (from Grok billing). "
			+ "100% means the plan pool is exhausted until the period resets.";

		LinearPaceToolTip = linear <= 0
			? "Linear pace needs a known billing period. It is the calendar share of the period that has elapsed, as a percent of 100."
			: $"If usage were even over the whole period, you would be at about {linear:0.#}% by now. "
			+ "This is only a time-based baseline — not how fast you actually used the allowance.";

		PaceLabelToolTip = FormatPaceToolTip(used, linear, PaceLabel);

		TokenBurn24hToolTip =
			$"Average subscription tokens per hour over the last 24 hours ({home.TokenBurnPerHourLast24h:N0}/h). "
			+ "Counts grok-* model inferences only.";

		TokenBurnPeriodToolTip =
			$"Average subscription tokens per hour so far in the selected period ({home.TokenBurnPerHourPeriod:N0}/h). "
			+ "Total period tokens divided by hours elapsed in the period.";

		var rateSource = string.IsNullOrEmpty(home.UsageRateSource)
			? "when enough billing history exists"
			: home.UsageRateSource;
		UsagePercentPerHourToolTip =
			$"Allowance used per hour ({home.UsagePercentPerHour:0.###} %/h), from {rateSource}. "
			+ "Used to project when you might hit 100% if the rate stays similar.";

		UsageEtaToolTip =
			"Projected time you would hit 100% usage at the current %/h rate. "
			+ "Linear estimate only — not a guarantee. Quiet hours or a burst will move it.";

		OnDemandToolTip = HasOnDemandCap
			? "On-demand spend against your configured cap after the included weekly allowance. "
			+ $"{home.OnDemandUsed:0.##} used of {home.OnDemandCap:0.##} cap."
			: "No on-demand cap was reported in billing snapshots for this home.";
	}

	private void ProjectHome()
	{
		var home = _state.GrokUsage.FindById(HomeId);
		if (home == null)
		{
			DisplayName = string.Empty;
			Path = string.Empty;
			IsBusy = false;
			ProgressText = string.Empty;
			ErrorText = string.Empty;
			HomeExists = false;
			HasBilling = false;
			HasCreditUsage = false;
			SubscriptionTier = string.Empty;
			UsagePercent = 0;
			PeriodType = string.Empty;
			PeriodStart = default;
			PeriodEnd = default;
			OnDemandCap = 0;
			OnDemandUsed = 0;
			PrepaidBalance = 0;
			GrandTotalPromptTokens = 0;
			GrandTotalCachedPromptTokens = 0;
			GrandTotalCompletionTokens = 0;
			GrandTotalReasoningTokens = 0;
			GrandTotalTokens = 0;
			LastRefreshedAt = default;
			Sessions.Clear();
			SelectedSession = null;
			AvailablePeriods.Clear();
			SelectedPeriod = null;
			TokenTotalsPeriodLabel = string.Empty;
			ClearAnalyticsProjection();
			ProjectViewClock(null);
			StatusText = "Grok home not found.";
			return;
		}

		DisplayName = home.DisplayName ?? string.Empty;
		Path = home.Path ?? string.Empty;
		IsBusy = home.IsBusy;
		ProgressText = home.ProgressText ?? string.Empty;
		ErrorText = home.ErrorText ?? string.Empty;
		HomeExists = home.HomeExists;
		HasBilling = home.HasBilling;
		HasCreditUsage = home.HasCreditUsage;
		SubscriptionTier = home.SubscriptionTier ?? string.Empty;
		UsagePercent = home.UsagePercent;
		PeriodType = GrokUsageAnalytics.FormatPeriodTypeDisplay(home.PeriodType);
		PeriodStart = home.PeriodStart;
		PeriodEnd = home.PeriodEnd;
		OnDemandCap = home.OnDemandCap;
		OnDemandUsed = home.OnDemandUsed;
		PrepaidBalance = home.PrepaidBalance;
		GrandTotalPromptTokens = home.GrandTotalPromptTokens;
		GrandTotalCachedPromptTokens = home.GrandTotalCachedPromptTokens;
		GrandTotalCompletionTokens = home.GrandTotalCompletionTokens;
		GrandTotalReasoningTokens = home.GrandTotalReasoningTokens;
		GrandTotalTokens = home.GrandTotalTokens;
		LastRefreshedAt = home.LastRefreshedAt;

		Sessions.ReconcileListAndItems(home.Sessions, SessionComparer);
		ProjectPeriods(home);
		ProjectAnalytics(home);
		ProjectViewClock(home);

		if (home.IsBusy)
		{
			StatusText = string.IsNullOrEmpty(home.ProgressText) ? "Refreshing…" : home.ProgressText;
		}
		else if (!string.IsNullOrEmpty(home.ErrorText))
		{
			StatusText = home.ErrorText;
		}
		else if (home.LastRefreshedAt != default)
		{
			var clockNote = _state.GrokUsage.IsViewLive
				? "Live"
				: $"As of {_state.GrokUsage.ViewAsOf.ToLocalTime():g}";
			if (IsReplayPlaying)
			{
				clockNote += $" · replay {FormatSpeed(ReplaySpeed)}";
			}

			StatusText = $"Updated {home.LastRefreshedAt:u} · {home.Sessions.Count} session(s) · {clockNote}";
		}
		else
		{
			StatusText = "Ready";
		}

		if (home is ITrackPropertyChanges trackable)
		{
			trackable.ResetHasChanges();
		}

		home.Sessions.ClearHasPending();
		home.DailyTokenTotals.ClearHasPending();
		home.DailyUsageTotals.ClearHasPending();
		home.AvailablePeriods.ClearHasPending();
	}

	private void ProjectPeriods(GrokHomeUsageState home)
	{
		_isProjectingPeriod = true;
		try
		{
			AvailablePeriods.ReconcileListAndItems(home.AvailablePeriods, PeriodComparer);

			GrokUsagePeriodState match = null;
			foreach (var period in AvailablePeriods)
			{
				if ((period.PeriodStart == home.SelectedPeriodStart)
					&& (period.PeriodEnd == home.SelectedPeriodEnd))
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
		finally
		{
			_isProjectingPeriod = false;
		}
	}

	private void ProjectViewClock(GrokHomeUsageState home)
	{
		_isProjectingViewClock = true;
		try
		{
			IsViewLive = _state.GrokUsage.IsViewLive;
			if ((home == null)
				|| (home.PeriodStart == default)
				|| (home.PeriodEnd == default)
				|| (home.PeriodEnd <= home.PeriodStart))
			{
				HasViewClock = false;
				ViewClockProgress = 1;
				ViewAsOfText = string.Empty;
				return;
			}

			PeriodStart = home.PeriodStart;
			PeriodEnd = home.PeriodEnd;
			if (!TryGetViewClockRange(out var start, out var max))
			{
				HasViewClock = false;
				ViewClockProgress = 1;
				ViewAsOfText = IsViewLive ? "Live" : string.Empty;
				return;
			}

			HasViewClock = true;
			if (IsViewLive || (_state.GrokUsage.ViewAsOf == default))
			{
				ViewClockProgress = 1;
				ViewAsOfText = IsReplayPlaying
					? $"Live · {FormatSpeed(ReplaySpeed)}"
					: "Live";
				return;
			}

			var asOf = _state.GrokUsage.ViewAsOf;
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
				? $"{asOfLabel} · {FormatSpeed(ReplaySpeed)}"
				: asOfLabel;
		}
		finally
		{
			_isProjectingViewClock = false;
		}
	}

	private void ReplayTimerOnTick(object sender, EventArgs e)
	{
		if (!IsReplayPlaying || _isDesignSample)
		{
			return;
		}

		if (!TryGetViewClockRange(out var start, out var max))
		{
			StopReplay();
			return;
		}

		var wallSeconds = _replayWallClock.Elapsed.TotalSeconds;
		_replayWallClock.Restart();
		if (wallSeconds <= 0)
		{
			return;
		}

		var advance = TimeSpan.FromSeconds(wallSeconds * ReplaySpeed);

		var current = ResolveCurrentViewAsOf(start, max);
		var next = current + advance;
		if (next >= max)
		{
			_bus.GrokUsage.SetViewLive();
			StopReplay();
			_needsProjectionSeed = true;
			return;
		}

		if (next < start)
		{
			next = start;
		}

		_bus.GrokUsage.SetViewAsOf(next);
		_needsProjectionSeed = true;
	}

	/// <summary>
	/// View clock instant used for scrub/replay (clamped into the period range).
	/// </summary>
	private DateTimeOffset ResolveCurrentViewAsOf(DateTimeOffset start, DateTimeOffset max)
	{
		if (_state.GrokUsage.IsViewLive || (_state.GrokUsage.ViewAsOf == default))
		{
			return max;
		}

		var asOf = _state.GrokUsage.ViewAsOf;
		if (asOf < start)
		{
			return start;
		}

		if (asOf > max)
		{
			return max;
		}

		return asOf;
	}

	private void SessionsSelectionOnSelectionChanged(
		object sender,
		TreeSelectionModelSelectionChangedEventArgs<GrokSessionUsageState> e)
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

	private void StopReplay()
	{
		if (_replayTimer != null)
		{
			_replayTimer.Stop();
		}

		_replayWallClock.Reset();
		if (IsReplayPlaying)
		{
			IsReplayPlaying = false;
		}
	}

	private bool TryGetViewClockRange(out DateTimeOffset start, out DateTimeOffset max)
	{
		start = default;
		max = default;

		if ((PeriodStart == default) || (PeriodEnd == default) || (PeriodEnd <= PeriodStart))
		{
			return false;
		}

		start = PeriodStart;

		// Live max: cannot scrub past real now on an open period; full end for historical.
		var wallNow = DateTimeOffset.UtcNow;
		max = PeriodEnd <= wallNow
			? PeriodEnd
			: wallNow < PeriodStart
				? PeriodStart
				: wallNow;
		if (max < start)
		{
			max = start;
		}

		return max > start;
	}

	#endregion
}