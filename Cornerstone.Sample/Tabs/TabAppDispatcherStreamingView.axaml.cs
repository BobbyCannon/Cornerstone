#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Extensions;
using Cornerstone.Generators;
using Cornerstone.Reflection;
using Cornerstone.Threading;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabAppDispatcherStreamingView : CornerstoneUserControl<TabAppDispatcherStreamingViewModel>
{
	#region Fields

	private double _isStreaming;

	#endregion

	#region Constructors

	public TabAppDispatcherStreamingView()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	private void ClearOnClick(object sender, RoutedEventArgs e)
	{
		ViewModel?.Editor.Clear();
		StreamView.AutoScroll = true;
	}

	private void StreamOnClick(object sender, RoutedEventArgs e)
	{
		var viewModel = ViewModel;
		var profiler = Profiler;
		if (viewModel is null || profiler is null)
		{
			return;
		}

		if (Interlocked.CompareExchange(ref _isStreaming, 1, 0) != 0)
		{
			return;
		}

		const int updatePerSecond = 2000;
		var period = TimeSpan.FromMilliseconds(1000.0 / updatePerSecond);
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
						var word = RandomGenerator.GetItem(RandomGenerator.LoremIpsumWords);
						model.Append(word);

						if (Random.Shared.Next(5) == 0)
						{
							var punctuation = Random.Shared.Next(10) switch
							{
								< 6 => ".",
								< 8 => "!",
								< 9 => "?",
								_ => ","
							};
							model.Append(punctuation);
						}

						model.Append(Random.Shared.Next(8) == 0 ? '\n' : ' ');
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
				Interlocked.Exchange(ref _isStreaming, 0);
			}
		});
	}

	#endregion
}