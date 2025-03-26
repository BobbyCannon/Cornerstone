#region References

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Collections;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabWeakEvents : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Weak Events";

	#endregion

	#region Fields

	private PageWeakProcessor _weakProcessor;

	private BackgroundWorker _worker;

	#endregion

	#region Constructors

	public TabWeakEvents() : this(GetInstance<IDateTimeProvider>(), GetInstance<IDispatcher>())
	{
	}

	public TabWeakEvents(IDateTimeProvider dateTimeProvider, IDispatcher dispatcher) : base(dispatcher)
	{
		LogList = new SpeedyList<string>(dispatcher);
		DateTimeProvider = dateTimeProvider;
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public bool ContextRunning { get; set; }

	public IDateTimeProvider DateTimeProvider { get; }

	public SpeedyList<string> LogList { get; }

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
			LogList.Clear();

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

	private void WeakProcessorOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(PageWeakProcessor.Count):
			{
				LogList.Add($"{_weakProcessor?.Count ?? -1} {e.PropertyName}");
				break;
			}
			default:
			{
				LogList.Add(e.PropertyName);
				break;
			}
		}
	}

	private void WeakPropertyOnClick(object sender, RoutedEventArgs e)
	{
		_weakProcessor?.ResetCount();
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
		_weakProcessor = new PageWeakProcessor(page);
		var normalProcessor = new PageRegularProcessor(page);
		var count = 0;

		try
		{
			WeakEventManager.AddPropertyChanged(_weakProcessor, this, WeakProcessorOnPropertyChanged);

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
		finally
		{
			_weakProcessor = null;
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

	public class PageWeakProcessor : Bindable
	{
		#region Fields

		private readonly TabWeakEvents _page;

		#endregion

		#region Constructors

		public PageWeakProcessor(TabWeakEvents page)
		{
			_page = page;
			//_page.WeakTrigger += OnWeakTrigger;

			WeakEventManager.Add<TabWeakEvents, PageWeakProcessor, EventArgs>(_page, nameof(_page.WeakTrigger), this, OnWeakTrigger);
		}

		#endregion

		#region Properties

		public int Count { get; private set; }

		#endregion

		#region Methods

		public void ResetCount()
		{
			Count = 0;
		}

		/// <summary>
		/// Comment out the [WeakAttribute] to trigger the exception.
		/// </summary>
		private void OnWeakTrigger(object sender, EventArgs e)
		{
			try
			{
				_page.Log.AppendText(" W ");

				Count++;
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