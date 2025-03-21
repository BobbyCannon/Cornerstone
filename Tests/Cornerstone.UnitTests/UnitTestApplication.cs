#region References

using System;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Cornerstone.Avalonia;
using Cornerstone.UnitTests;
using Cornerstone.Weaver;

#endregion

[assembly: AvaloniaTestApplication(typeof(UnitTestApplication))]

namespace Cornerstone.UnitTests;

[DoNotNotify]
public class UnitTestApplication : Application
{
	#region Methods

	public UnitTestApplication()
	{
		Styles.Add(new SimpleTheme());
	}

	public static AppBuilder BuildAvaloniaApp()
	{
		var builder = AppBuilder
			.Configure<UnitTestApplication>()
			//.With(new FontManagerOptions
			//{
			//	DefaultFamilyName = "Default",
			//	FontFallbacks =
			//	[
			//		new FontFallback
			//		{
			//			FontFamily = new FontFamily("Default")
			//		}
			//	]
			//})
			.WithCornerstoneFont()
			.UseHeadless(new AvaloniaHeadlessPlatformOptions
			{
				UseHeadlessDrawing = true
			});

		return builder;
	}


	public static void InitializeStyles()
	{
		//
		// NOTE: Do not add Cornerstone theme, fonts break
		//
		//Source = new Uri("/CornerstoneTheme.xaml", UriKind.Relative)

		var rootUri = new Uri("avares://Cornerstone.Avalonia");

		var styles = new[]
		{
			new StyleInclude(rootUri) { Source = new Uri("/TextEditor/Themes/TextEditor.axaml", UriKind.Relative) },
		};

		foreach (var style in styles)
		{
			Current?.Styles.Add(style);
		}

		var resources = new[]
		{
			new ResourceInclude(rootUri) { Source = new Uri("/Themes/Theme.axaml", UriKind.Relative) },
			new ResourceInclude(rootUri) { Source = new Uri("/Themes/Theme.Dark.axaml", UriKind.Relative) },
			new ResourceInclude(rootUri) { Source = new Uri("/Themes/Theme.Shared.axaml", UriKind.Relative) }
		};

		foreach (var resource in resources)
		{
			Current?.Resources.MergedDictionaries.Add(resource);
		}
	}
	
	public static void InitializeCornerstoneTheme()
	{
		Current.Styles.Clear();
		Current.Styles.Add(new StyleInclude(new Uri("avares://Cornerstone.Avalonia"))
		{
			Source = new Uri("/CornerstoneTheme.xaml", UriKind.Relative)
		});
		Current.RequestedThemeVariant = ThemeVariant.Dark;
	}

	#endregion
}