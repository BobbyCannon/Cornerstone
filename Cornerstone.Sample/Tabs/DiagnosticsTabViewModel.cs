#region References

using System;
using Cornerstone.Compare;
using Cornerstone.Data;
using Cornerstone.Diagnostics;
using Cornerstone.Keystone.Messages;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Sample.Tabs;

/// <summary>
/// Projects <see cref="DiagnosticsSession" /> models onto bindable presentation state via AppDispatcher.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
public partial class DiagnosticsTabViewModel : DispatchableViewModel
{
	#region Fields

	private static readonly GenericEqualityComparer<ChannelMessageHistory> _historyComparer;
	private static readonly GenericEqualityComparer<ProfilerScopeModel> _scopeComparer;
	private static readonly GenericEqualityComparer<TrackedDispatchableModel> _trackedComparer;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public DiagnosticsTabViewModel(DiagnosticsSession session)
	{
		ActiveIntervalText = string.Empty;
		DispatchModeText = "Idle";
		IdleIntervalText = string.Empty;
		LoadStatus = "Idle";
		Session = session ?? throw new ArgumentNullException(nameof(session));

		BusHistory = [];
		TrackedDispatchables = [];
		Scopes = [];
		ApplyRateView = new SeriesDataProvider(session.ApplyRateModel.Length);

		TrackBinding(session.SessionPending, ApplySessionScalars);
		TrackBinding(session.LoadPending, ApplyLoadScalars);
		TrackCollection(session.BusHistory, BusHistory, _historyComparer);
		TrackCollection(session.Tracked, TrackedDispatchables, _trackedComparer, CollectionReconcileMode.ListAndItems);
		TrackCollection(session.Scopes, Scopes, _scopeComparer, CollectionReconcileMode.ListAndItems);
		TrackSeries(session.ApplyRateModel, ApplyRateView);
	}

	static DiagnosticsTabViewModel()
	{
		_historyComparer = new((x, y) => (x != null) && (y != null) && (x.Sequence == y.Sequence), x => x.Sequence.GetHashCode());
		_trackedComparer = new((x, y) => (x != null) && (y != null) && (x.Name == y.Name), x => x.Name?.GetHashCode() ?? 0);
		_scopeComparer = new((x, y) => (x != null) && (y != null) && (x.Name == y.Name), x => x.Name?.GetHashCode() ?? 0);
	}

	#endregion

	#region Properties

	public string ActiveIntervalText { get; private set; }

	public SeriesDataProvider ApplyRateView { get; }

	public PresentationList<ChannelMessageHistory> BusHistory { get; }

	public string DispatchModeText { get; private set; }

	/// <summary>
	/// Ring capacity for recorded bus history.
	/// </summary>
	public int HistoryLimit
	{
		get => Session.HistoryLimit;
		set
		{
			if (Session.HistoryLimit == value)
			{
				return;
			}

			Session.HistoryLimit = value;
			NotifyComputedPropertyChanged(nameof(HistoryLimit), Session.HistoryLimit);
		}
	}

	/// <summary>
	/// Live recording filter on the bus (channel:, type:, error:). Empty = all.
	/// </summary>
	public string HistoryRecordFilter
	{
		get => Session.HistoryRecordFilter;
		set
		{
			var text = value ?? string.Empty;
			if (Session.HistoryRecordFilter == text)
			{
				return;
			}

			Session.HistoryRecordFilter = text;
			NotifyComputedPropertyChanged(nameof(HistoryRecordFilter), text);
		}
	}

	public string IdleIntervalText { get; private set; }

	public bool IsHistoryEnabled
	{
		get => Session.IsHistoryEnabled;
		set
		{
			if (Session.IsHistoryEnabled == value)
			{
				return;
			}

			Session.IsHistoryEnabled = value;
			NotifyComputedPropertyChanged(nameof(IsHistoryEnabled), value);
		}
	}

	/// <summary>
	/// Continuous synthetic feature load via tracked <see cref="LoadSimulationDispatchable" />.
	/// </summary>
	public bool IsSimulatingLoad
	{
		get => Session.IsSimulatingLoad;
		set
		{
			if (Session.IsSimulatingLoad == value)
			{
				return;
			}

			Session.IsSimulatingLoad = value;
			if (value)
			{
				Session.PulseLoad();
			}

			NotifyComputedPropertyChanged(nameof(IsSimulatingLoad), value);
		}
	}

	public int LastApplyBatchSize { get; private set; }

	public int LoadApplyCount { get; private set; }

	public string LoadStatus { get; private set; }

	public PresentationList<ProfilerScopeModel> Scopes { get; }

	public DiagnosticsSession Session { get; }

	public int TrackedCount { get; private set; }

	public PresentationList<TrackedDispatchableModel> TrackedDispatchables { get; }

	/// <summary>
	/// View-only filter bar text (does not remove bus ring rows).
	/// </summary>
	public string ViewHistoryFilter
	{
		get => Session.ViewHistoryFilter;
		set
		{
			var text = value ?? string.Empty;
			if (Session.ViewHistoryFilter == text)
			{
				return;
			}

			Session.ViewHistoryFilter = text;
			NotifyComputedPropertyChanged(nameof(ViewHistoryFilter), text);
		}
	}

	#endregion

	#region Methods

	public void ClearBusHistory()
	{
		Session.ClearBusHistory();
	}

	public void PulseLoad()
	{
		Session.PulseLoad();
	}

	private void ApplyLoadScalars()
	{
		if (LoadApplyCount != Session.LoadApplyCount)
		{
			LoadApplyCount = Session.LoadApplyCount;
			NotifyComputedPropertyChanged(nameof(LoadApplyCount), LoadApplyCount);
		}

		if (LoadStatus != Session.LoadStatus)
		{
			LoadStatus = Session.LoadStatus;
			NotifyComputedPropertyChanged(nameof(LoadStatus), LoadStatus);
		}
	}

	private void ApplySessionScalars()
	{
		if (DispatchModeText != Session.DispatchModeText)
		{
			DispatchModeText = Session.DispatchModeText;
			NotifyComputedPropertyChanged(nameof(DispatchModeText), DispatchModeText);
		}

		var idle = Session.IdleInterval.Humanize();
		if (IdleIntervalText != idle)
		{
			IdleIntervalText = idle;
			NotifyComputedPropertyChanged(nameof(IdleIntervalText), IdleIntervalText);
		}

		var active = Session.ActiveInterval.Humanize();
		if (ActiveIntervalText != active)
		{
			ActiveIntervalText = active;
			NotifyComputedPropertyChanged(nameof(ActiveIntervalText), ActiveIntervalText);
		}

		if (LastApplyBatchSize != Session.LastApplyBatchSize)
		{
			LastApplyBatchSize = Session.LastApplyBatchSize;
			NotifyComputedPropertyChanged(nameof(LastApplyBatchSize), LastApplyBatchSize);
		}

		if (TrackedCount != Session.TrackedCount)
		{
			TrackedCount = Session.TrackedCount;
			NotifyComputedPropertyChanged(nameof(TrackedCount), TrackedCount);
		}
	}

	#endregion
}