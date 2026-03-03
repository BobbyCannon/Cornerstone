#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Runtime;
using Cornerstone.Threading;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// The service to debounce or throttle work that supports cancellation.
/// </summary>
public abstract partial class DebounceOrThrottleService<T> : Notifiable, IDisposable
{
	#region Fields

	private readonly Action<CancellationToken, T, bool> _action;
	private CancellationTokenSource _currentActionCts;
	private bool _disposed;
	private readonly IDateTimeProvider _timeProvider;
	private CancellationTokenSource _workerCts;
	private Task _workerTask = Task.CompletedTask;

	#endregion

	#region Constructors

	/// <summary>
	/// Create an instance of the service for throttling an action.
	/// </summary>
	/// <param name="interval"> The amount of time between each trigger. </param>
	/// <param name="action"> The action to throttle. </param>
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	protected DebounceOrThrottleService(TimeSpan interval, Action<CancellationToken, T, bool> action, IDateTimeProvider timeProvider = null)
	{
		Interval = interval;
		Lock = new ReaderWriterLockTiny();
		Queue = new SpeedyQueue<T> { Limit = 1 };
		Queue.QueueChanged += QueueOnQueueChanged;
		_action = action;
		_timeProvider = timeProvider ?? DateTimeProvider.RealTime;
		_workerCts = new CancellationTokenSource();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Allow triggering during processing.
	/// </summary>
	[Notify]
	public partial bool AllowTriggerDuringProcessing { get; set; }

	/// <summary>
	/// The amount of time between each trigger.
	/// </summary>
	[Notify]
	public partial TimeSpan Interval { get; set; }

	/// <summary>
	/// The service has been trigger or is processing.
	/// </summary>
	[DependsOn(nameof(IsTriggered), nameof(IsProcessing))]
	public bool IsActive => IsTriggered || IsProcessing;

	/// <summary>
	/// True if the action is processing.
	/// </summary>
	[Notify]
	public partial bool IsProcessing { get; private set; }

	/// <summary>
	/// Is triggered and enough time has passed to process.
	/// </summary>
	public bool IsReadyToProcess => TimeToNextTrigger <= TimeSpan.Zero;

	/// <summary>
	/// True if the throttle service has been triggered.
	/// </summary>
	public bool IsTriggered => !Queue.IsEmpty;

	/// <summary>
	/// The throttle is triggered and the delay has expired, so it is ready to process.
	/// </summary>
	[DependsOn(nameof(IsTriggered), nameof(IsReadyToProcess))]
	public bool IsTriggeredAndReadyToProcess => IsTriggered && IsReadyToProcess;

	/// <summary>
	/// True if the next trigger was forced.
	/// </summary>
	[Notify]
	public partial bool IsTriggeredForced { get; private set; }

	/// <summary>
	/// The Date / Time the service was last processed on.
	/// </summary>
	[Notify]
	public partial DateTime LastProcessedOn { get; protected set; }

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
	/// A reset has been requested.
	/// </summary>
	[Notify]
	public partial bool ResetRequested { get; private set; }

	/// <summary>
	/// The timespan until next trigger
	/// </summary>
	public abstract TimeSpan TimeToNextTrigger { get; }

	/// <summary>
	/// The Date / Time the service was triggered on.
	/// </summary>
	[Notify]
	public partial DateTime TriggeredOn { get; protected set; }

	/// <summary>
	/// The current time for the throttle.
	/// </summary>
	protected internal DateTime CurrentTime => _timeProvider.UtcNow;

	/// <summary>
	/// Lock for tracking worker / reset process.
	/// </summary>
	protected ReaderWriterLockTiny Lock { get; }

	/// <summary>
	/// The next Date / Time to trigger on.
	/// </summary>
	protected abstract DateTime NextTriggerDate { get; }

	/// <summary>
	/// The queue for the data.
	/// </summary>
	protected SpeedyQueue<T> Queue { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Cancel the current InProcess work.
	/// </summary>
	public void Cancel()
	{
		Queue.Clear();
		TriggeredOn = DateTime.MinValue;
		try
		{
			Interlocked.Exchange(ref _currentActionCts, null)?.Cancel();
		}
		catch
		{
			// Ignore already disposed issues
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Reset the throttle service
	/// </summary>
	public async Task ResetAsync()
	{
		ResetRequested = true;

		_workerCts.Cancel();

		if (!_workerTask.IsCompleted)
		{
			try
			{
				await _workerTask.ConfigureAwait(false);
			}
			catch (TaskCanceledException)
			{
				// Ignore cancellation exception
			}
		}

		Lock.EnterWriteLock();

		try
		{
			Queue.Clear();
			TriggeredOn = DateTime.MinValue;
			LastProcessedOn = DateTime.MinValue;
			IsTriggeredForced = false;
			ResetRequested = false;
			Interlocked.Exchange(ref _currentActionCts, null)?.Cancel();
		}
		finally
		{
			var oldCts = _workerCts;
			_workerCts = new CancellationTokenSource();
			oldCts?.Dispose();

			Lock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Trigger the service. Will be trigger after the timespan.
	/// </summary>
	/// <param name="value"> The value to trigger with. </param>
	/// <param name="force"> An optional flag to immediately trigger if true. Defaults to false. </param>
	public void Trigger(T value, bool force = false)
	{
		if (_disposed)
		{
			return;
		}

		if (IsProcessing && !AllowTriggerDuringProcessing && !QueueTriggers)
		{
			return;
		}

		if (IsProcessing && AllowTriggerDuringProcessing)
		{
			Interlocked.Exchange(ref _currentActionCts, null)?.Cancel();
		}

		IsTriggeredForced |= force;
		Queue.Enqueue(value);
		TriggeredOn = NextTriggerDate;
		OnPropertyChanged(nameof(IsTriggered));
		EnsureWorkerRunning();
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		try
		{
			_workerCts.Cancel();

			Interlocked.Exchange(ref _currentActionCts, null)?.Cancel();

			if ((_workerTask != null)
				&& (_workerTask.Status != TaskStatus.WaitingForActivation))
			{
				_workerTask?.Wait(1000);
			}
		}
		catch
		{
			// Ignore any wait issues.
		}

		_workerCts.Dispose();
		_currentActionCts?.Dispose();

		Queue.QueueChanged -= QueueOnQueueChanged;
	}

	private void EnsureWorkerRunning()
	{
		// Quick early exit if obviously no work or already cancelled
		if (Queue.IsEmpty || _workerCts.IsCancellationRequested)
		{
			return;
		}

		if (!Lock.TryEnterWriteLock())
		{
			return;
		}

		try
		{
			// Double-check everything under the lock
			if (_workerTask.IsCompleted && !Queue.IsEmpty && !_workerCts.IsCancellationRequested)
			{
				_workerTask = Task.Run(WorkerLoop, _workerCts.Token);
			}
		}
		finally
		{
			Lock.ExitWriteLock();
		}
	}

	private void QueueOnQueueChanged(object sender, EventArgs e)
	{
		OnPropertyChanged(nameof(IsTriggered));
		OnPropertyChanged(nameof(QueueCount));
	}

	private async Task WorkerLoop()
	{
		var source = _workerCts;
		if (source == null)
		{
			return;
		}

		while (!source.IsCancellationRequested)
		{
			// Wait until something is ready or we get cancelled
			while (!IsTriggeredAndReadyToProcess
					&& !IsTriggeredForced
					&& !source.IsCancellationRequested)
			{
				await Task.Delay(10, source.Token).ConfigureAwait(false);
			}

			try
			{
				Lock.EnterWriteLock();

				var forced = IsTriggeredForced;
				IsTriggeredForced = false;
				LastProcessedOn = TriggeredOn;
				IsProcessing = true;

				if (!Queue.TryDequeue(out var data))
				{
					continue;
				}

				var actionCts = CancellationTokenSource.CreateLinkedTokenSource(source.Token);
				Interlocked.Exchange(ref _currentActionCts, actionCts);

				try
				{
					_action?.Invoke(actionCts.Token, data, forced);
				}
				catch (Exception)
				{
					//Debug.WriteLine($"Error in action: {ex}");
				}
				finally
				{
					if (Interlocked.CompareExchange(ref _currentActionCts, null, actionCts) == actionCts)
					{
						actionCts.Dispose();
					}
				}

				if (Queue.IsEmpty)
				{
					break;
				}
				else
				{
					TriggeredOn = NextTriggerDate;
				}
			}
			finally
			{
				IsProcessing = false;
				Lock.ExitWriteLock();
			}
		}

		if (_workerCts.IsCancellationRequested)
		{
			var oldCts = _workerCts;
			_workerCts = new CancellationTokenSource();
			oldCts?.Dispose();
		}
	}

	#endregion
}