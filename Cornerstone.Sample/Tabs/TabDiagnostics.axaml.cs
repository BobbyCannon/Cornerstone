#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Charts;
using Cornerstone.Diagnostics;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Keystone;

#endregion

namespace Cornerstone.Sample.Tabs;

/// <summary>
/// Thin view over <see cref="DiagnosticsTabViewModel" />. No poll timers — AppDispatcher applies models.
/// </summary>
[SourceReflection]
public partial class TabDiagnostics : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Diagnostics";

	#endregion

	#region Fields

	private readonly ApplicationViewModel _appDispatcher;
	private readonly AppBus _bus;
	private IDiagnosticsCapture _previousCapture;
	private DispatchableViewModel _previousDiagnosticsDispatchable;
	private Profiler _previousSystemProfiler;

	#endregion

	#region Constructors

	public TabDiagnostics()
		: this(GetInstance<AppBus>(), GetInstance<IAppDispatcher>())
	{
	}

	[DependencyInjectionConstructor]
	public TabDiagnostics(AppBus bus, IAppDispatcher appDispatcher)
	{
		_bus = bus;
		_appDispatcher = appDispatcher as ApplicationViewModel
			?? throw new InvalidOperationException("Diagnostics requires ApplicationViewModel as IAppDispatcher.");

		Profiler = new Profiler("Diagnostics");
		Session = new DiagnosticsSession(bus, Profiler);
		ViewModel = new DiagnosticsTabViewModel(Session);

		// DiagnosticsDispatchable is applied after the feature loop (not Track()'d).
		// LoadSimulation is a real tracked feature root for apply-rate / monitoring tests.

		DataContext = ViewModel;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public DiagnosticsSession Session { get; }

	public DiagnosticsTabViewModel ViewModel { get; }

	#endregion

	#region Methods

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (!Design.IsDesignMode)
		{
			_previousSystemProfiler = _appDispatcher.SystemProfiler;
			_previousCapture = _appDispatcher.DiagnosticsCapture;
			_previousDiagnosticsDispatchable = _appDispatcher.DiagnosticsDispatchable;

			_appDispatcher.SystemProfiler = Profiler;
			_appDispatcher.DiagnosticsCapture = Session;
			_appDispatcher.DiagnosticsDispatchable = ViewModel;

			// Feature load root: participates in CollectPending / Apply / apply chart.
			Session.LoadSimulation.Attach(this);
			_appDispatcher.Track(Session.LoadSimulation);

			ViewModel.Attach(this);

			// One wake so capture + apply run soon after open.
			_appDispatcher.RequestDispatch();
		}

		base.OnAttachedToVisualTree(e);

		if (this.FindControl<LineChart>("ApplyRateChart") is { } chart)
		{
			chart.ValueFormatter = x => $"{x:N0} / s";
		}
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (!Design.IsDesignMode)
		{
			ViewModel.IsSimulatingLoad = false;
			ViewModel.Detach(this);

			_appDispatcher.Release(Session.LoadSimulation);
			Session.LoadSimulation.Detach(this);

			if (ReferenceEquals(_appDispatcher.DiagnosticsCapture, Session))
			{
				_appDispatcher.DiagnosticsCapture = _previousCapture;
			}

			if (ReferenceEquals(_appDispatcher.DiagnosticsDispatchable, ViewModel))
			{
				_appDispatcher.DiagnosticsDispatchable = _previousDiagnosticsDispatchable;
			}

			if (ReferenceEquals(_appDispatcher.SystemProfiler, Profiler))
			{
				_appDispatcher.SystemProfiler = _previousSystemProfiler;
			}
		}

		base.OnDetachedFromVisualTree(e);
	}

	[RelayCommand]
	private void ClearBusHistory()
	{
		ViewModel.ClearBusHistory();
		_appDispatcher.RequestDispatch();
	}

	[RelayCommand]
	private void PublishSampleBurst()
	{
		for (var i = 0; i < 10; i++)
		{
			_bus.Notification.ShowMessage("Diagnostics", $"Burst {i + 1}", NotificationType.Information);
		}

		_appDispatcher.RequestDispatch();
	}

	[RelayCommand]
	private void PublishSampleNotification()
	{
		_bus.Notification.ShowMessage(
			"Diagnostics",
			$"Sample at {DateTime.UtcNow:HH:mm:ss.fff}",
			NotificationType.Information);
		_appDispatcher.RequestDispatch();
	}

	[RelayCommand]
	private void PulseLoad()
	{
		ViewModel.PulseLoad();
		_appDispatcher.RequestDispatch();
	}

	[RelayCommand]
	private void RequestDispatch()
	{
		_appDispatcher.RequestDispatch();
	}

	/// <summary>
	/// Called when the simulate-load checkbox changes so continuous load wakes immediately.
	/// </summary>
	[RelayCommand]
	private void SimulateLoadChanged()
	{
		if (ViewModel.IsSimulatingLoad)
		{
			ViewModel.PulseLoad();
		}

		_appDispatcher.RequestDispatch();
	}

	#endregion
}