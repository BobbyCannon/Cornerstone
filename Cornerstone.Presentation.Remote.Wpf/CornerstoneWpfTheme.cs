#region References

using System;
using System.Collections;
using System.Windows;
using System.Windows.Media;

#endregion

namespace Cornerstone.Presentation.Remote.Wpf;

/// <summary>
/// Light or dark brushes generated from the Cornerstone theme palette.
/// Unprefixed brush keys stay in place and change color when the theme changes,
/// so DynamicResource templates follow without being rebuilt.
/// </summary>
public static class CornerstoneWpfTheme
{
	#region Fields

	private static readonly ResourceDictionary Source;

	#endregion

	#region Constructors

	static CornerstoneWpfTheme()
	{
		Source = new ResourceDictionary();
		Source.Source = new Uri(
			"pack://application:,,,/Cornerstone.Presentation.Remote.Wpf;component/Themes/CornerstoneTheme.xaml",
			UriKind.Absolute);
	}

	#endregion

	#region Methods

	public static void Apply(ResourceDictionary dictionary, bool dark)
	{
		var prefix = dark ? "Dark." : "Light.";
		foreach (DictionaryEntry entry in Source)
		{
			var key = entry.Key as string;
			var from = entry.Value as SolidColorBrush;
			if ((key == null) || (from == null) || !key.StartsWith(prefix))
			{
				continue;
			}

			var localKey = key.Substring(prefix.Length);
			if ((dictionary[localKey] is SolidColorBrush { IsFrozen: false } live))
			{
				live.Color = from.Color;
			}
			else
			{
				dictionary[localKey] = new SolidColorBrush(from.Color);
			}
		}
	}

	#endregion
}
