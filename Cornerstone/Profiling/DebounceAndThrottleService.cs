#region References

using System;
using System.ComponentModel;
using System.Threading;
using Cornerstone.Collections;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using PropertyChanged;
using DependsOn2 = PropertyChanging.DependsOnAttribute;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// The service to debounce or throttle work that supports cancellation.
/// </summary>
public abstract class DebounceOrThrottleService<T> : Bindable, IDisposable
{
	#region Fields

	private readonly Action<CancellationToken, T> _action;
	private CancellationTokenSource _cancellationTokenSource;
	private bool _force;
	private readonly IDateTimeProvider _timeService;
	private BackgroundWorker _worker;
	private readonly object _workerLock;

	#endregion

	#region Constructors

	/// <summary>
	/// Create an instance of the service for throttling an action.
	/// </summary>
	/// <param name="interval"> The amount of time between each trigger. </param>
	/// <param name="action"> The action to throttle. </param>
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	protected DebounceOrThrottleService(TimeSpan interval, Action<CancellationToken, T> action, IDateTimeProvider timeService = null, IDispatcher dispatcher = null) : base(dispatcher)
	{
		AllowTriggerDuringProcessing = false;
		Interval = interval;
		TriggerOnDateTime = DateTime.MinValue;
		Queue = new SpeedyQueue<T> { Limit = 1 };

		_action = action;
		_timeService = timeService ?? TimeService.RealTime;
		_workerLock = new object();
		_worker = new BackgroundWorker();
		_worker.RunWorkerCompleted += WorkerOnRunWorkerCompleted;
		_worker.DoWork += WorkerOnDoWork;
		_worker.WorkerSupportsCancellation = true;
		_worker.RunWorkerAsync();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Allow triggering during processing.
	/// </summary>
	public bool AllowTriggerDuringProcessing { get; set; }

	/// <summary>
	/// The amount of time between each trigger.
	/// </summary>
	public TimeSpan Interval { get; set; }

	/// <summary>
	/// The service has been trigger or is processing.
	/// </summary>
	public bool IsActive => IsTriggered || IsProcessing;

	/// <summary>
	/// True if the action is processing.
	/// </summary>
	public bool IsProcessing { get; private set; }

	/// <summary>
	/// Is triggered and enough time has passed to process.
	/// </summary>
	public bool IsReadyToProcess => TimeToNextTrigger <= TimeSpan.Zero;

	/// <summary>
	/// True if the throttle service has been triggered.
	/// </summary>
	[DependsOn(nameof(IsProcessing))]
	[DependsOn2(nameof(IsProcessing))]
	public bool IsTriggered => !Queue.IsEmpty;

	/// <summary>
	/// The throttle is triggered and the delay has expired, so it is ready to process.
	/// </summary>
	[DependsOn(nameof(IsTriggered), nameof(IsReadyToProcess))]
	[DependsOn2(nameof(IsTriggered), nameof(IsReadyToProcess))]
	public bool IsTriggeredAndReadyToProcess => IsTriggered && IsReadyToProcess;

	/// <summary>
	/// Number of items in the queue.
	/// </summary>
	public int QueueCount => Queue.Count;

	/// <summary>
	/// If true trigger will queue and be processed. Be careful because queueing on a delay could
	/// end with a never processed queue. Defaults to false, meaning only last trigger processes.
	/// </summary>
	public bool QueueTriggers
	{
		get => Queue.Limit > 1;
		set => Queue.Limit = value ? int.MaxValue : 1;
	}

	/// <summary>
	/// The timespan until next trigger
	/// </summary>
	public abstract TimeSpan TimeToNextTrigger { get; }

	/// <summary>
	/// The current time for the throttle.
	/// </summary>
	protected internal DateTime CurrentTime => _timeService.UtcNow;

	/// <summary>
	/// The Date / Time the service was triggered on.
	/// </summary>
	protected internal DateTime TriggerOnDateTime { get; set; }

	/// <summary>
	/// The Date / Time the service was last processed on.
	/// </summary>
	protected DateTime LastTriggerProcessedOn { get; set; }

	/// <summary>
	/// The next Date / Time to trigger on.
	/// </summary>
	protected abstract DateTime NextTriggerDate { get; }

	/// <summary>
	/// The queue for the data.
	/// </summary>
	protected ISpeedyQueue<T> Queue { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Cancel the current InProcess work.
	/// </summary>
	public void Cancel()
	{
		_cancellationTokenSource?.Cancel();
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
	/// <param name="value"> The value to trigger with. </param>
	/// <param name="force"> An optional flag to immediately trigger if true. Defaults to false. </param>
	public void Trigger(T value, bool force = false)
	{
		if (IsProcessing)
		{
			if (AllowTriggerDuringProcessing)
			{
				Cancel();
			}

			if (!AllowTriggerDuringProcessing && !QueueTriggers)
			{
				// Do not allow new triggers
				return;
			}
		}

		// Optionally turn on force
		_force |= force;

		Queue.Enqueue(value);
		TriggerOnDateTime = NextTriggerDate;

		OnPropertyChanged(nameof(IsTriggered));
		StartWorker();
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
		lock (_workerLock)
		{
			var worker = _worker;

			if (!disposing || (worker == null))
			{
				return;
			}

			if (worker.IsBusy)
			{
				worker.CancelAsync();
			}

			worker.RunWorkerCompleted -= WorkerOnRunWorkerCompleted;
			worker.DoWork -= WorkerOnDoWork;

			_worker = null;
		}
	}

	private void ClearQueue()
	{
		Queue.Clear();
	}

	private void StartWorker()
	{
		//lock (_workerLock)
		//{
		//	// Start the worker if it's not running
		//	if (_worker?.IsBusy != true)
		//	{
		//		_worker?.RunWorkerAsync();
		//	}
		//}
	}

	private void WorkerOnDoWork(object sender, DoWorkEventArgs e)
	{
		var worker = (BackgroundWorker) sender;

		while (!worker.CancellationPending)
		{
			if (!IsTriggeredAndReadyToProcess && !_force)
			{
				// Nothing to do...
				Thread.Sleep(10);
				continue;
			}

			try
			{
				_force = false;

				LastTriggerProcessedOn = TriggerOnDateTime;
				IsProcessing = true;

				if (!Queue.TryDequeue(out var data))
				{
					continue;
				}

				_cancellationTokenSource = new CancellationTokenSource();
				_action(_cancellationTokenSource.Token, data);

				// See if we need to re-trigger due to queued data
				if (!Queue.IsEmpty)
				{
					// Queue up the next run
					TriggerOnDateTime = NextTriggerDate;
				}
			}
			finally
			{
				_cancellationTokenSource?.Dispose();
				_cancellationTokenSource = null;

				IsProcessing = false;
			}
		}
	}

	private void WorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
	{
		if (!IsTriggered)
		{
			return;
		}

		StartWorker();
	}

	#endregion
}