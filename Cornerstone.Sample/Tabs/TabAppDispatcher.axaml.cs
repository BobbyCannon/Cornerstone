#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Cornerstone.Avalonia;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Text;
using DispatcherPriority = Avalonia.Threading.DispatcherPriority;

#endregion

namespace Cornerstone.Sample.Tabs;

/// <summary>
/// AppDispatcher sample shell: shared charts/profiler and a tab host that shows one demo View at a time.
/// Each demo is a View + ViewModel pair; attach/detach follows the visual tree (no host-level Attach).
/// Page layout uses <see cref="Cornerstone.Avalonia.Controls.AdaptiveFillLayout" /> (fill desktop / compact mobile).
/// </summary>
[SourceReflection]
public partial class TabAppDispatcher : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "AppDispatcher";

	#endregion

	#region Fields

	private readonly IAppDispatcher _appDispatcher;
	private readonly DispatcherTimer _timer;

	#endregion

	#region Constructors

	public TabAppDispatcher() : this(GetInstance<IAppDispatcher>())
	{
	}

	[DependencyInjectionConstructor]
	public TabAppDispatcher(IAppDispatcher appDispatcher)
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

		appDispatcher.Track(AutomaticViewModel.Projection);
		appDispatcher.Track(StreamingViewModel);
		appDispatcher.Track(CollectionsViewModel);
		appDispatcher.Track(SeriesViewModel);
		appDispatcher.Track(PropertiesViewModel);

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

	[Notify]
	public partial int SelectedDemoIndex { get; set; }

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
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_timer.IsEnabled = false;
		if (ReferenceEquals(_appDispatcher.SystemProfiler, Profiler))
		{
			_appDispatcher.SystemProfiler = null;
		}

		base.OnDetachedFromVisualTree(e);
	}

	#endregion
}
