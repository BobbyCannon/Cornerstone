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

public class DebounceThrottleManager : WorkerManager, IDisposable
{
	#region Fields

	protected internal readonly IDateTimeProvider DateTimeProvider;
	private readonly PresentationList<DebounceThrottleProxy> _proxies;

	#endregion

	#region Constructors

	private DebounceThrottleManager(IDateTimeProvider timeProvider, int workerDelay)
		: base(workerDelay)
	{
		_proxies = [];

		DateTimeProvider = timeProvider ?? Runtime.DateTimeProvider.RealTime;
	}

	#endregion

	#region Methods

	public static DebounceThrottleManager Create(IDateTimeProvider timeProvider = null, int workerDelay = 10)
	{
		var response = new DebounceThrottleManager(timeProvider, workerDelay);
		response.Initialize();
		return response;
	}

	public DebounceProxy CreateDebounce(TimeSpan interval, Action<CancellationToken, object, bool> action)
	{
		var response = new DebounceProxy(this, interval, action);
		_proxies.Add(response);
		return response;
	}

	public ThrottleProxy CreateThrottle(TimeSpan interval, Action<CancellationToken, object, bool> action)
	{
		var response = new ThrottleProxy(this, interval, action);
		_proxies.Add(response);
		return response;
	}

	public void Dispose()
	{
		StopWorking();
		Uninitialize();
	}

	public void Release(DebounceThrottleProxy proxy)
	{
		// Remove then cancel / reset the proxy
		_proxies.Remove(proxy);
		proxy.Cancel();
	}

	public static DebounceThrottleManager Start(IDateTimeProvider timeProvider = null, int workerDelay = 10)
	{
		var response = new DebounceThrottleManager(timeProvider, workerDelay);
		response.Initialize();
		response.StartWorking();
		return response;
	}

	public override void Uninitialize()
	{
		_proxies.Clear();
		base.Uninitialize();
	}

	public override void Update()
	{
		foreach (var proxy in _proxies)
		{
			if (proxy.IsProcessing)
			{
				continue;
			}

			if (!proxy.IsTriggeredAndReadyToProcess
				&& !proxy.IsTriggeredForced)
			{
				continue;
			}

			proxy.IsProcessing = true;
			Task.Run(proxy.Process);
		}
		base.Update();
	}

	protected internal void Trigger(DebounceThrottleProxy proxy)
	{
		WakeUp();
	}

	/// <summary>
	/// Auto-calculates the optimal sleep time based on the earliest next trigger.
	/// This is called every cycle by the base WorkerManager.
	/// </summary>
	protected override int? CalculateDynamicDelay()
	{
		var minDelay = TimeSpan.MaxValue;
		var snapshot = _proxies.ToArray();

		foreach (var proxy in snapshot)
		{
			if (proxy.IsProcessing && !proxy.AllowTriggerDuringProcessing)
			{
				continue;
			}

			if (!proxy.IsTriggered && !proxy.IsTriggeredForced)
			{
				continue;
			}

			if (proxy.IsTriggeredForced || (proxy.TimeToNextTrigger <= TimeSpan.Zero))
			{
				return 0;
			}

			var timeToWait = proxy.TimeToNextTrigger;
			if (timeToWait < minDelay)
			{
				minDelay = timeToWait;
			}
		}

		if (minDelay == TimeSpan.MaxValue)
		{
			return null;
		}

		var ms = (int) Math.Min(minDelay.TotalMilliseconds, WorkerDelay * 10);
		return ms > 0 ? ms : 0;
	}

	#endregion
}

public class DebounceProxy : DebounceThrottleProxy
{
	#region Constructors

	internal DebounceProxy(DebounceThrottleManager manager, TimeSpan interval, Action<CancellationToken, object, bool> action)
		: base(manager, interval, action)
	{
	}

	#endregion

	#region Properties

	/// <summary>
	/// The timespan until next trigger
	/// </summary>
	public override TimeSpan TimeToNextTrigger
	{
		get
		{
			if (TriggeredOn == DateTime.MinValue)
			{
				return Interval;
			}

			var elapsed = CurrentTime - TriggeredOn;
			if (elapsed < TimeSpan.Zero)
			{
				return TimeSpan.Zero;
			}

			return Interval - elapsed;
		}
	}

	protected override DateTime NextTriggerDate => CurrentTime;

	#endregion
}

public class ThrottleProxy : DebounceThrottleProxy
{
	#region Constructors

	internal ThrottleProxy(DebounceThrottleManager manager, TimeSpan interval, Action<CancellationToken, object, bool> action)
		: base(manager, interval, action)
	{
	}

	#endregion

	#region Properties

	/// <summary>
	/// The timespan until next trigger
	/// </summary>
	public override TimeSpan TimeToNextTrigger
	{
		get
		{
			if (TriggeredOn == DateTime.MinValue)
			{
				return Interval;
			}

			// Edge case that probably will never happen unless time is being
			// controlled but if trigger on is exactly current time then the delay
			// and the queue is empty then we should consider the trigger processed
			if ((TriggeredOn == CurrentTime) && Queue.IsEmpty)
			{
				return Interval;
			}

			var e = TriggeredOn - CurrentTime;
			if (e <= TimeSpan.Zero)
			{
				return TimeSpan.Zero;
			}

			return e;
		}
	}

	/// <summary>
	/// Calculate the next trigger date.
	/// </summary>
	protected override DateTime NextTriggerDate
	{
		get
		{
			if (TriggeredOn == DateTime.MinValue)
			{
				return CurrentTime;
			}

			if ((TriggeredOn > CurrentTime)
				|| (LastProcessedOn < TriggeredOn))
			{
				return TriggeredOn;
			}

			var timeSinceLastTrigger = CurrentTime - TriggeredOn;
			if ((timeSinceLastTrigger == TimeSpan.Zero)
				&& (LastProcessedOn == TriggeredOn))
			{
				return LastProcessedOn + Interval;
			}

			if (timeSinceLastTrigger < Interval)
			{
				// todo: need test for this
				return CurrentTime + (Interval - timeSinceLastTrigger);
			}

			return CurrentTime;
		}
	}

	#endregion
}

public abstract partial class DebounceThrottleProxy : Notifiable
{
	#region Fields

	internal CancellationTokenSource CurrentActionCts;
	private readonly Action<CancellationToken, object, bool> _action;
	private readonly DebounceThrottleManager _manager;

	#endregion

	#region Constructors

	protected DebounceThrottleProxy(DebounceThrottleManager manager, TimeSpan interval, Action<CancellationToken, object, bool> action)
	{
		_manager = manager;
		_action = action;
		_interval = interval;

		Lock = new ReaderWriterLockTiny();
		Queue = new SpeedyQueue<object> { MaxItems = 1 };
		Queue.QueueChanged += QueueOnQueueChanged;
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
	public partial bool IsProcessing { get; internal set; }

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
		get => Queue.MaxItems > 1;
		set => Queue.MaxItems = value ? int.MaxValue : 1;
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
	protected internal DateTime CurrentTime => _manager.DateTimeProvider.UtcNow;

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
	protected SpeedyQueue<object> Queue { get; }

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
			Interlocked.Exchange(ref CurrentActionCts, null)?.Cancel();
		}
		catch
		{
			// Ignore already disposed issues
		}
	}

	public void Reset()
	{
		ResetRequested = true;

		Cancel();
		Lock.EnterWriteLock();

		try
		{
			Queue.Clear();
			TriggeredOn = DateTime.MinValue;
			LastProcessedOn = DateTime.MinValue;
			IsTriggeredForced = false;
			ResetRequested = false;
			Interlocked.Exchange(ref CurrentActionCts, null)?.Cancel();
		}
		finally
		{
			Lock.ExitWriteLock();
		}
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
	/// Trigger the service. Will be triggered after the timespan.
	/// </summary>
	/// <param name="value"> The value to trigger with. </param>
	/// <param name="force"> An optional flag to immediately trigger if true. Defaults to false. </param>
	public void Trigger(object value, bool force = false)
	{
		// Always allow queuing when QueueTriggers is enabled, regardless of processing state
		if (IsProcessing && !AllowTriggerDuringProcessing && !QueueTriggers)
		{
			return;
		}

		if (IsProcessing && AllowTriggerDuringProcessing)
		{
			Interlocked.Exchange(ref CurrentActionCts, null)?.Cancel();
		}

		IsTriggeredForced |= force;
		Queue.Enqueue(value);
		TriggeredOn = NextTriggerDate;
		OnPropertyChanged(nameof(IsTriggered));

		_manager.Trigger(this);
	}

	internal void Process()
	{
		try
		{
			Lock.EnterWriteLock();

			var forced = IsTriggeredForced;
			IsTriggeredForced = false;
			LastProcessedOn = TriggeredOn;
			IsProcessing = true;

			if (!Queue.TryDequeue(out var data))
			{
				return;
			}

			var actionCts = new CancellationTokenSource();
			var oldCts = Interlocked.Exchange(ref CurrentActionCts, actionCts);
			oldCts?.Cancel();
			oldCts?.Dispose();

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
				if (Interlocked.CompareExchange(ref CurrentActionCts, null, actionCts) == actionCts)
				{
					actionCts.Dispose();
				}
			}

			if (!Queue.IsEmpty)
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

	private void QueueOnQueueChanged(object sender, EventArgs e)
	{
		OnPropertyChanged(nameof(IsTriggered));
		OnPropertyChanged(nameof(QueueCount));
	}

	#endregion
}