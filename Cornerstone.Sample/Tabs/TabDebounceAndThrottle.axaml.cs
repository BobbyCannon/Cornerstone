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

	public ThrottleProxy Throttle { get; }

	public IDateTimeProvider TimeProvider { get; }

	public SeriesDataProvider Triggers { get; }

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

	protected override void OnUnloaded(RoutedEventArgs e)
	{
		_monitorTimer.IsEnabled = false;
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
		Debounce.Cancel();
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

	private void DebounceOnClick(object sender, RoutedEventArgs e)
	{
		Debounce.Trigger(1);
		_triggered = 60d;
	}

	private void DebounceResetOnClick(object sender, RoutedEventArgs e)
	{
		Debounce.Reset();
	}

	private void ThrottleCancelOnClick(object sender, RoutedEventArgs e)
	{
		Throttle.Cancel();
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

	private void ThrottleOnClick(object sender, RoutedEventArgs e)
	{
		Throttle.Trigger(2);
		_triggered = 60d;
	}

	private void ThrottleResetOnClick(object sender, RoutedEventArgs e)
	{
		Throttle.Reset();
	}

	private void TimerTick(object sender, EventArgs e)
	{
		Triggers.Add(_triggered);
		Processing.Add(Debounce.IsProcessing | Throttle.IsProcessing ? 60 : 0);
		_triggered = 0;
	}

	#endregion
}