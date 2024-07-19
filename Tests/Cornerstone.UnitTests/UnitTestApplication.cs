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
			new StyleInclude(new Uri("avares://Cornerstone.Avalonia"))
			{
				Source = new Uri("/AvaloniaEdit/Themes/AvaloniaEdit.xaml", UriKind.Relative)
			}
		};

		foreach (var style in styles)
		{
			Current?.Styles.Add(style);
		}
		
		var resources = new[]
		{
			new ResourceInclude(new Uri("avares://Cornerstone.Avalonia"))
			{
				Source = new Uri("/Resources/Theme.axaml", UriKind.Relative)
			}
		};

		foreach (var resource in resources)
		{
			Current?.Resources.MergedDictionaries.Add(resource);
		}
	}

	#endregion
}