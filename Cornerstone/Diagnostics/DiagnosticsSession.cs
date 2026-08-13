#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Keystone;
using Cornerstone.Keystone.Messages;
using Cornerstone.Presentation;
using Cornerstone.Profiling;

#endregion

namespace Cornerstone.Diagnostics;

/// <summary>
/// Diagnostics models + capture step. Register on <see cref="ApplicationViewModel.DiagnosticsCapture" />
/// while a diagnostics surface is open. Models are UI-free; a DispatchableViewModel projects them.
/// </summary>
public sealed class DiagnosticsSession : IDiagnosticsCapture
{
	#region Fields

	private readonly KeystoneBus _bus;
	private long _lastBusSequence;
	private readonly List<DispatchableViewModel> _trackedBuffer;
	private BusHistoryFilter _viewHistoryFilter;
	private string _viewHistoryFilterText;

	#endregion

	#region Constructors

	public DiagnosticsSession(KeystoneBus bus, Profiler profiler)
	{
		_bus = bus ?? throw new ArgumentNullException(nameof(bus));
		_trackedBuffer = [];
		_viewHistoryFilterText = string.Empty;
		_viewHistoryFilter = BusHistoryFilter.Parse(string.Empty);

		Profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
		
		DispatchModeText = "Idle";
		LoadStatus = "Idle";
		BusHistory = new SpeedyList<ChannelMessageHistory>(128, true);
		LoadPending = new DispatchPending();
		LoadSimulation = new LoadSimulationDispatchable();
		SessionPending = new DispatchPending();
		Tracked = new SpeedyList<TrackedDispatchableModel>(64, true);
		Scopes = new SpeedyList<ProfilerScopeModel>(32, true);

		// Chart ring: filled on capture from profiler per-second history after Refresh.
		var (_, perSecond) = Profiler.SetupScopeHistory(ApplicationViewModel.ApplyScopeName, 60);
		ApplyRateModel = (SeriesDataProvider) perSecond;
	}

	#endregion

	#region Properties

	public TimeSpan ActiveInterval { get; private set; }

	/// <summary>
	/// Model series for AppDispatcher.Apply rate chart (projected with TrackSeries).
	/// </summary>
	public SeriesDataProvider ApplyRateModel { get; private set; }

	public SpeedyList<ChannelMessageHistory> BusHistory { get; }

	public string DispatchModeText { get; private set; }

	/// <summary>
	/// Ring capacity for bus History (drop oldest when exceeded).
	/// </summary>
	public int HistoryLimit
	{
		get => _bus.History.Limit;
		set
		{
			var limit = value < 1 ? 1 : value;
			if (_bus.History.Limit == limit)
			{
				return;
			}

			_bus.History.Limit = limit;

			// Bound the session mirror as well.
			while (BusHistory.Count > limit)
			{
				BusHistory.RemoveAt(0);
			}

			SessionPending.MarkPending();
		}
	}

	/// <summary>
	/// Live recording filter on the bus (does not remove rows already in History).
	/// Same text grammar as <see cref="ViewHistoryFilter" />.
	/// </summary>
	public string HistoryRecordFilter
	{
		get => _bus.HistoryFilter;
		set => _bus.HistoryFilter = value ?? string.Empty;
	}

	public TimeSpan IdleInterval { get; private set; }

	public bool IsHistoryEnabled
	{
		get => _bus.IsHistoryEnabled;
		set => _bus.IsHistoryEnabled = value;
	}

	/// <summary>
	/// When true, each capture re-dirties <see cref="LoadSimulation" /> so feature apply
	/// runs every poll (real AppDispatcher.Apply / tracked load for monitoring tests).
	/// </summary>
	public bool IsSimulatingLoad { get; set; }

	public int LastApplyBatchSize { get; private set; }

	public int LoadApplyCount { get; private set; }

	/// <summary>
	/// Pending signal for load counter projection onto the diagnostics surface.
	/// </summary>
	public DispatchPending LoadPending { get; }

	/// <summary>
	/// Tracked feature root that produces synthetic apply work when simulating load.
	/// </summary>
	public LoadSimulationDispatchable LoadSimulation { get; }

	public string LoadStatus { get; private set; }

	public Profiler Profiler { get; }

	public SpeedyList<ProfilerScopeModel> Scopes { get; }

	/// <summary>
	/// Aggregate pending for scalar dispatcher fields and session-level apply.
	/// </summary>
	public DispatchPending SessionPending { get; }

	public SpeedyList<TrackedDispatchableModel> Tracked { get; }

	public int TrackedCount { get; private set; }

	/// <summary>
	/// View-only filter over the session bus history list. Changing rebuilds from bus History.
	/// Does not delete recorded rows on the bus.
	/// </summary>
	public string ViewHistoryFilter
	{
		get => _viewHistoryFilterText;
		set
		{
			var text = value ?? string.Empty;
			if (_viewHistoryFilterText == text)
			{
				return;
			}

			_viewHistoryFilterText = text;
			_viewHistoryFilter = BusHistoryFilter.Parse(text);

			RebuildBusHistoryFromBus();
			SessionPending.MarkPending();
		}
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public void Capture(ApplicationViewModel host, int pendingApplyCount)
	{
		if (host is null)
		{
			return;
		}

		var changed = false;

		var mode = host.IsDispatchActive ? "Active" : "Idle";
		if (DispatchModeText != mode)
		{
			DispatchModeText = mode;
			changed = true;
		}

		if (IdleInterval != host.IdleInterval)
		{
			IdleInterval = host.IdleInterval;
			changed = true;
		}

		if (ActiveInterval != host.ActiveInterval)
		{
			ActiveInterval = host.ActiveInterval;
			changed = true;
		}

		// Batch size known for this tick (pre-capture dirty count; host may refine after re-poll).
		if (LastApplyBatchSize != pendingApplyCount)
		{
			LastApplyBatchSize = pendingApplyCount;
			changed = true;
		}

		host.CopyTrackedDispatchables(_trackedBuffer);
		if (TrackedCount != _trackedBuffer.Count)
		{
			TrackedCount = _trackedBuffer.Count;
			changed = true;
		}

		if (ReconcileTracked())
		{
			changed = true;
		}

		if (SyncBusHistory())
		{
			changed = true;
		}

		Profiler.Refresh();
		if (ReconcileScopes())
		{
			changed = true;
		}

		// ApplyRateModel is the profiler PerSecondHistory ring (SetupScopeHistory); Refresh mutates it.

		// Project feature load-sim counters into session models (after prior tick's apply).
		if (SyncLoadSimulationScalars())
		{
			changed = true;
		}

		if (IsSimulatingLoad)
		{
			// Dirty the tracked feature root for the *next* poll; Collect already ran this tick.
			LoadSimulation.MarkWork();

			// Keep active rate (default ~120 Hz) without a view timer.
			host.RequestDispatch();
		}

		if (changed)
		{
			SessionPending.MarkPending();
		}
	}

	public void ClearBusHistory()
	{
		_bus.History.Clear();
		BusHistory.Clear();
		_lastBusSequence = 0;
		SessionPending.MarkPending();
	}

	/// <summary>
	/// One-shot feature work (tracked root). Prefer this over diagnostics-only pending.
	/// </summary>
	public void PulseLoad()
	{
		LoadSimulation.MarkWork();

		// Reflect last known applied count until next capture after apply.
		SyncLoadSimulationScalars();
		SessionPending.MarkPending();
	}

	/// <summary>
	/// Rebuild the session bus list from the bus ring using the current view filter.
	/// </summary>
	public void RebuildBusHistoryFromBus()
	{
		BusHistory.Clear();
		_lastBusSequence = 0;
		var history = _bus.History;
		var filter = _viewHistoryFilter;
		var maxSequence = 0L;
		for (var i = 0; i < history.Count; i++)
		{
			var entry = history[i];
			if (entry.Sequence > maxSequence)
			{
				maxSequence = entry.Sequence;
			}

			if (filter is not null && !filter.IsMatchAll && !filter.Matches(entry))
			{
				continue;
			}

			BusHistory.Add(entry);
		}

		_lastBusSequence = maxSequence;
	}

	private bool ReconcileScopes()
	{
		var ordered = Profiler
			.Where(x => !string.IsNullOrEmpty(x.Name))
			.OrderBy(x => x.Name)
			.ToList();

		var changed = false;
		if (Scopes.Count != ordered.Count)
		{
			changed = true;
		}

		// Rebuild model list (SpeedyList marks pending on structural change).
		if (changed || !ScopesMatch(ordered))
		{
			Scopes.Clear();
			foreach (var stats in ordered)
			{
				Scopes.Add(new ProfilerScopeModel(
					stats.Name,
					stats.CallsPerSecond,
					stats.AverageTicks,
					stats.Count));
			}

			return true;
		}

		// Same names/count: update values in place when rates move.
		for (var i = 0; i < ordered.Count; i++)
		{
			var stats = ordered[i];
			var row = Scopes[i];
			if ((Math.Abs(row.CallsPerSecond - stats.CallsPerSecond) > 0.0001)
				|| (Math.Abs(row.AverageTicks - stats.AverageTicks) > 0.0001)
				|| (row.Count != stats.Count))
			{
				row.CallsPerSecond = stats.CallsPerSecond;
				row.AverageTicks = stats.AverageTicks;
				row.Count = stats.Count;
				changed = true;
			}
		}

		return changed;
	}

	private bool ReconcileTracked()
	{
		var snapshot = _trackedBuffer
			.Select(x => new TrackedDispatchableModel(
				x.GetType().Name,
				x.IsAttached,
				x.HasModelChanges()))
			.OrderBy(x => x.Name)
			.ToList();

		if (Tracked.Count != snapshot.Count)
		{
			Tracked.Clear();
			foreach (var row in snapshot)
			{
				Tracked.Add(row);
			}

			return true;
		}

		var changed = false;
		for (var i = 0; i < snapshot.Count; i++)
		{
			var want = snapshot[i];
			var have = Tracked[i];
			if (have.Name != want.Name)
			{
				Tracked.Clear();
				foreach (var row in snapshot)
				{
					Tracked.Add(row);
				}

				return true;
			}

			if ((have.IsAttached != want.IsAttached) || (have.HasModelChanges != want.HasModelChanges))
			{
				have.IsAttached = want.IsAttached;
				have.HasModelChanges = want.HasModelChanges;
				changed = true;
			}
		}

		return changed;
	}

	private bool ScopesMatch(List<TimedScopeStats> ordered)
	{
		if (Scopes.Count != ordered.Count)
		{
			return false;
		}

		for (var i = 0; i < ordered.Count; i++)
		{
			if (Scopes[i].Name != ordered[i].Name)
			{
				return false;
			}
		}

		return true;
	}

	private bool SyncBusHistory()
	{
		var history = _bus.History;
		if (history.Count == 0)
		{
			if (BusHistory.Count == 0)
			{
				return false;
			}

			BusHistory.Clear();
			_lastBusSequence = 0;
			return true;
		}

		// Bus ring may have dropped oldest entries (limit); sequences can jump.
		// If our cursor is ahead of everything still on the bus, rebuild.
		var busMinSequence = long.MaxValue;
		var busMaxSequence = 0L;
		for (var i = 0; i < history.Count; i++)
		{
			var seq = history[i].Sequence;
			if (seq < busMinSequence)
			{
				busMinSequence = seq;
			}

			if (seq > busMaxSequence)
			{
				busMaxSequence = seq;
			}
		}

		if ((_lastBusSequence > 0) && (busMinSequence > (_lastBusSequence + 1)))
		{
			// Gaps from ring eviction — resync from bus.
			RebuildBusHistoryFromBus();
			return true;
		}

		var filter = _viewHistoryFilter;
		var added = false;
		for (var i = 0; i < history.Count; i++)
		{
			var entry = history[i];
			if (entry.Sequence <= _lastBusSequence)
			{
				continue;
			}

			if (filter is null || filter.IsMatchAll || filter.Matches(entry))
			{
				BusHistory.Add(entry);
				added = true;
			}
		}

		_lastBusSequence = busMaxSequence;

		// Bound model list like bus History.Limit.
		var limit = history.Limit > 0 ? history.Limit : 100;
		while (BusHistory.Count > limit)
		{
			BusHistory.RemoveAt(0);
		}

		return added;
	}

	private bool SyncLoadSimulationScalars()
	{
		var applyCount = LoadSimulation.ApplyCount;
		var status = LoadSimulation.Status;
		var changed = false;

		if (LoadApplyCount != applyCount)
		{
			LoadApplyCount = applyCount;
			changed = true;
		}

		if (LoadStatus != status)
		{
			LoadStatus = status;
			changed = true;
		}

		if (changed)
		{
			LoadPending.MarkPending();
		}

		return changed;
	}

	#endregion
}