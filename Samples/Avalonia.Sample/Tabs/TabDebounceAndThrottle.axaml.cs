#region References

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Sample.ViewModels;
using Avalonia.Threading;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using DispatcherPriority = Avalonia.Threading.DispatcherPriority;
using IDispatcher = Cornerstone.Presentation.IDispatcher;

#endregion

namespace Avalonia.Sample.Tabs;

public partial class TabDebounceAndThrottle : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Debounce / Throttle";

	#endregion

	#region Fields

	private readonly DateTimeAxis _customXAxis;
	private readonly Axis _customYAxis;
	private readonly DispatcherTimer _secondsTimer;

	#endregion

	#region Constructors

	public TabDebounceAndThrottle() : this(
		DesignModeDependencyProvider.Get<IDateTimeProvider>(),
		DesignModeDependencyProvider.Get<IDispatcher>())
	{
	}

	public TabDebounceAndThrottle(IDateTimeProvider timeProvider, IDispatcher dispatcher)
	{
		TimeProvider = timeProvider;

		DebounceRequests = new SpeedyList<DateTimePoint>(dispatcher);
		Debounces = new SpeedyList<DateTimePoint>(dispatcher) { Limit = 250 };
		Debounce = new DebounceService<int>(TimeSpan.FromSeconds(2), DebouncedMethod, null, dispatcher);

		ThrottleRequests = new SpeedyList<DateTimePoint>(dispatcher);
		Throttles = new SpeedyList<DateTimePoint>(dispatcher) { Limit = 250 };
		Throttle = new ThrottleService<int>(TimeSpan.FromSeconds(2), ThrottledAction, null, dispatcher);

		WorkDelay = 5;

		Series =
		[
			new StepLineSeries<DateTimePoint> { Values = DebounceRequests, Fill = null, GeometryFill = null, GeometryStroke = null, Name = "Debounce" },
			new StepLineSeries<DateTimePoint> { Values = ThrottleRequests, Fill = null, GeometryFill = null, GeometryStroke = null, Name = "Throttle" }
		];

		Series2 =
		[
			new StepLineSeries<DateTimePoint> { Values = Debounces, Fill = null, GeometryFill = null, GeometryStroke = null, Name = "Debounce" },
			new StepLineSeries<DateTimePoint> { Values = Throttles, Fill = null, GeometryFill = null, GeometryStroke = null, Name = "Throttle" }
		];

		_customYAxis = new Axis
		{
			AnimationsSpeed = TimeSpan.FromMilliseconds(0),
			MinLimit = 0,
			MaxLimit = 2,
			MinStep = 1,
			Name = "Value",
			IsVisible = true
		};

		_customXAxis = new DateTimeAxis(TimeSpan.FromSeconds(1), Formatter)
		{
			CustomSeparators = GetSeparators(DateTimeProvider.RealTime.Now),
			AnimationsSpeed = TimeSpan.FromMilliseconds(0),
			SeparatorsPaint = new SolidColorPaint(SKColors.White.WithAlpha(100)),
			ShowSeparatorLines = true,
			IsVisible = true
		};

		YAxes = [_customYAxis];
		XAxes = [_customXAxis];

		DataContext = this;

		InitializeComponent();

		_secondsTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Background, TimerTick) { IsEnabled = false };
	}

	#endregion

	#region Properties

	public SpeedyList<DateTimePoint> DebounceRequests { get; set; }

	public IDateTimeProvider TimeProvider { get; }

	public SpeedyList<DateTimePoint> Debounces { get; set; }

	public SpeedyList<ISeries<DateTimePoint>> Series { get; set; }

	public SpeedyList<ISeries<DateTimePoint>> Series2 { get; set; }

	public SpeedyList<DateTimePoint> ThrottleRequests { get; set; }

	public SpeedyList<DateTimePoint> Throttles { get; set; }

	public bool WorkCanCancel { get; set; }

	public int WorkDelay { get; set; }

	public Axis[] XAxes { get; }

	public Axis[] YAxes { get; }

	private DebounceService<int> Debounce { get; }

	private ThrottleService<int> Throttle { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		Task.Run(() =>
		{
			// Delay the start of the background timer.
			Thread.Sleep(1000);
			_secondsTimer.IsEnabled = true;
		});

		base.OnLoaded(e);
	}

	/// <inheritdoc />
	protected override void OnUnloaded(RoutedEventArgs e)
	{
		_secondsTimer.IsEnabled = false;
		base.OnUnloaded(e);
	}

	private void AppendText(string message)
	{
		this.Dispatch(() => { Log.AppendText(message); });
	}

	private void ClearLog(object sender, RoutedEventArgs e)
	{
		Log.Clear();
	}

	private void DebounceCancelOnClick(object sender, RoutedEventArgs e)
	{
		Debounce.Cancel();
	}

	private void DebouncedMethod(CancellationToken arg1, int arg2)
	{
		AppendText("+ Debounce\r\n");

		var watch = Stopwatch.StartNew();

		while (watch.Elapsed.TotalSeconds < WorkDelay)
		{
			if (arg1.IsCancellationRequested && WorkCanCancel)
			{
				AppendText("* Debounce\r\n");
				return;
			}

			Thread.Sleep(250);
		}

		Debounces.Add(new DateTimePoint(DateTimeProvider.RealTime.Now, arg2));

		AppendText("- Debounce\r\n");
	}

	private void DebounceOnClick(object sender, RoutedEventArgs e)
	{
		DebounceRequests.Add(new DateTimePoint(DateTimeProvider.RealTime.Now, 1));
		Debounce.Trigger(1);
	}

	private void DebounceResetOnClick(object sender, RoutedEventArgs e)
	{
		Debounce.Reset();
	}

	private static string Formatter(DateTime date)
	{
		var secsAgo = (DateTimeProvider.RealTime.Now - date).TotalSeconds;
		return secsAgo < 1 ? "now" : $"{secsAgo:N0}s ago";
	}

	private double[] GetSeparators(DateTime from)
	{
		return
		[
			from.AddSeconds(-25).Ticks,
			from.AddSeconds(-20).Ticks,
			from.AddSeconds(-15).Ticks,
			from.AddSeconds(-10).Ticks,
			from.AddSeconds(-5).Ticks,
			from.Ticks
		];
	}

	private void ThrottleCancelOnClick(object sender, RoutedEventArgs e)
	{
		Throttle.Cancel();
	}

	private void ThrottledAction(CancellationToken arg1, int arg2)
	{
		AppendText("+ Throttle\r\n");

		var watch = Stopwatch.StartNew();

		while (watch.Elapsed.TotalSeconds < WorkDelay)
		{
			if (arg1.IsCancellationRequested && WorkCanCancel)
			{
				AppendText("* Throttle\r\n");
				return;
			}

			Thread.Sleep(250);
		}

		Throttles.Add(new DateTimePoint(DateTimeProvider.RealTime.Now, arg2));

		AppendText("- Throttle\r\n");
	}

	private void ThrottleOnClick(object sender, RoutedEventArgs e)
	{
		ThrottleRequests.Add(new DateTimePoint(DateTimeProvider.RealTime.Now, 2));
		Throttle.Trigger(2);
	}

	private void ThrottleResetOnClick(object sender, RoutedEventArgs e)
	{
		Throttle.Reset();
	}

	private void TimerTick(object sender, EventArgs e)
	{
		var currentTime = DateTimeProvider.RealTime.Now;

		try
		{
			DebounceRequests.Add(new DateTimePoint(currentTime, 0));
			Debounces.Add(new DateTimePoint(currentTime, Debounce.IsProcessing ? 1 : 0));

			ThrottleRequests.Add(new DateTimePoint(currentTime, 0));
			Throttles.Add(new DateTimePoint(currentTime, Throttle.IsProcessing ? 2 : 0));

			var min = currentTime.AddSeconds(-25);
			DebounceRequests.RemoveWhere(x => x.DateTime < min);
			Debounces.RemoveWhere(x => x.DateTime < min);
			ThrottleRequests.RemoveWhere(x => x.DateTime < min);
			Throttles.RemoveWhere(x => x.DateTime < min);
		}
		finally
		{
			// we need to update the separators every time we add a new point 
			_customXAxis.CustomSeparators = GetSeparators(currentTime);
		}
	}

	#endregion
}