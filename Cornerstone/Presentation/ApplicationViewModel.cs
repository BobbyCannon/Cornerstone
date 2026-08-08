#region References

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Data;
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

	#endregion

	#region Fields

	private CancellationTokenSource _cts;
	private readonly IDependencyProvider _dependencyProvider;
	private readonly HashSet<DispatchableViewModel> _dispatchables;
	private readonly IDispatcher _dispatcher;
	private IntervalTimer _timer;
	private readonly Dictionary<string, Func<ViewModel>> _viewModelFactories;
	private Task _workerTask;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public ApplicationViewModel(IDependencyProvider dependencyProvider, IDispatcher dispatcher, int updatesPerSecond = 120)
	{
		_dependencyProvider = dependencyProvider;
		_dispatcher = dispatcher;
		_dispatchables = [];
		_viewModelFactories = new Dictionary<string, Func<ViewModel>>();

		WorkInterval = TimeSpan.FromMilliseconds(1000.0 / updatesPerSecond);
	}

	#endregion

	#region Properties

	[Notify]
	public partial ViewModel CurrentViewModel { get; private set; }

	/// <summary>
	/// Optional. When null, the dispatcher apply path does no profiling.
	/// </summary>
	public Profiler SystemProfiler { get; set; }

	protected TimeSpan WorkInterval { get; }

	#endregion

	#region Methods

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

	public override void StartLifecycle()
	{
		if (_workerTask is { IsCompleted: false })
		{
			return;
		}

		_cts = new CancellationTokenSource();
		_timer = new IntervalTimer(WorkInterval);
		_workerTask = Task.Run(() => RunWorkerAsync(_cts.Token), _cts.Token);

		base.StartLifecycle();
	}

	public override void StopLifecycle()
	{
		_cts?.Cancel();
		_timer?.Dispose();
		_timer = null;

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

		base.UninitializeLifecycle();
	}

	protected virtual void Update()
	{
		// Tracked dispatchables (IAppDispatcher.Track). Nested TrackDispatchChild trees
		// apply when a parent ApplyModelChanges flows to its children.
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

		if (pending is null)
		{
			return;
		}

		var profiler = SystemProfiler;
		this.Dispatch(() =>
			{
				foreach (var update in pending)
				{
					update.ApplyModelChanges();
					// Rate-only: true view-projection count (null profiler = no work).
					profiler?.Increment(ApplyScopeName);
				}
			},
			DispatcherPriority.Render
		);
	}

	private async Task RunWorkerAsync(CancellationToken cancellationToken)
	{
		// Capture local reference so StopLifecycle can null the field safely
		var timer = _timer;
		if (timer is null)
		{
			return;
		}

		try
		{
			while (await timer.WaitForNextTickAsync(cancellationToken))
			{
				try
				{
					Update();
				}
				catch (Exception)
				{
					if (!cancellationToken.IsCancellationRequested)
					{
						Debugging.BreakIfAttached();
					}
				}
			}
		}
		catch (OperationCanceledException)
		{
			// Expected on stop
		}
		catch (ObjectDisposedException)
		{
			// Timer was disposed while we were waiting – also expected
		}
	}

	[RelayCommand]
	private void SelectView(object assemblyName)
	{
		TryToSelectViewByModel(assemblyName.ToString());
	}

	#endregion
}