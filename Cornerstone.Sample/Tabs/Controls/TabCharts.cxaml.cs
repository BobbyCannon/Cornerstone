#region References

using System;
using Cornerstone.Generators;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.DesignTime;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Threading;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Sample.Tabs.Controls;

[SourceReflection]
public partial class TabCharts : SampleUserControl
{
	#region Constants

	public const string HeaderName = "Charts";

	#endregion

	#region Fields

	private readonly DispatcherTimer _timer, _timer2;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public TabCharts()
	{
		_timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal, ProviderUpdate) { IsEnabled = false };
		_timer2 = new DispatcherTimer(TimeSpan.FromMilliseconds(25), DispatcherPriority.Normal, RandomUpdater) { IsEnabled = false };

		Profiler = new Profiler();
		RandomData = new SeriesDataProvider(60);
		RandomDelay = _timer2.Interval;
		(RenderData, PerSecondData) = Profiler.SetupScopeHistory("Render");
		RuntimeInformation = AppBootstrap.GetInstance<IRuntimeInformation>();
		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Properties

	public ISeriesDataProvider PerSecondData { get; }

	public ISeriesDataProvider RandomData { get; }

	[StyledProperty]
	public partial TimeSpan RandomDelay { get; set; }

	public ISeriesDataProvider RenderData { get; }

	public IRuntimeInformation RuntimeInformation { get; }

	#endregion

	#region Methods

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		if (RuntimeInformation.DevicePlatform == DevicePlatform.Browser)
		{
			// Browser is not that performant so increase minimum to prevent lock up.
			RandomDelaySlider.Minimum = 10;
		}

		RenderChart.ValueFormatter = x => TimeSpan.FromTicks((long) x).Humanize();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (!Design.IsDesignMode)
		{
			_timer.IsEnabled = true;
			_timer2.IsEnabled = true;
		}
		base.OnAttachedToVisualTree(e);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_timer.IsEnabled = false;
		_timer2.IsEnabled = false;
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		if ((change.Property == RandomDelayProperty)
			&& change.NewValue is TimeSpan newValue)
		{
			_timer2.Interval = newValue;
		}

		base.OnPropertyChanged(change);
	}

	private void ProviderUpdate(object sender, EventArgs e)
	{
		Profiler.Refresh();
	}

	private void RandomUpdater(object sender, EventArgs e)
	{
		RandomData.Add(RandomGenerator.NextDouble(0, 100));
	}

	#endregion
}