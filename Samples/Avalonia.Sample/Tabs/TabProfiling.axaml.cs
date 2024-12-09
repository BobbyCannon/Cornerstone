#region References

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Sample.ViewModels;
using Avalonia.Skia;
using Avalonia.Threading;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Avalonia.Resources;
using Cornerstone.Collections;
using Cornerstone.Profiling;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using DispatcherPriority = Avalonia.Threading.DispatcherPriority;

#endregion

namespace Avalonia.Sample.Tabs;

public partial class TabProfiling : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Profiling";

	#endregion

	#region Fields

	private readonly DateTimeAxis _customXAxis;
	private readonly Axis _customYAxis;
	private readonly MainViewModel _mainViewModel;
	private readonly DispatcherTimer _secondsTimer;

	#endregion

	#region Constructors

	public TabProfiling() : this(DesignModeDependencyProvider.Get<MainViewModel>())
	{
	}

	public TabProfiling(MainViewModel mainViewModel)
	{
		_mainViewModel = mainViewModel;
		_customYAxis = new Axis
		{
			AnimationsSpeed = TimeSpan.FromMilliseconds(0),
			MinLimit = 0,
			MaxLimit = 12,
			MinStep = 1,
			Name = "Value",
			IsVisible = true
		};

		_customXAxis = new DateTimeAxis(TimeSpan.FromSeconds(1), Formatter)
		{
			CustomSeparators = GetSeparators(mainViewModel.TimeProvider.Now),
			AnimationsSpeed = TimeSpan.FromMilliseconds(0),
			SeparatorsPaint = new SolidColorPaint(SKColors.White.WithAlpha(100)),
			ShowSeparatorLines = true,
			IsVisible = true
		};

		Timer = new AverageTimer(_mainViewModel.GetDispatcher());
		ValuesPerMinute = new SpeedyList<DateTimePoint>(_mainViewModel.GetDispatcher()) { Limit = 25 };

		Series =
		[
			new LineSeries<DateTimePoint>
			{
				Fill = null,
				GeometryFill = null,
				GeometryStroke = null,
				Stroke = new SolidColorPaint(ResourceService.GetColor("ThemeColor06").ToSKColor(), 4),
				Name = "Per Minute",
				Values = ValuesPerMinute
			}
		];

		YAxes = [_customYAxis];
		XAxes = [_customXAxis];
		DataContext = this;

		InitializeComponent();

		_secondsTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(1000), DispatcherPriority.Background, TimerTick) { IsEnabled = false };
	}

	#endregion

	#region Properties

	public SpeedyList<ISeries<DateTimePoint>> Series { get; set; }

	public AverageTimer Timer { get; set; }

	public SpeedyList<DateTimePoint> ValuesPerMinute { get; }

	public Axis[] XAxes { get; }

	public Axis[] YAxes { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		((LineSeries<DateTimePoint>) Series[0]).Stroke = new SolidColorPaint(ResourceService.GetColor("ThemeColor06").ToSKColor(), 4);

		_mainViewModel.ApplicationSettings.PropertyChanged += ApplicationSettingsOnPropertyChanged;

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
		_mainViewModel.ApplicationSettings.PropertyChanged -= ApplicationSettingsOnPropertyChanged;
		_secondsTimer.IsEnabled = false;
		base.OnUnloaded(e);
	}

	private void ApplicationSettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(_mainViewModel.ApplicationSettings.ThemeColor):
			{
				((LineSeries<DateTimePoint>) Series[0]).Stroke = new SolidColorPaint(ResourceService.GetColor("ThemeColor06").ToSKColor(), 4);
				break;
			}
		}
	}

	private string Formatter(DateTime date)
	{
		var secsAgo = (_mainViewModel.TimeProvider.Now - date).TotalSeconds;
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

	private void TimerTick(object sender, EventArgs e)
	{
		var currentTime = _mainViewModel.TimeProvider.Now;

		try
		{
			var value = new DateTimePoint(currentTime, Timer.Count);
			Timer.Reset();
			ValuesPerMinute.Add(value);
		}
		finally
		{
			// we need to update the separators every time we add a new point 
			_customXAxis.CustomSeparators = GetSeparators(currentTime);
		}
	}

	private void Trigger(object sender, RoutedEventArgs e)
	{
		Timer.Time(() => { });
	}

	#endregion
}