#region References

using System;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls.DesignTime;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Threading;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Text;
using DispatcherPriority = Cornerstone.Presentation.DispatcherPriority;

#endregion

namespace Cornerstone.Sample.Tabs.AppDispatcher;

/// <summary>
/// AppDispatcher live demos. Charts stay on this page. Each demo is a tab.
/// Attach and detach follow the visual tree (no host-level Attach).
/// Switching tabs detaches that demo. This host stays mounted so the charts keep recording.
/// </summary>
[SourceReflection]
public partial class TabAppDispatcherDemos : SampleUserControl
{
	#region Constants

	public const string HeaderName = "AppDispatcher";

	#endregion

	#region Fields

	private readonly IAppDispatcher _appDispatcher;
	private readonly DispatcherTimer _timer;

	#endregion

	#region Constructors

	public TabAppDispatcherDemos() : this(AppBootstrap.GetInstance<IAppDispatcher>())
	{
	}

	[DependencyInjectionConstructor]
	public TabAppDispatcherDemos(IAppDispatcher appDispatcher)
	{
		_appDispatcher = appDispatcher;
		_timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal, (_, _) => Profiler?.Refresh())
		{
			IsEnabled = false
		};

		// One profiler: Model scopes at mutation sites; View = AppDispatcher.Apply (system).
		Profiler = new Profiler("App Dispatcher Sample");
		(_, GraphDataForModel) = Profiler.SetupScopeHistory("Model");
		(_, GraphDataForView) = Profiler.SetupScopeHistory(ApplicationViewModel.ApplyScopeName);

		AutomaticViewModel = new TabAppDispatcherAutomaticViewModel(Profiler);
		StreamingViewModel = new TabAppDispatcherStreamingViewModel(new TextIngress());
		CollectionsViewModel = new TabAppDispatcherCollectionsViewModel();
		SeriesViewModel = new TabAppDispatcherSeriesViewModel();

		var propertyMapModel = new TabAppDispatcherPropertyMapModel
		{
			Title = "hello",
			Count = 1,
			Ratio = 0.25
		};
		propertyMapModel.ResetHasChanges();
		PropertiesViewModel = new TabAppDispatcherPropertiesViewModel(propertyMapModel);

		AutomaticView = new TabAppDispatcherAutomaticView
		{
			DataContext = AutomaticViewModel,
			Profiler = Profiler
		};
		StreamingView = new TabAppDispatcherStreamingView
		{
			ViewModel = StreamingViewModel,
			DataContext = StreamingViewModel,
			Profiler = Profiler
		};
		CollectionsView = new TabAppDispatcherCollectionsView
		{
			ViewModel = CollectionsViewModel,
			DataContext = CollectionsViewModel,
			Profiler = Profiler
		};
		SeriesView = new TabAppDispatcherSeriesView
		{
			ViewModel = SeriesViewModel,
			DataContext = SeriesViewModel,
			Profiler = Profiler
		};
		PropertiesView = new TabAppDispatcherPropertiesView
		{
			ViewModel = PropertiesViewModel,
			DataContext = PropertiesViewModel,
			Profiler = Profiler
		};

		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public TabAppDispatcherAutomaticView AutomaticView { get; }

	public TabAppDispatcherAutomaticViewModel AutomaticViewModel { get; }

	public TabAppDispatcherCollectionsView CollectionsView { get; }

	public TabAppDispatcherCollectionsViewModel CollectionsViewModel { get; }

	public ISeriesDataProvider GraphDataForModel { get; }

	public ISeriesDataProvider GraphDataForView { get; }

	public TabAppDispatcherPropertiesView PropertiesView { get; }

	public TabAppDispatcherPropertiesViewModel PropertiesViewModel { get; }

	public TabAppDispatcherSeriesView SeriesView { get; }

	public TabAppDispatcherSeriesViewModel SeriesViewModel { get; }

	public TabAppDispatcherStreamingView StreamingView { get; }

	public TabAppDispatcherStreamingViewModel StreamingViewModel { get; }

	#endregion

	#region Methods

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (!Design.IsDesignMode)
		{
			_timer.IsEnabled = true;

			// Opt in: View chart is AppDispatcher apply rate (null = no system cost).
			_appDispatcher.SystemProfiler = Profiler;
		}

		base.OnAttachedToVisualTree(e);

		ModelChart.ValueFormatter = x => $"{x:N0} per second";
		ViewChart.ValueFormatter = x => $"{x:N0} per second";

		PageScroll.SizeChanged += OnPageScrollSizeChanged;
		FitLayout();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		PageScroll.SizeChanged -= OnPageScrollSizeChanged;
		_timer.IsEnabled = false;
		if (ReferenceEquals(_appDispatcher.SystemProfiler, Profiler))
		{
			_appDispatcher.SystemProfiler = null;
		}

		base.OnDetachedFromVisualTree(e);
	}

	private void OnPageScrollSizeChanged(object sender, SizeChangedEventArgs e)
	{
		FitLayout();
	}

	private void FitLayout()
	{
		var width = Layout.Bounds.Width;
		if (width > 0)
		{
			var chartWidth = Math.Min(360, width);
			ModelChart.Width = chartWidth;
			ViewChart.Width = chartWidth;
		}

		var viewport = PageScroll.Bounds.Height;
		if (viewport <= 0)
		{
			return;
		}

		// Fill the window when it is tall. Keep a usable demo pane when the window is short,
		// and let the page scroll to reach it. Same rule on every platform.
		var header = Intro.Bounds.Height + Charts.Bounds.Height;
		var chrome = Layout.Margin.Top + Layout.Margin.Bottom;
		var remaining = viewport - chrome - header;
		var tabHeight = Math.Max(360, remaining);
		Layout.RowDefinitions[2].Height = new GridLength(tabHeight);
	}

	#endregion
}