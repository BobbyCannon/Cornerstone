#region References

using System;
using System.Linq;
using System.Web;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Browser;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.Controls.Camera;
using Cornerstone.Presentation.Controls.MediaPlayer;
using Cornerstone.Platforms.Browser;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Web;
using Cornerstone.Presentation.Controls.Chrome;

using Dispatcher = Cornerstone.Presentation.Threading.Dispatcher;
using DispatcherPriority = Cornerstone.Presentation.DispatcherPriority;

#endregion

namespace Cornerstone.Presentation.Platforms.Browser;

internal static class AppBuilderExtensions
{
	#region Methods

	/// <summary>
	/// Ex.
	/// ?DebugOverlays=Fps, DirtyRects, LayoutTimeGraph, RenderTimeGraph
	/// </summary>
	public static RendererDebugOverlays ParseBrowserPlatformOptions(string[] args, out BrowserPlatformOptions options)
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

			//Console.WriteLine("DebugOverlays: " + overlays);
			//Console.WriteLine("PreferFileDialogPolyfill: " + options.PreferFileDialogPolyfill);
			//Console.WriteLine("PreferManagedThreadDispatcher: " + options.PreferManagedThreadDispatcher);
			//Console.WriteLine("RenderingMode: " + string.Join(";", options.RenderingMode));

			return overlays;
		}
		catch (Exception ex)
		{
			Console.WriteLine("ParseArgs of BrowserPlatformOptions failed: " + ex);
			return overlays;
		}
	}

	public static AppBuilder UseCornerstone<T>(AppBuilder builder, string[] args, out T options) where T : class
	{
		var overlays = ParseBrowserPlatformOptions(args, out var platformOptions);
		options = platformOptions as T;

		// Cornerstone (11.x) has issues with responsiveness with WASM MT
		// This will probably be fixed eventually
		// An alternative is to run a small infinite animation
		//options.PreferManagedThreadDispatcher = false;

		var dependencyProvider = AppBootstrap.DependencyProvider;
		// Factory avoids SourceReflector constructor discovery. That lookup throws on
		// WASM for CornerstoneBrowserInteropProxy, and the exception escapes the pointer-up.
		dependencyProvider.SetTransient<BrowserInteropProxy, CornerstoneBrowserInteropProxy>(() => new CornerstoneBrowserInteropProxy());
		dependencyProvider.SetTransient<IWebViewAdapter, WebViewAdapter>(() => new WebViewAdapter());
		dependencyProvider.SetTransient<ICameraAdapter, CameraAdapterStub>(() => new CameraAdapterStub());
		dependencyProvider.SetTransient<BaseMediaPlayerAdapter, MediaPlayerAdapterStub>(() => new MediaPlayerAdapterStub());

		return builder
			.AfterSetup(_ =>
			{
				if (overlays == default)
				{
					return;
				}

				Dispatcher.UIThread.InvokeAsync(
					() =>
					{
						if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime lifetime
							&& (lifetime.MainView != null))
						{
							var topLevel = TopLevel.GetTopLevel(lifetime.MainView);
							if (topLevel != null)
							{
								topLevel.RendererDiagnostics.DebugOverlays = overlays;
							}
						}
					},
					DispatcherPriority.Background
				);
			});
	}


#endregion
}
