#region References

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// The service to throttle work that supports cancellation.
/// </summary>
public class DebounceService<T> : DebounceService
{
	#region Constructors

	/// <summary>
	/// Create an instance of the service for debouncing an action.
	/// </summary>
	/// <param name="delay"> The amount of time before the action will trigger. </param>
	/// <param name="action"> The action to debounce. </param>
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	public DebounceService(TimeSpan delay, Action<CancellationToken, T> action, ITimeProvider timeService = null)
		: base(delay, (x, v) => action(x, v is T tValue ? tValue : default), timeService)
	{
	}

	#endregion

	#region Methods

	/// <summary>
	/// Trigger the service. Will be trigger after the timespan.
	/// </summary>
	/// <param name="value"> The value to trigger with. </param>
	/// <param name="force"> An optional flag to immediately trigger if true. Defaults to false. </param>
	public void Trigger(T value, bool force = false)
	{
		base.Trigger(value, force);
	}

	#endregion
}

/// <summary>
/// The service to throttle work that supports cancellation.
/// </summary>
public class DebounceService : IDisposable
{
	#region Fields

	private readonly Action<CancellationToken, object> _action;
	private object _data;
	private readonly TimeSpan _delay;
	private bool _force;
	private static readonly ConcurrentDictionary<string, DebounceService> _globalServices;
	private DateTime _requestedFor;
	private readonly ITimeProvider _timeService;
	private BackgroundWorker _worker;

	#endregion

	#region Constructors

	/// <summary>
	/// Create an instance of the service for debouncing an action.
	/// </summary>
	/// <param name="delay"> The amount of time before the action will trigger. </param>
	/// <param name="action"> The action to debounce. </param>
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	public DebounceService(TimeSpan delay, Action<CancellationToken> action, ITimeProvider timeService = null)
		: this(delay, (token, _) => action(token), timeService)
	{
	}

	/// <summary>
	/// Create an instance of the service for debouncing an action.
	/// </summary>
	/// <param name="delay"> The amount of time before the action will trigger. </param>
	/// <param name="action"> The action to debounce. </param>
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	public DebounceService(TimeSpan delay, Action<CancellationToken, object> action, ITimeProvider timeService = null)
	{
		_delay = delay;
		_action = action;
		_timeService = timeService ?? TimeService.CurrentTime;
		_worker = new BackgroundWorker();
		_worker.RunWorkerCompleted += WorkerOnRunWorkerCompleted;
		_worker.DoWork += WorkerOnDoWork;
		_worker.WorkerReportsProgress = true;
		_worker.WorkerSupportsCancellation = true;
	}

	static DebounceService()
	{
		_globalServices = new ConcurrentDictionary<string, DebounceService>();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Globally track debouncing of an action.
	/// </summary>
	public static void Debounce(string id, Action<CancellationToken> action, int delay, bool force = false)
	{
		var service = _globalServices.GetOrAdd(id,
			_ =>
			{
				var service = new DebounceService(TimeSpan.FromMilliseconds(delay), (token, _) => action(token));
				return service;
			});
		service.Trigger(force);
	}

	/// <summary>
	/// Globally track debouncing of an action.
	/// </summary>
	public static void Debounce<T>(string id, Action<CancellationToken, T> action, int delay, T value, bool force = false)
	{
		_globalServices.GetOrAdd(id,
			_ => new DebounceService<T>(TimeSpan.FromMilliseconds(delay), action)
		).Trigger(value, force);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Trigger the service. Will be trigger after the timespan.
	/// </summary>
	/// <param name="force"> An optional flag to immediately trigger if true. Defaults to false. </param>
	public void Trigger(bool force = false)
	{
		Trigger(null, force);
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
		var worker = _worker;

		if (!disposing || (worker == null))
		{
			return;
		}

		lock (_worker)
		{
			if (worker.IsBusy)
			{
				worker.CancelAsync();
			}

			worker.RunWorkerCompleted -= WorkerOnRunWorkerCompleted;
			worker.DoWork -= WorkerOnDoWork;

			_worker = null;
		}
	}

	/// <summary>
	/// Trigger the service. Will be trigger after the timespan.
	/// </summary>
	/// <param name="value"> The value to trigger with. </param>
	/// <param name="force"> An optional flag to immediately trigger if true. Defaults to false. </param>
	protected void Trigger(object value, bool force = false)
	{
		lock (_worker)
		{
			// Optionally turn on force
			_force |= force;
			_data = value;

			// Queue up the next run
			_requestedFor = _force
				? _timeService.UtcNow
				: _timeService.UtcNow + _delay;

			// Start the worker if it's not running
			if (_worker?.IsBusy != true)
			{
				_worker?.RunWorkerAsync();
			}
		}
	}

	private void WorkerOnDoWork(object sender, DoWorkEventArgs e)
	{
		var worker = (BackgroundWorker) sender;
		var lastStart = DateTime.MinValue;
		var cancellationTokenSource = new CancellationTokenSource();

		Task currentRun = null;

		while (!worker.CancellationPending)
		{
			if (((_requestedFor <= lastStart) || (_requestedFor > _timeService.UtcNow)) && !_force)
			{
				if (currentRun is { Status: TaskStatus.RanToCompletion or TaskStatus.Canceled or TaskStatus.Faulted })
				{
					currentRun = null;

					// We don't have an outstanding request so bounce
					if (_requestedFor <= lastStart)
					{
						// Exit the worker
						return;
					}
				}

				// Nothing to do...
				Thread.Sleep(10);
				continue;
			}

			// A new request is ready to be processed
			if (currentRun != null)
			{
				if (!cancellationTokenSource.IsCancellationRequested)
				{
					// Cancel the current run
					cancellationTokenSource.Cancel();
				}

				if (currentRun is { Status: TaskStatus.RanToCompletion or TaskStatus.Canceled or TaskStatus.Faulted })
				{
					currentRun = null;

					// We don't have an outstanding request so bounce
					if (_requestedFor <= lastStart)
					{
						// Exit the worker
						return;
					}
				}
			}
			else
			{
				_force = false;
				var data = _data;
				lastStart = _requestedFor;
				cancellationTokenSource = new CancellationTokenSource();
				currentRun = Task.Run(() => _action(cancellationTokenSource.Token, data), cancellationTokenSource.Token);
			}
		}
	}

	private void WorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
	{
		if ((_worker == null) || (_requestedFor < _timeService.UtcNow))
		{
			return;
		}

		lock (_worker)
		{
			// Start the worker if it's not running
			if (_worker?.IsBusy != true)
			{
				_worker?.RunWorkerAsync();
			}
		}
	}

	#endregion
}