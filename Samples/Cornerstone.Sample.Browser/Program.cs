#region References

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Rendering;
using Avalonia.Threading;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Avalonia.Platforms.Browser;
using Cornerstone.Platforms.Browser;
using AppBuilderExtension = Cornerstone.Avalonia.AppBuilderExtension;

#endregion

namespace Cornerstone.Sample.Browser;

internal class Program
{
	#region Methods

	private static async Task Main(string[] args)
	{
		var dependencyProvider = CornerstoneApplication.DependencyProvider;

		BrowserPlatform.Initialize(dependencyProvider);
		
		await AppBuilder
			.Configure<App>()
			.WithInterFont()
			.UseCornerstone(args, out var options)
			.StartBrowserAppAsync("out", options);
	}

	#endregion
}