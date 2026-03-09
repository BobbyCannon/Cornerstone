#region References

using System.ComponentModel;
using System.Threading;
using Cornerstone.Data;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Threading;

/// <summary>
/// Represents a manager that also supports a working
/// thread for processing / monitoring.
/// </summary>
public abstract partial class WorkerManager : Manager
{
	#region Fields

	private readonly BackgroundWorker _worker;
	private bool _workerStarting;
	private bool _workerStopping;

	#endregion

	#region Constructors

	protected WorkerManager(int workerDelay)
	{
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
	public void StartWorking()
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
	}

	public override void Uninitialize()
	{
		_worker.DoWork += WorkerDoWork;
		_worker.ProgressChanged += WorkerProgressChanged;
		_worker.RunWorkerCompleted += WorkerRunWorkerCompleted;
		base.Uninitialize();
	}

	protected virtual void Delay(int delay)
	{
		Thread.Sleep(delay);
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
			Delay(WorkerDelay);
		}
	}

	#endregion
}