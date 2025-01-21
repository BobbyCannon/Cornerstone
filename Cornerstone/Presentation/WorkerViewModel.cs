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

	protected readonly IWeakEventManager WeakEventManager;
	private readonly BackgroundWorker _worker;
	private bool _workerStarting;
	private bool _workerStopping;

	#endregion

	#region Constructors

	protected WorkerViewModel(int workerDelay, IWeakEventManager weakEventManager, IDispatcher dispatcher)
		: base(dispatcher)
	{
		_workerStarting = false;
		_workerStopping = false;
		_worker = new BackgroundWorker();

		//_worker.DoWork += WorkerDoWork;
		//_worker.ProgressChanged += WorkerProgressChanged;
		//_worker.RunWorkerCompleted += WorkerRunWorkerCompleted;

		WeakEventManager = weakEventManager;
		WeakEventManager.Add<BackgroundWorker, DoWorkEventArgs>(_worker, nameof(_worker.DoWork), WorkerDoWork);
		WeakEventManager.Add<BackgroundWorker, ProgressChangedEventArgs>(_worker, nameof(_worker.ProgressChanged), WorkerProgressChanged);
		WeakEventManager.Add<BackgroundWorker, RunWorkerCompletedEventArgs>(_worker, nameof(_worker.RunWorkerCompleted), WorkerRunWorkerCompleted);
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
	protected bool IsWorking => _worker.IsBusy || _workerStarting || _workerStopping;

	protected int WorkerDelay { get; set; }

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

	protected abstract void Work();

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
			Work();
			Thread.Sleep(WorkerDelay);
		}
	}

	#endregion
}