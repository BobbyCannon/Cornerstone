#region References

using System;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Themes.Simple;
using Cornerstone.UnitTests;
using PropertyChanged;

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
			.With(new FontManagerOptions
			{
				DefaultFamilyName = "Default",
				FontFallbacks =
				[
					new FontFallback
					{
						FontFamily = new FontFamily("Default")
					}
				]
			})
			.UseHeadless(new AvaloniaHeadlessPlatformOptions
			{
				UseHeadlessDrawing = true
			});

		return builder;
	}

	public static void InitializeStyles()
	{
		var styles = new[]
		{
			new ResourceInclude(new Uri("avares://Cornerstone.Avalonia")) { Source = new Uri("AvaloniaEdit/Themes/Base.xaml", UriKind.Relative) },
			new ResourceInclude(new Uri("avares://Cornerstone.Avalonia")) { Source = new Uri("Resources/Themes.axaml", UriKind.Relative) }
		};

		foreach (var style in styles)
		{
			Current?.Resources.MergedDictionaries.Add(style);
		}
	}

	#endregion
}