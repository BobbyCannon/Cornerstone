#region References

using System.ComponentModel;
using System.Threading;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace Cornerstone.Threading;

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

	protected WorkerViewModel(int workerDelay)
	{
		WorkerStatus = WorkerStatus.Stopped;
		WorkerDelay = workerDelay;

		_worker = new BackgroundWorker();
		_worker.WorkerReportsProgress = true;
		_worker.WorkerSupportsCancellation = true;
	}

	#endregion

	#region Properties

	/// <summary>
	/// True if the manager is requesting cancelling.
	/// </summary>
	public bool IsCancellingPending => _worker?.CancellationPending ?? false;

	protected int WorkerDelay { get; set; }

	protected WorkerStatus WorkerStatus { get; private set; }

	#endregion

	#region Methods

	public override void InitializeLifecycle()
	{
		_worker.DoWork += WorkerDoWork;
		_worker.ProgressChanged += WorkerProgressChanged;
		_worker.RunWorkerCompleted += WorkerRunWorkerCompleted;
		base.InitializeLifecycle();
	}

	public override bool IsLifecycleStarted()
	{
		return _worker.IsBusy || (WorkerStatus != WorkerStatus.Stopped);
	}

	public override void StartLifecycle()
	{
		if (IsLifecycleStarted())
		{
			return;
		}

		WorkerStatus = WorkerStatus.Starting;

		_worker.RunWorkerAsync();
		base.StartLifecycle();
	}

	public override void StopLifecycle()
	{
		if (!_worker.IsBusy)
		{
			return;
		}

		WorkerStatus = WorkerStatus.Stopping;

		_worker.CancelAsync();
	}

	public override void UninitializeLifecycle()
	{
		_worker.DoWork -= WorkerDoWork;
		_worker.ProgressChanged -= WorkerProgressChanged;
		_worker.RunWorkerCompleted -= WorkerRunWorkerCompleted;
		base.UninitializeLifecycle();
	}

	protected abstract void Update();

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
			try
			{
				Update();
			}
			catch
			{
				if (!_worker.CancellationPending)
				{
					Debugging.BreakIfAttached();
				}
			}

			Thread.Sleep(WorkerDelay);
		}
	}

	#endregion
}