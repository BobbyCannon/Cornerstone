#region References

using System.ComponentModel;
using System.Threading;
using Cornerstone.Data;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Threading;

/// <summary>
/// Represents a manager that also supports a working thread for processing / monitoring.
/// Now supports dynamic delay calculation via override while remaining fully backwards-compatible.
/// </summary>
public abstract partial class WorkerManager : Manager
{
	#region Fields

	private readonly AutoResetEvent _wakeEvent;
	private readonly BackgroundWorker _worker;
	private bool _workerStarting;
	private bool _workerStopping;

	#endregion

	#region Constructors

	protected WorkerManager(int workerDelay = 10)
	{
		_wakeEvent = new(false);
		_workerStarting = false;
		_workerStopping = false;
		_worker = new BackgroundWorker();

		WorkerDelay = workerDelay;

		_worker.WorkerReportsProgress = true;
		_worker.WorkerSupportsCancellation = true;
	}

	#endregion

	#region Properties

	/// <summary>
	/// True if the manager is requesting cancelling.
	/// </summary>
	public bool IsCancellingPending => _worker?.CancellationPending ?? false;

	/// <summary>
	/// True if the manager is working.
	/// </summary>
	public bool IsWorking => _worker.IsBusy || _workerStarting || _workerStopping;

	[Notify]
	protected partial int WorkerDelay { get; set; }

	#endregion

	#region Methods

	public override void Initialize()
	{
		_worker.DoWork += WorkerDoWork;
		_worker.ProgressChanged += WorkerProgressChanged;
		_worker.RunWorkerCompleted += WorkerRunWorkerCompleted;
		base.Initialize();
	}

	/// <summary>
	/// Start working.
	/// </summary>
	public virtual void StartWorking()
	{
		if (IsWorking)
		{
			return;
		}

		_workerStarting = true;
		_worker.RunWorkerAsync();
	}

	/// <summary>
	/// Stop working.
	/// </summary>
	public void StopWorking()
	{
		if (!_worker.IsBusy)
		{
			return;
		}

		_workerStopping = true;
		_worker.CancelAsync();
		_wakeEvent.Set();
	}

	public override void Uninitialize()
	{
		_worker.DoWork -= WorkerDoWork;
		_worker.ProgressChanged -= WorkerProgressChanged;
		_worker.RunWorkerCompleted -= WorkerRunWorkerCompleted;
		base.Uninitialize();
	}

	/// <summary>
	/// Override in derived classes to provide a dynamic delay.
	/// Return:
	/// - null or negative → use the fixed WorkerDelay
	/// - 0                → no sleep / immediate next iteration (or yield)
	/// - positive         → sleep this many milliseconds this cycle
	/// 
	/// This allows managers like DebounceThrottleManager to auto-calculate
	/// the optimal wait time until the next proxy needs processing.
	/// </summary>
	protected virtual int? CalculateDynamicDelay()
	{
		return null; // Default = use fixed WorkerDelay (backwards compatible)
	}

	protected virtual void Delay(int delay)
	{
		if (delay <= 0)
		{
			// Yield to keep the loop responsive
			Thread.Yield();
			return;
		}

		// Safety: never sleep longer than 1 second so we can respond to cancellation
		if (delay > 1000)
		{
			delay = 1000;
		}

		// Wait on the event so WakeUp() can wake us early
		_wakeEvent.WaitOne(delay);
	}

	protected virtual void WakeUp()
	{
		// Wake the worker thread immediately so it can re-evaluate
		// CalculateDynamicDelay() and potentially process right away.
		_wakeEvent.Set();
	}

	protected virtual void WorkerProgressChanged(object sender, ProgressChangedEventArgs e)
	{
	}

	protected virtual void WorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
	{
		_workerStarting = false;
		_workerStopping = false;
	}

	private void WorkerDoWork(object sender, DoWorkEventArgs e)
	{
		_workerStarting = false;

		while (!_worker.CancellationPending)
		{
			Update();

			// Try dynamic delay first, fall back to fixed WorkerDelay
			var dynamicMs = CalculateDynamicDelay();
			var delayMs = dynamicMs ?? WorkerDelay;

			Delay(delayMs);
		}
	}

	#endregion
}