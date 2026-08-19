#region References

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Cornerstone.Avalonia;
using Cornerstone.Data;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using DispatcherPriority = Avalonia.Threading.DispatcherPriority;
using IDispatcher = Cornerstone.Presentation.IDispatcher;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabDebounceAndThrottle : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Debounce / Throttle";

	#endregion

	#region Fields

	private readonly IDispatcher _dispatcher;
	private readonly DispatcherTimer _monitorTimer;
	private Debounce _simpleDebounce;
	private int _simpleProcessing;
	private Throttle _simpleThrottle;

	private double _triggered;

	#endregion

	#region Constructors

	public TabDebounceAndThrottle() : this(
		GetInstance<IDateTimeProvider>(),
		GetInstance<IDispatcher>())
	{
	}

	[DependencyInjectionConstructor]
	public TabDebounceAndThrottle(IDateTimeProvider timeProvider, IDispatcher dispatcher)
	{
		_dispatcher = dispatcher;
		_triggered = 0d;

		TimeProvider = timeProvider;
		DebounceThrottleManager = DebounceThrottleManager.Start(timeProvider);
		Debounce = DebounceThrottleManager.CreateDebounce(TimeSpan.FromSeconds(1), Debounced);
		Throttle = DebounceThrottleManager.CreateThrottle(TimeSpan.FromSeconds(1), Throttled);
		WorkDelay = 1;
		UseManager = true;
		SimpleDebounceInterval = TimeSpan.FromSeconds(1);
		SimpleThrottleInterval = TimeSpan.FromSeconds(1);
		RecreateSimpleDebounce();
		RecreateSimpleThrottle();

		// 50ms updates == 20 call per seconds, for 25 seconds
		Processing = new SeriesDataProvider(500);
		Triggers = new SeriesDataProvider(500);

		DataContext = this;
		InitializeComponent();

		_monitorTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Normal, TimerTick) { IsEnabled = false };
	}

	#endregion

	#region Properties

	public DebounceProxy Debounce { get; }

	public DebounceThrottleManager DebounceThrottleManager { get; }

	public SeriesDataProvider Processing { get; }

	[Notify]
	public partial TimeSpan SimpleDebounceInterval { get; set; }

	[Notify]
	public partial TimeSpan SimpleThrottleInterval { get; set; }

	public ThrottleProxy Throttle { get; }

	public IDateTimeProvider TimeProvider { get; }

	public SeriesDataProvider Triggers { get; }

	[Notify]
	public partial bool UseManager { get; set; }

	[DependsOn(nameof(UseManager))]
	public bool UseSimple
	{
		get => !UseManager;
		set => UseManager = !value;
	}

	[Notify]
	public partial bool WorkCanCancel { get; set; }

	public int WorkDelay { get; set; }

	#endregion

	#region Methods

	protected override void OnLoaded(RoutedEventArgs e)
	{
		if (!Design.IsDesignMode)
		{
			Task.Run(() =>
			{
				// Delay the start of the background timer.
				Thread.Sleep(1000);
				_monitorTimer.IsEnabled = true;
			});
		}
		base.OnLoaded(e);
	}

	protected override void OnPropertyChanged(string propertyName)
	{
		base.OnPropertyChanged(propertyName);

		if (propertyName == nameof(SimpleDebounceInterval))
		{
			RecreateSimpleDebounce();
		}
		else if (propertyName == nameof(SimpleThrottleInterval))
		{
			RecreateSimpleThrottle();
		}
		else if (propertyName == nameof(UseManager))
		{
			OnPropertyChanged(nameof(UseSimple));

			if (UseManager)
			{
				_simpleDebounce?.Dispose();
				_simpleThrottle?.Dispose();
				_simpleDebounce = null;
				_simpleThrottle = null;
				Volatile.Write(ref _simpleProcessing, 0);
			}
			else
			{
				WorkCanCancel = false;
				Debounce.Cancel();
				Throttle.Cancel();
				RecreateSimpleDebounce();
				RecreateSimpleThrottle();
			}
		}
	}

	protected override void OnUnloaded(RoutedEventArgs e)
	{
		_monitorTimer.IsEnabled = false;
		_simpleDebounce?.Dispose();
		_simpleThrottle?.Dispose();
		base.OnUnloaded(e);
	}

	private void AppendText(string message)
	{
		_dispatcher.Dispatch(() => { Log.ViewModel.Append(message); });
	}

	private void ClearLog(object sender, RoutedEventArgs e)
	{
		Log.ViewModel.Load(string.Empty);
	}

	private void DebounceCancelOnClick(object sender, RoutedEventArgs e)
	{
		if (UseManager)
		{
			Debounce.Cancel();
		}
	}

	private void DebounceOnClick(object sender, RoutedEventArgs e)
	{
		if (UseManager)
		{
			Debounce.Trigger(1);
		}
		else
		{
			EnsureSimpleInstances();
			_simpleDebounce.Trigger();
		}

		_triggered = 60d;
	}

	private void DebounceResetOnClick(object sender, RoutedEventArgs e)
	{
		if (UseManager)
		{
			Debounce.Reset();
		}
	}

	private void Debounced(CancellationToken token, object value, bool forced)
	{
		AppendText("+ Debounce\r\n");

		var watch = Stopwatch.StartNew();

		while (watch.Elapsed.TotalSeconds < WorkDelay)
		{
			if (token.IsCancellationRequested && WorkCanCancel)
			{
				AppendText("* Debounce\r\n");
				return;
			}

			Thread.Sleep(250);
		}

		AppendText("- Debounce\r\n");
	}

	private void EnsureSimpleInstances()
	{
		if (_simpleDebounce is null)
		{
			RecreateSimpleDebounce();
		}

		if (_simpleThrottle is null)
		{
			RecreateSimpleThrottle();
		}
	}

	private void RecreateSimpleDebounce()
	{
		_simpleDebounce?.Dispose();
		_simpleDebounce = new Debounce(SimpleDebounced, SimpleDebounceInterval);
	}

	private void RecreateSimpleThrottle()
	{
		_simpleThrottle?.Dispose();
		_simpleThrottle = new Throttle(SimpleThrottled, SimpleThrottleInterval);
	}

	private void RunSimpleWork(string name)
	{
		Interlocked.Increment(ref _simpleProcessing);
		try
		{
			AppendText($"+ {name}\r\n");

			var watch = Stopwatch.StartNew();
			while (watch.Elapsed.TotalSeconds < WorkDelay)
			{
				Thread.Sleep(250);
			}

			AppendText($"- {name}\r\n");
		}
		finally
		{
			Interlocked.Decrement(ref _simpleProcessing);
		}
	}

	private void SimpleDebounced()
	{
		RunSimpleWork("Debounce");
	}

	private void SimpleThrottled()
	{
		// Leading-edge Trigger() runs on the caller (UI) thread.
		// Do not block it with WorkDelay sleep.
		Task.Run(() => RunSimpleWork("Throttle"));
	}

	private void ThrottleCancelOnClick(object sender, RoutedEventArgs e)
	{
		if (UseManager)
		{
			Throttle.Cancel();
		}
	}

	private void ThrottleOnClick(object sender, RoutedEventArgs e)
	{
		if (UseManager)
		{
			Throttle.Trigger(2);
		}
		else
		{
			EnsureSimpleInstances();
			_simpleThrottle.Trigger();
		}

		_triggered = 60d;
	}

	private void ThrottleResetOnClick(object sender, RoutedEventArgs e)
	{
		if (UseManager)
		{
			Throttle.Reset();
		}
	}

	private void Throttled(CancellationToken token, object value, bool forced)
	{
		AppendText("+ Throttle\r\n");

		var watch = Stopwatch.StartNew();

		while (watch.Elapsed.TotalSeconds < WorkDelay)
		{
			if (token.IsCancellationRequested && WorkCanCancel)
			{
				AppendText("* Throttle\r\n");
				return;
			}

			Thread.Sleep(250);
		}

		AppendText("- Throttle\r\n");
	}

	private void TimerTick(object sender, EventArgs e)
	{
		Triggers.Add(_triggered);
		var processing = UseManager
			? Debounce.IsProcessing | Throttle.IsProcessing ? 60 : 0
			: Volatile.Read(ref _simpleProcessing) > 0
				? 60
				: 0;
		Processing.Add(processing);
		_triggered = 0;
	}

	#endregion
}