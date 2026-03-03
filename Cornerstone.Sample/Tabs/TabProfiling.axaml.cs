#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Cornerstone.Avalonia;
using Cornerstone.Generators;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabProfiling : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Profiling";

	#endregion

	#region Fields

	private readonly DispatcherTimer _timer;
	private readonly DispatcherTimer _timer2;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public TabProfiling()
	{
		_timer = new DispatcherTimer(TimeSpan.FromMilliseconds(25), DispatcherPriority.Normal, RandomUpdater) { IsEnabled = false };
		_timer2 = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Background, ProviderUpdate) { IsEnabled = false };

		Profiler = new Profiler();
		RandomData = new SeriesDataProvider(60);
		RandomDelay = _timer.Interval;
		RenderData = Profiler.SetupScopeHistory("Render");
		RuntimeInformation = GetInstance<IRuntimeInformation>();
		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Properties

	public SeriesDataProvider RandomData { get; }

	[StyledProperty]
	public partial TimeSpan RandomDelay { get; set; }

	public SeriesDataProvider RenderData { get; }

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

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if ((change.Property == RandomDelayProperty)
			&& change.NewValue is TimeSpan newValue)
		{
			_timer.Interval = newValue;
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