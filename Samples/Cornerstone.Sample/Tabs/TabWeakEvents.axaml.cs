#region References

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabWeakEvents : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Weak Events";

	#endregion

	#region Fields

	private BackgroundWorker _worker;

	#endregion

	#region Constructors

	public TabWeakEvents()
	{
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public bool ContextRunning { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void OnPropertyChanged(string propertyName)
	{
		switch (propertyName)
		{
			case nameof(ContextRunning):
			{
				ToggleWorker(ContextRunning);
				break;
			}
		}

		base.OnPropertyChanged(propertyName);
	}

	protected virtual void OnRegularTrigger()
	{
		RegularTrigger?.Invoke(this, EventArgs.Empty);
	}

	protected virtual void OnWeakTrigger()
	{
		RunGarbageCollection();
		WeakTrigger?.Invoke(this, EventArgs.Empty);
	}

	private void RegularTriggerOnClick(object sender, RoutedEventArgs e)
	{
		OnRegularTrigger();
	}

	private static void RunGarbageCollection()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		// Does not work no Android
		//GC.WaitForFullGCApproach();
	}

	private void ToggleWorker(bool enable)
	{
		if ((_worker == null) && enable)
		{
			Log.Clear();

			_worker = new BackgroundWorker();
			_worker.DoWork += WorkerOnDoWork;
			_worker.ProgressChanged += WorkerOnProgressChanged;
			_worker.WorkerReportsProgress = true;
			_worker.WorkerSupportsCancellation = true;
			_worker.RunWorkerAsync(this);
			return;
		}

		if ((_worker != null) && !enable)
		{
			_worker.CancelAsync();
			_worker = null;
		}
	}

	private void WeakTriggerOnClick(object sender, RoutedEventArgs e)
	{
		OnWeakTrigger();
	}

	[SuppressMessage("ReSharper", "UnusedVariable")]
	private void WorkerOnDoWork(object sender, DoWorkEventArgs e)
	{
		var worker = (BackgroundWorker) sender;
		var page = (TabWeakEvents) e.Argument;
		var weakProcessor = new PageWeakProcessor(page);
		var normalProcessor = new PageRegularProcessor(page);
		var count = 0;

		while (!worker.CancellationPending)
		{
			worker.ReportProgress(1);
			Thread.Sleep(25);

			if (count++ >= 80)
			{
				worker.ReportProgress(0);
				count = 0;
			}
		}
	}

	private void WorkerOnProgressChanged(object sender, ProgressChangedEventArgs e)
	{
		switch (e.ProgressPercentage)
		{
			case 0:
			{
				this.Dispatch(() => Log.AppendText(Environment.NewLine));
				break;
			}
			case 2:
			{
				this.Dispatch(() => Log.AppendText(" * "));
				break;
			}
			default:
			{
				this.Dispatch(() => Log.AppendText(e.ProgressPercentage.ToString()));
				break;
			}
		}
	}

	#endregion

	#region Events

	public event EventHandler RegularTrigger;

	public event EventHandler WeakTrigger;

	#endregion

	#region Classes

	public class PageRegularProcessor
	{
		#region Fields

		private readonly TabWeakEvents _page;

		#endregion

		#region Constructors

		public PageRegularProcessor(TabWeakEvents page)
		{
			_page = page;
			_page.RegularTrigger += OnRegularTrigger;
		}

		#endregion

		#region Methods

		/// <summary>
		/// This will fail because it never collects.
		/// </summary>
		private void OnRegularTrigger(object sender, EventArgs e)
		{
			try
			{
				_page.Log.AppendText(" R ");
			}
			catch (Exception ex)
			{
				_page.Log.AppendText(Environment.NewLine);
				_page.Log.AppendText(ex.Message);
			}
		}

		#endregion
	}

	public class PageWeakProcessor
	{
		#region Fields

		private readonly TabWeakEvents _page;
		private readonly WeakEventManager _weakEventManager;

		#endregion

		#region Constructors

		public PageWeakProcessor(TabWeakEvents page)
		{
			_page = page;
			_weakEventManager = new WeakEventManager();
			_weakEventManager.Add<TabWeakEvents, EventArgs>(_page, nameof(_page.WeakTrigger), OnWeakTrigger);
			//_page.WeakTrigger += OnWeakTrigger;
		}

		~PageWeakProcessor()
		{
			_weakEventManager.Remove(_page);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Comment out the [WeakAttribute] to trigger the exception.
		/// </summary>
		private void OnWeakTrigger(object sender, EventArgs e)
		{
			try
			{
				_page.Log.AppendText(" W ");
			}
			catch (Exception ex)
			{
				_page.Log.AppendText(Environment.NewLine);
				_page.Log.AppendText(ex.Message);
			}
		}

		#endregion
	}

	#endregion
}