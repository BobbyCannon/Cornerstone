#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Cornerstone.Avalonia;
using Cornerstone.Extensions;
using Cornerstone.Generators;
using Cornerstone.Reflection;
using Cornerstone.Threading;
using DispatcherPriority = Avalonia.Threading.DispatcherPriority;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabAppDispatcherPropertiesView : CornerstoneUserControl<TabAppDispatcherPropertiesViewModel>
{
	#region Fields

	private double _isRunning;
	private readonly DispatcherTimer _pollTimer;

	#endregion

	#region Constructors

	public TabAppDispatcherPropertiesView()
	{
		_pollTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal, (_, _) => RefreshModelDisplay())
		{
			IsEnabled = false
		};
		InitializeComponent();
	}

	#endregion

	#region Methods

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_pollTimer.IsEnabled = true;
		base.OnAttachedToVisualTree(e);
		RefreshModelDisplay();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_pollTimer.IsEnabled = false;
		base.OnDetachedFromVisualTree(e);
	}

	private void BumpItemCountOnClick(object sender, RoutedEventArgs e)
	{
		var viewModel = ViewModel;
		var profiler = Profiler;
		if (viewModel is null || profiler is null)
		{
			return;
		}

		// View edit: two-way map writes ItemCount back to model.Count on the next dispatcher tick.
		Task.Run(() => profiler.Time("Model", () => viewModel.ItemCount++));
	}

	private void RefreshModelDisplay()
	{
		if (ModelTitle is null || ViewModel is null)
		{
			return;
		}

		var model = ViewModel.Model;
		ModelTitle.Text = model.Title;
		ModelCount.Text = model.Count.ToString();
		ModelRatio.Text = model.Ratio.ToString("F2");
	}

	private void UpdateManyOnClick(object sender, RoutedEventArgs e)
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
		var model = viewModel.Model;

		Task.Run(async () =>
		{
			try
			{
				using var timer = new IntervalTimer(period);

				for (var i = 0; i < iterations; i++)
				{
					profiler.Time("Model", () =>
					{
						model.Title = RandomGenerator.GetItem(RandomGenerator.LoremIpsumWords);
						model.Count = RandomGenerator.NextInteger(0, 100);
						model.Ratio = RandomGenerator.NextInteger(0, 100) / 100.0;
					});

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
		if (viewModel is null || profiler is null)
		{
			return;
		}

		var model = viewModel.Model;
		Task.Run(() =>
		{
			profiler.Time("Model", () =>
			{
				model.Title = RandomGenerator.GetItem(RandomGenerator.LoremIpsumWords);
				model.Count = RandomGenerator.NextInteger(0, 100);
				model.Ratio = RandomGenerator.NextInteger(0, 100) / 100.0;
			});
		});
	}

	#endregion
}