#region References

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Browser;
using Cornerstone.Presentation.Platforms;
using Cornerstone.Extensions;
using Cornerstone.Platforms.Browser;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using SQLitePCL;

#endregion

namespace Cornerstone.Sample.Browser;

internal sealed class Program
{
	#region Fields

	private static AppViewModel _applicationViewModel;

	#endregion

	#region Methods

	public static AppBuilder BuildCornerstoneApp()
	{
		return AppBuilder.Configure<App>();
	}

	private static void AppViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(AppViewModel.SelectedTab):
			{
				var item = _applicationViewModel.SelectedTab;
				if (item is null)
				{
					break;
				}

				var browser = AppBootstrap.GetInstance<BrowserInteropProxy>();
				var location = browser.WindowsLocation;
				location = location.UpdateQueryParameter("Tab", item.TabName);
				browser.WindowsLocation = location;
				break;
			}
		}
	}

	private static async Task Main(string[] args)
	{
		try
		{
			AppBootstrap.StartupProfiler ??= new StartupProfiler();
			AppBootstrap.Initialize("Cornerstone.Sample", typeof(Program).Assembly, args);
			Batteries.Init();

			await BuildCornerstoneApp()
				.UseCornerstone<BrowserPlatformOptions>(args, out var options)
				.StartBrowserAppAsync("out", options);

			_applicationViewModel ??= AppBootstrap.GetInstance<AppViewModel>();
			_applicationViewModel.PropertyChanged += AppViewModelOnPropertyChanged;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
			throw;
		}
	}

	#endregion
}