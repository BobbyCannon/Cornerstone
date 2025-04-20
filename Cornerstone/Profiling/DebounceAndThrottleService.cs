#region References

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Collections;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Threading;
using Cornerstone.Weaver;

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
	private bool _disposed;
	private readonly IDateTimeProvider _timeProvider;
	private CancellationTokenSource _workerCancellationTokenSource;
	private Task _workerTask;

	#endregion

	#region Constructors

	/// <summary>
	/// Create an instance of the service for throttling an action.
	/// </summary>
	/// <param name="interval"> The amount of time between each trigger. </param>
	/// <param name="action"> The action to throttle. </param>
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	protected DebounceOrThrottleService(TimeSpan interval, Action<CancellationToken, T> action, IDateTimeProvider timeProvider = null, IDispatcher dispatcher = null) : base(dispatcher)
	{
		AllowTriggerDuringProcessing = false;
		Interval = interval;
		Lock = new ReaderWriterLockTiny();
		ResetRequested = false;
		TriggeredOn = DateTime.MinValue;
		Queue = new SpeedyQueue<T> { Limit = 1 };
		Queue.QueueChanged += QueueOnQueueChanged;

		_action = action;
		_timeProvider = timeProvider ?? DateTimeProvider.RealTime;
		_workerCancellationTokenSource = new CancellationTokenSource();
		_workerTask = Task.Run(() => WorkerLoop(_workerCancellationTokenSource.Token), _workerCancellationTokenSource.Token);
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
	public bool IsTriggered => !Queue.IsEmpty;

	/// <summary>
	/// The throttle is triggered and the delay has expired, so it is ready to process.
	/// </summary>
	[DependsOn(nameof(IsTriggered), nameof(IsReadyToProcess))]
	public bool IsTriggeredAndReadyToProcess => IsTriggered && IsReadyToProcess;

	/// <summary>
	/// True if the next trigger was forced.
	/// </summary>
	public bool IsTriggeredForced { get; private set; }

	/// <summary>
	/// The Date / Time the service was last processed on.
	/// </summary>
	public DateTime LastProcessedOn { get; protected set; }

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
	public bool ResetRequested { get; private set; }

	/// <summary>
	/// The timespan until next trigger
	/// </summary>
	public abstract TimeSpan TimeToNextTrigger { get; }

	/// <summary>
	/// The Date / Time the service was triggered on.
	/// </summary>
	public DateTime TriggeredOn { get; protected set; }

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
		_cancellationTokenSource?.Cancel();
	}

	/// <inheritdoc />
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
		if (_disposed)
		{
			return;
		}

		ResetRequested = true;

		// Wait for worker to process reset
		const int maxAttempts = 100; // Wait up to ~1 second (10ms * 100)
		for (var i = 0; i < maxAttempts; i++)
		{
			if (!ResetRequested)
			{
				return; // Reset completed
			}
			await Task.Delay(10).ConfigureAwait(false);
		}

		// Optional: Log or throw if reset not completed
		// throw new TimeoutException("Reset not completed within timeout period.");
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
		IsTriggeredForced |= force;

		Queue.Enqueue(value);
		TriggeredOn = NextTriggerDate;

		OnPropertyChanged(nameof(IsTriggered));
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

		// Call ResetAsync synchronously for compatibility
		ResetAsync().GetAwaiter().GetResult();

		Queue.QueueChanged -= QueueOnQueueChanged;

		_workerCancellationTokenSource?.Cancel();
		try
		{
			_workerTask?.Wait(1000); // Wait briefly for task to complete
		}
		catch (OperationCanceledException)
		{
		}
		catch (AggregateException)
		{
		}

		_workerCancellationTokenSource?.Dispose();
		_workerCancellationTokenSource = null;
		_workerTask = null;
	}

	private void QueueOnQueueChanged(object sender, EventArgs e)
	{
		OnPropertyChanged(nameof(IsTriggered));
		OnPropertyChanged(nameof(QueueCount));
	}

	private async Task WorkerLoop(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested && !_disposed)
		{
			// Check for reset request
			if (ResetRequested)
			{
				try
				{
					Lock.EnterWriteLock();

					IsTriggeredForced = false;

					Queue.Clear();
					LastProcessedOn = DateTime.MinValue;
					TriggeredOn = DateTime.MinValue;
					ResetRequested = false;
				}
				finally
				{
					Lock.ExitWriteLock();
				}

				await Task.Delay(10, cancellationToken).ConfigureAwait(false);
				continue;
			}

			if (!IsTriggeredAndReadyToProcess && !IsTriggeredForced)
			{
				// Nothing to do, wait briefly
				await Task.Delay(10, cancellationToken).ConfigureAwait(false);
				continue;
			}

			try
			{
				Lock.EnterWriteLock();

				IsTriggeredForced = false;

				LastProcessedOn = TriggeredOn;
				IsProcessing = true;

				if (!Queue.TryDequeue(out var data))
				{
					continue;
				}

				_cancellationTokenSource = new CancellationTokenSource();

				try
				{
					_action(_cancellationTokenSource.Token, data);
				}
				catch (OperationCanceledException)
				{
				}
				catch (Exception ex)
				{
					// Handle or log exception as needed
					Debug.WriteLine($"Error in action: {ex.Message}");
				}

				// See if we need to re-trigger due to queued data
				if (!Queue.IsEmpty)
				{
					// Queue up the next run
					TriggeredOn = NextTriggerDate;
				}
			}
			finally
			{
				_cancellationTokenSource?.Dispose();
				_cancellationTokenSource = null;

				IsProcessing = false;
				Lock.ExitWriteLock();
			}
		}
	}

	#endregion
}