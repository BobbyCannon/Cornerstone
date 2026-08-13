#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Extensions;
using Cornerstone.Reflection;
using Cornerstone.Threading;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabAppDispatcherSeriesView : CornerstoneUserControl<TabAppDispatcherSeriesViewModel>
{
	#region Fields

	private double _isRunning;

	#endregion

	#region Constructors

	public TabAppDispatcherSeriesView()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	private void FixedManyOnClick(object sender, RoutedEventArgs e)
	{
		var viewModel = ViewModel;
		var profiler = Profiler;
		if (viewModel is null || profiler is null)
		{
			return;
		}

		if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
		{
			return;
		}

		const int updatesPerSecond = 2000;
		var period = TimeSpan.FromMilliseconds(1000.0 / updatesPerSecond);
		const int iterations = 10000;

		Task.Run(async () =>
		{
			try
			{
				using var timer = new IntervalTimer(period);

				for (var i = 0; i < iterations; i++)
				{
					profiler.Time("Model", viewModel.MutateFixedOnce);

					if (!await timer.WaitForNextTickAsync())
					{
						break;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToDetailedString());
			}
			finally
			{
				Interlocked.Exchange(ref _isRunning, 0);
			}
		});
	}

	private void FixedOnceOnClick(object sender, RoutedEventArgs e)
	{
		var viewModel = ViewModel;
		var profiler = Profiler;
		if (viewModel is null || profiler is null)
		{
			return;
		}

		Task.Run(() => profiler.Time("Model", viewModel.MutateFixedOnce));
	}

	private void GrowOnClick(object sender, RoutedEventArgs e)
	{
		// Length / caption are presentation concerns; still mutate SpeedyList so TrackSeries applies.
		ViewModel?.GrowVariableWindow();
	}

	private void ShrinkOnClick(object sender, RoutedEventArgs e)
	{
		ViewModel?.ShrinkVariableWindow();
	}

	private void VariableValuesOnClick(object sender, RoutedEventArgs e)
	{
		var viewModel = ViewModel;
		var profiler = Profiler;
		if (viewModel is null || profiler is null)
		{
			return;
		}

		// Values can be produced Off Dispatcher like Grok analytics rebuilds.
		Task.Run(() => profiler.Time("Model", viewModel.MutateVariableValues));
	}

	#endregion
}