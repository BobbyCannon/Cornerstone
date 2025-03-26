#region References

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Managers;

#endregion

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace Cornerstone.Threading;

/// <summary>
/// Represents a manager that also supports a working
/// thread for processing / monitoring.
/// </summary>
public abstract class WorkerManager : Manager
{
	#region Fields

	private readonly BackgroundWorker _worker;
	private bool _workerStarting;
	private bool _workerStopping;

	#endregion

	#region Constructors

	protected WorkerManager(int workerDelay, IDispatcher dispatcher)
		: base(dispatcher)
	{
		_workerStarting = false;
		_workerStopping = false;
		_worker = new BackgroundWorker();

		//_worker.DoWork += WorkerDoWork;
		//_worker.ProgressChanged += WorkerProgressChanged;
		//_worker.RunWorkerCompleted += WorkerRunWorkerCompleted;

		WeakEventManager.Add<BackgroundWorker, WorkerManager, DoWorkEventArgs>(_worker, nameof(_worker.DoWork), this, WorkerDoWork);
		WeakEventManager.Add<BackgroundWorker, WorkerManager, ProgressChangedEventArgs>(_worker, nameof(_worker.ProgressChanged), this, WorkerProgressChanged);
		WeakEventManager.Add<BackgroundWorker, WorkerManager, RunWorkerCompletedEventArgs>(_worker, nameof(_worker.RunWorkerCompleted), this, WorkerRunWorkerCompleted);
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

	/// <summary>
	/// The working method.
	/// </summary>
	/// <param name="elapsed"> The time since last time called. </param>
	protected abstract void Work(TimeSpan elapsed);

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

		var watch = new Stopwatch();

		while (!_worker.CancellationPending)
		{
			Work(watch.Elapsed);
			watch.Restart();
			Thread.Sleep(WorkerDelay);
		}
	}

	#endregion
}