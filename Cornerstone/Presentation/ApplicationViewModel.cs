#region References

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Data;
using Cornerstone.Diagnostics;
using Cornerstone.Extensions;
using Cornerstone.Keystone.Lifecycle;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Threading;

#endregion

namespace Cornerstone.Presentation;

[SourceReflection]
public partial class ApplicationViewModel : LifecycleTracker<ViewModel>, IAppNavigator, IAppDispatcher, IDispatchable
{
	#region Constants

	/// <summary>
	/// Count scope: one increment per <see cref="DispatchableViewModel.ApplyModelChanges" /> on the UI dispatch.
	/// Opt in via <see cref="SystemProfiler" />.
	/// </summary>
	public const string ApplyScopeName = "AppDispatcher.Apply";

	/// <summary>
	/// Default high-rate poll while work is flowing or was just requested.
	/// Uses <see cref="IntervalTimer" /> so short periods (e.g. 120 Hz) stay on schedule.
	/// </summary>
	public const int DefaultActiveUpdatesPerSecond = 120;

	/// <summary>
	/// Default empty active ticks before returning to the idle poll rate.
	/// </summary>
	public const int DefaultIdleTicksBeforeThrottle = 8;

	/// <summary>
	/// Default slow safety poll when no work is pending (parked wait, not IntervalTimer).
	/// </summary>
	public const int DefaultIdleUpdatesPerSecond = 10;

	#endregion

	#region Fields

	private IntervalTimer _activeTimer;
	private CancellationTokenSource _cts;
	private readonly IDependencyProvider _dependencyProvider;
	private readonly HashSet<DispatchableViewModel> _dispatchables;
	private readonly IDispatcher _dispatcher;
	private readonly Dictionary<string, Func<ViewModel>> _viewModelFactories;
	private ManualResetEventSlim _wakeEvent;
	private Task _workerTask;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public ApplicationViewModel(
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher,
		int activeUpdatesPerSecond = DefaultActiveUpdatesPerSecond,
		int idleUpdatesPerSecond = DefaultIdleUpdatesPerSecond,
		int idleTicksBeforeThrottle = DefaultIdleTicksBeforeThrottle)
	{
		if (idleUpdatesPerSecond <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(idleUpdatesPerSecond));
		}

		if (activeUpdatesPerSecond <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(activeUpdatesPerSecond));
		}

		if (idleTicksBeforeThrottle <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(idleTicksBeforeThrottle));
		}

		_dependencyProvider = dependencyProvider;
		_dispatcher = dispatcher;
		_dispatchables = [];
		_viewModelFactories = new Dictionary<string, Func<ViewModel>>();

		IdleInterval = TimeSpan.FromMilliseconds(1000.0 / idleUpdatesPerSecond);
		ActiveInterval = TimeSpan.FromMilliseconds(1000.0 / activeUpdatesPerSecond);
		IdleTicksBeforeThrottle = idleTicksBeforeThrottle;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Active-path tick period (high rate while work is flowing).
	/// </summary>
	public TimeSpan ActiveInterval { get; }

	[Notify]
	public partial ViewModel CurrentViewModel { get; private set; }

	/// <summary>
	/// Optional diagnostics snapshot step. When non-null, invoked each poll before UI apply.
	/// Null (default) means zero diagnostics capture cost.
	/// </summary>
	public IDiagnosticsCapture DiagnosticsCapture { get; set; }

	/// <summary>
	/// Optional diagnostics ViewModel. Not part of <see cref="Track" /> membership.
	/// Applied once after the feature apply loop when attached and dirty.
	/// </summary>
	public DispatchableViewModel DiagnosticsDispatchable { get; set; }

	/// <summary>
	/// Idle-path park timeout (slow safety poll when quiet).
	/// </summary>
	public TimeSpan IdleInterval { get; }

	/// <summary>
	/// True while the worker is using the high-rate active path (<see cref="IntervalTimer" />).
	/// </summary>
	public bool IsDispatchActive { get; private set; }

	/// <summary>
	/// Number of dispatchables that ran <see cref="DispatchableViewModel.ApplyModelChanges" />
	/// on the last apply tick (0 when the last tick applied nothing).
	/// </summary>
	public int LastApplyBatchSize { get; private set; }

	/// <summary>
	/// Optional. When null, the dispatcher apply path does no profiling.
	/// </summary>
	public Profiler SystemProfiler { get; set; }

	protected int IdleTicksBeforeThrottle { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Copies currently tracked dispatchable roots into <paramref name="destination" /> (cleared first).
	/// For diagnostics UI; not required for normal AppDispatcher use.
	/// </summary>
	public void CopyTrackedDispatchables(List<DispatchableViewModel> destination)
	{
		if (destination is null)
		{
			throw new ArgumentNullException(nameof(destination));
		}

		destination.Clear();
		lock (_dispatchables)
		{
			foreach (var item in _dispatchables)
			{
				destination.Add(item);
			}
		}
	}

	public IDispatcher GetDispatcher()
	{
		return _dispatcher;
	}

	public override bool IsLifecycleStarted()
	{
		return _workerTask is { IsCompleted: false };
	}

	public void RegisterViewModel<T>() where T : ViewModel
	{
		var typeOfT = typeof(T);
		_viewModelFactories.TryAdd(typeOfT.ToAssemblyName(), () =>
		{
			var viewModel = _dependencyProvider.GetInstance<T>();
			Track(viewModel);
			return viewModel;
		});
	}

	public void Release(DispatchableViewModel dispatchableViewModel)
	{
		if (dispatchableViewModel == null)
		{
			return;
		}

		// Dispatcher membership only — lifecycle is owned by DockingManager (or other hosts).
		lock (_dispatchables)
		{
			_dispatchables.Remove(dispatchableViewModel);
		}
	}

	/// <inheritdoc />
	public void RequestDispatch()
	{
		// Coalescing: Set while already set is fine; worker Reset after wait.
		// Idle: unparks ManualResetEventSlim. Active: noted before each IntervalTimer tick.
		_wakeEvent?.Set();
	}

	public override void StartLifecycle()
	{
		if (_workerTask is { IsCompleted: false })
		{
			return;
		}

		IsDispatchActive = false;

		_cts = new CancellationTokenSource();
		_wakeEvent = new ManualResetEventSlim(false);
		_workerTask = Task.Run(() => RunWorkerAsync(_cts.Token), _cts.Token);

		base.StartLifecycle();
	}

	public override void StopLifecycle()
	{
		_cts?.Cancel();

		// Unblock idle Wait if the worker is parked.
		_wakeEvent?.Set();

		// Unblock active IntervalTimer.WaitForNextTickAsync (returns false when disposed).
		DisposeActiveTimer();

		base.StopLifecycle();
	}

	public void Track(DispatchableViewModel dispatchableViewModel)
	{
		if (dispatchableViewModel == null)
		{
			return;
		}

		// Membership for the apply loop only. Do not LifecycleTracker.Track here —
		// docked tabs are already lifecycle children of DockingManager.
		lock (_dispatchables)
		{
			_dispatchables.Add(dispatchableViewModel);
		}
	}

	public bool TryToSelectViewByModel(string assemblyName)
	{
		if (!_viewModelFactories.TryGetValue(assemblyName, out var factory))
		{
			return false;
		}

		CurrentViewModel = factory.Invoke();
		return true;
	}

	public override void UninitializeLifecycle()
	{
		_workerTask = null;
		_cts?.Dispose();
		_cts = null;
		_wakeEvent?.Dispose();
		_wakeEvent = null;
		DisposeActiveTimer();
		IsDispatchActive = false;

		base.UninitializeLifecycle();
	}

	/// <summary>
	/// Polls tracked feature roots and applies pending model changes on the UI dispatcher.
	/// When <see cref="DiagnosticsCapture" /> is set, snapshots diagnostics models first.
	/// <see cref="DiagnosticsDispatchable" /> is not in the tracked set; it is applied once
	/// after the feature loop when attached and dirty (no ordering of the feature list).
	/// </summary>
	/// <returns>
	/// True when at least one <strong>feature</strong> ViewModel was applied.
	/// Diagnostics-only apply does not count — otherwise capture of IsDispatchActive
	/// feeds back into Active↔Idle oscillation (dirty → apply → Active → quiet → Idle → dirty).
	/// </returns>
	protected virtual bool Update()
	{
		// Feature roots only (IAppDispatcher.Track). Nested TrackDispatchChild trees
		// apply when a parent ApplyModelChanges flows to its children.
		var pending = CollectPendingDispatchables();
		var featureCount = pending?.Count ?? 0;

		var capture = DiagnosticsCapture;
		if (capture is not null)
		{
			capture.Capture(this, featureCount);
		}

		var diagnostics = DiagnosticsDispatchable;
		var applyDiagnostics = diagnostics is not null
			&& diagnostics.IsAttached
			&& diagnostics.HasModelChanges();

		if (pending is null && !applyDiagnostics)
		{
			LastApplyBatchSize = 0;
			return false;
		}

		// Feature work only — diagnostics is monitoring overhead, not app projection batch size.
		LastApplyBatchSize = featureCount;
		var profiler = SystemProfiler;
		this.Dispatch(() =>
			{
				if (pending is not null)
				{
					foreach (var update in pending)
					{
						update.ApplyModelChanges();

						// Rate-only: true feature view-projection count (null profiler = no work).
						profiler?.Increment(ApplyScopeName);
					}
				}

				if (applyDiagnostics)
				{
					// After the feature loop; do not Increment ApplyScopeName (keeps chart = app work).
					diagnostics?.ApplyModelChanges();
				}
			},
			DispatcherPriority.Render
		);

		// Adaptive idle/active follows feature work (and RequestDispatch), not diagnostics.
		return pending is not null;
	}

	private List<DispatchableViewModel> CollectPendingDispatchables()
	{
		List<DispatchableViewModel> pending = null;

		lock (_dispatchables)
		{
			foreach (var dispatchable in _dispatchables)
			{
				if (!dispatchable.IsAttached || !dispatchable.HasModelChanges())
				{
					continue;
				}

				pending ??= [];
				pending.Add(dispatchable);
			}
		}

		return pending;
	}

	private void DisposeActiveTimer()
	{
		var timer = Interlocked.Exchange(ref _activeTimer, null);
		timer?.Dispose();
	}

	private IntervalTimer EnsureActiveTimer()
	{
		var timer = _activeTimer;
		if (timer is not null)
		{
			return timer;
		}

		timer = new IntervalTimer(ActiveInterval);
		_activeTimer = timer;
		return timer;
	}

	private async Task RunWorkerAsync(CancellationToken cancellationToken)
	{
		var wake = _wakeEvent;
		if (wake is null)
		{
			return;
		}

		var isActive = false;
		var idleStreak = 0;

		try
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var requested = false;

				try
				{
					if (isActive)
					{
						// High-rate path: IntervalTimer can hit short periods (e.g. 120 Hz).
						// Consume any RequestDispatch flags so producers keep the active streak.
						if (wake.IsSet)
						{
							wake.Reset();
							requested = true;
						}

						var timer = EnsureActiveTimer();
						if (!await timer.WaitForNextTickAsync(cancellationToken))
						{
							// Disposed during stop
							break;
						}
					}
					else
					{
						// Idle path: park cheaply until period or RequestDispatch / stop.
						DisposeActiveTimer();
						requested = wake.Wait(IdleInterval, cancellationToken);
						if (requested)
						{
							wake.Reset();
							if (cancellationToken.IsCancellationRequested)
							{
								break;
							}
						}
					}
				}
				catch (OperationCanceledException)
				{
					break;
				}

				var applied = false;
				try
				{
					applied = Update();
				}
				catch (Exception)
				{
					if (!cancellationToken.IsCancellationRequested)
					{
						Debugging.BreakIfAttached();
					}
				}

				var wasActive = isActive;
				AdaptiveDispatchMode.Advance(ref isActive, ref idleStreak, applied, requested, IdleTicksBeforeThrottle);

				if (wasActive && !isActive)
				{
					// Drop precision timer so idle stays low-CPU.
					DisposeActiveTimer();
				}

				IsDispatchActive = isActive;
			}
		}
		catch (OperationCanceledException)
		{
			// Expected on stop
		}
		catch (ObjectDisposedException)
		{
			// Wake event or timer disposed during teardown
		}
		finally
		{
			DisposeActiveTimer();
			IsDispatchActive = false;
		}
	}

	[RelayCommand]
	private void SelectView(object assemblyName)
	{
		TryToSelectViewByModel(assemblyName.ToString());
	}

	#endregion
}