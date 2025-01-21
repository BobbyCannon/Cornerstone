#region References

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Skia;
using Avalonia.Threading;
using Cornerstone.Attributes;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Resources;
using Cornerstone.Collections;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.ViewModels;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using IDispatcher = Cornerstone.Presentation.IDispatcher;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabProfiling : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Profiling";

	#endregion

	#region Fields

	private readonly ApplicationSettings _applicationSettings;

	private readonly DateTimeAxis _customXAxis;
	private readonly Axis _customYAxis;
	private readonly IDateTimeProvider _dateTimeProvider;
	private readonly DispatcherTimer _secondsTimer;

	#endregion

	#region Constructors

	public TabProfiling() : this(
		DesignModeDependencyProvider.Get<ApplicationSettings>(),
		DesignModeDependencyProvider.Get<IDateTimeProvider>(),
		null)
	{
	}

	[DependencyInjectionConstructor]
	public TabProfiling(ApplicationSettings applicationSettings,
		IDateTimeProvider dateTimeProvider, IDispatcher dispatcher) : base(dispatcher)
	{
		_applicationSettings = applicationSettings;
		_dateTimeProvider = dateTimeProvider;
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
			CustomSeparators = GetSeparators(_dateTimeProvider.Now),
			AnimationsSpeed = TimeSpan.FromMilliseconds(0),
			SeparatorsPaint = new SolidColorPaint(SKColors.White.WithAlpha(100)),
			ShowSeparatorLines = true,
			IsVisible = true
		};

		Timer = new AverageTimer(GetDispatcher());
		ValuesPerMinute = new SpeedyList<DateTimePoint>(GetDispatcher()) { Limit = 25 };

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

		_applicationSettings.PropertyChanged += ApplicationSettingsOnPropertyChanged;

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
		_applicationSettings.PropertyChanged -= ApplicationSettingsOnPropertyChanged;
		_secondsTimer.IsEnabled = false;
		base.OnUnloaded(e);
	}

	private void ApplicationSettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(_applicationSettings.ThemeColor):
			{
				((LineSeries<DateTimePoint>) Series[0]).Stroke = new SolidColorPaint(ResourceService.GetColor("ThemeColor06").ToSKColor(), 4);
				break;
			}
		}
	}

	private string Formatter(DateTime date)
	{
		var secsAgo = (_dateTimeProvider.Now - date).TotalSeconds;
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
		var currentTime = _dateTimeProvider.Now;

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