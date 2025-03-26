#region References

using System.ComponentModel;
using System.Threading;

#endregion

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace Cornerstone.Presentation;

/// <summary>
/// Represents a view model that also supports a working
/// thread for processing / monitoring.
/// </summary>
public abstract class WorkerViewModel : ViewModel
{
	#region Fields

	private readonly BackgroundWorker _worker;

	#endregion

	#region Constructors

	protected WorkerViewModel(int workerDelay, IDispatcher dispatcher)
		: base(dispatcher)
	{
		WorkerStatus = WorkerStatus.Stopped;

		_worker = new BackgroundWorker();

		//_worker.DoWork += WorkerDoWork;
		//_worker.ProgressChanged += WorkerProgressChanged;
		_worker.RunWorkerCompleted += WorkerRunWorkerCompleted;

		WeakEventManager.Add<BackgroundWorker, WorkerViewModel, DoWorkEventArgs>(_worker, nameof(_worker.DoWork), this, WorkerDoWork);
		WeakEventManager.Add<BackgroundWorker, WorkerViewModel, ProgressChangedEventArgs>(_worker, nameof(_worker.ProgressChanged), this, WorkerProgressChanged);
		//WeakEventManager.Add<BackgroundWorker, RunWorkerCompletedEventArgs>(_worker, nameof(_worker.RunWorkerCompleted), WorkerRunWorkerCompleted);
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
	protected bool IsWorking => _worker.IsBusy || (WorkerStatus != WorkerStatus.Stopped);

	protected int WorkerDelay { get; set; }

	protected WorkerStatus WorkerStatus { get; private set; }

	#endregion

	#region Methods

	/// <summary>
	/// Start working.
	/// </summary>
	public void StartWorking()
	{
		if (IsWorking)
		{
			return;
		}

		WorkerStatus = WorkerStatus.Starting;

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

		WorkerStatus = WorkerStatus.Stopping;

		_worker.CancelAsync();
	}

	protected abstract void Work();

	protected virtual void WorkerProgressChanged(object sender, ProgressChangedEventArgs e)
	{
	}

	protected virtual void WorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
	{
		WorkerStatus = WorkerStatus.Stopped;
	}

	private void WorkerDoWork(object sender, DoWorkEventArgs e)
	{
		WorkerStatus = WorkerStatus.Started;

		while (!_worker.CancellationPending)
		{
			Work();
			Thread.Sleep(WorkerDelay);
		}
	}

	#endregion
}