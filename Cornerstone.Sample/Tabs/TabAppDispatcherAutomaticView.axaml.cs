#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Extensions;
using Cornerstone.Generators;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Threading;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabAppDispatcherAutomaticView : CornerstoneUserControl
{
	#region Fields

	private double _isRunningMany;

	#endregion

	#region Constructors

	public TabAppDispatcherAutomaticView()
	{
		InitializeComponent();
	}

	#endregion

	#region Properties

	public TabAppDispatcherAutomaticViewModel Host => DataContext as TabAppDispatcherAutomaticViewModel;

	#endregion

	#region Methods

	public void SetModelValueText(string text)
	{
		if (ModelValue is not null)
		{
			ModelValue.Text = text;
		}
	}

	private void AttachViewOnClick(object sender, RoutedEventArgs e)
	{
		Host?.AttachView();
	}

	private void DetachViewOnClick(object sender, RoutedEventArgs e)
	{
		Host?.DetachView();
	}

	private void UpdateManyOnClick(object sender, RoutedEventArgs e)
	{
		var host = Host;
		var profiler = Profiler;
		if (host is null || profiler is null)
		{
			return;
		}

		if (Interlocked.CompareExchange(ref _isRunningMany, 1, 0) != 0)
		{
			return;
		}

		const int updatePerSecond = 2000;
		var period = TimeSpan.FromMilliseconds(1000.0 / updatePerSecond);
		var model = host.Model;
		var throttle = new Throttle(
			() => this.DispatchPost(() => SetModelValueText(model.Number.ToString())),
			TimeSpan.FromSeconds(1));

		Task.Run(async () =>
		{
			try
			{
				using var timer = new IntervalTimer(period);

				for (var i = 0; i < 10000; i++)
				{
					profiler.Time("Model", () => { model.Number = RandomGenerator.NextInteger(0, 100); });
					throttle.Trigger();

					if (!await timer.WaitForNextTickAsync())
					{
						break;
					}
				}

				throttle.Trigger(true);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToDetailedString());
			}
			finally
			{
				Interlocked.Exchange(ref _isRunningMany, 0);
			}
		});
	}

	private void UpdateOnceOnClick(object sender, RoutedEventArgs e)
	{
		var host = Host;
		var profiler = Profiler;
		if (host is null || profiler is null)
		{
			return;
		}

		var model = host.Model;
		Task.Run(() =>
		{
			profiler.Time("Model", () => model.Number = RandomGenerator.NextInteger(0, 100));
			this.DispatchPost(() => SetModelValueText(model.Number.ToString()));
		});
	}

	#endregion
}