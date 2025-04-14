#region References

using System;
using System.Linq;
using System.Web;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Rendering;
using Avalonia.Threading;
using Cornerstone.Avalonia.Camera;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Avalonia.MediaPlayer;
using Cornerstone.Avalonia.WebView;
using Cornerstone.Web;

#endregion

namespace Cornerstone.Avalonia.Platforms.Browser;

public static class AppBuilderExtensions
{
	#region Methods

	public static RendererDebugOverlays ParseArgs(string[] args, out BrowserPlatformOptions options)
	{
		options = new BrowserPlatformOptions();
		var overlays = RendererDebugOverlays.None;

		try
		{
			if ((args.Length == 0)
				|| !Uri.TryCreate(args[0], UriKind.Absolute, out var uri)
				|| (uri.Query.Length <= 1))
			{
				return overlays;
			}

			var queryParams = HttpUtility.ParseQueryString(uri.Query);

			if (bool.TryParse(queryParams[nameof(options.PreferFileDialogPolyfill)], out var preferDialogsPolyfill))
			{
				options.PreferFileDialogPolyfill = preferDialogsPolyfill;
			}

			if (bool.TryParse(queryParams[nameof(options.PreferManagedThreadDispatcher)], out var preferManagedThreadDispatcher))
			{
				options.PreferManagedThreadDispatcher = preferManagedThreadDispatcher;
			}

			if (queryParams[nameof(options.RenderingMode)] is { } renderingModePairs)
			{
				options.RenderingMode = renderingModePairs
					.Split(';', StringSplitOptions.RemoveEmptyEntries)
					.Select(entry => Enum.Parse<BrowserRenderingMode>(entry, true))
					.ToArray();
			}

			Enum.TryParse(queryParams[nameof(RendererDiagnostics.DebugOverlays)], out overlays);

			Console.WriteLine("DebugOverlays: " + overlays);
			Console.WriteLine("PreferFileDialogPolyfill: " + options.PreferFileDialogPolyfill);
			Console.WriteLine("PreferManagedThreadDispatcher: " + options.PreferManagedThreadDispatcher);
			Console.WriteLine("RenderingMode: " + string.Join(";", options.RenderingMode));

			return overlays;
		}
		catch (Exception ex)
		{
			Console.WriteLine("ParseArgs of BrowserPlatformOptions failed: " + ex);
			return overlays;
		}
	}

	public static AppBuilder UseCornerstone(this AppBuilder builder, string[] args, out BrowserPlatformOptions options)
	{
		ApplicationLifetimeExtensions.SetBrowserArgs(args);

		var overlays = ParseArgs(args, out options);

		return builder
			.AfterSetup(_ =>
			{
				Dispatcher.UIThread.InvokeAsync(
					() =>
					{
						if (Application.Current!.ApplicationLifetime is ISingleViewApplicationLifetime lifetime
							&& (overlays != default))
						{
							TopLevel.GetTopLevel(lifetime.MainView)!.RendererDiagnostics.DebugOverlays = overlays;
						}
					},
					DispatcherPriority.Background
				);
			})
			.AfterPlatformServicesSetup(_ =>
			{
				var dependencyProvider = CornerstoneApplication.DependencyProvider;
				dependencyProvider.AddSingleton<IBrowserInterop, BrowserInterop>();

				dependencyProvider.AddOrUpdateTransient<ICameraAdapter, CameraAdapterStub>();
				dependencyProvider.AddOrUpdateTransient<IMediaPlayerAdapter, MediaPlayerAdapterStub>();
				dependencyProvider.AddOrUpdateTransient<IWebViewAdapter, WebViewAdapter>();
			});
	}

	#endregion
}