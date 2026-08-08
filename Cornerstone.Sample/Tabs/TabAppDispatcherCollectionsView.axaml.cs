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
public partial class TabAppDispatcherCollectionsView : CornerstoneUserControl<TabAppDispatcherCollectionsViewModel>
{
	#region Fields

	private double _isRunning;

	#endregion

	#region Constructors

	public TabAppDispatcherCollectionsView()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	private void ClearOnClick(object sender, RoutedEventArgs e)
	{
		var viewModel = ViewModel;
		var profiler = Profiler;
		if ((viewModel is null) || (profiler is null))
		{
			return;
		}

		Task.Run(() => profiler.Time("Model", () => viewModel.Model.Clear()));
	}

	private void UpdateManyOnClick(object sender, RoutedEventArgs e)
	{
		var viewModel = ViewModel;
		var profiler = Profiler;
		if ((viewModel is null) || (profiler is null))
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
					profiler.Time("Model", viewModel.MutateOnce);

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

	private void UpdateOnceOnClick(object sender, RoutedEventArgs e)
	{
		var viewModel = ViewModel;
		var profiler = Profiler;
		if ((viewModel is null) || (profiler is null))
		{
			return;
		}

		Task.Run(() => profiler.Time("Model", viewModel.MutateOnce));
	}

	#endregion
}
