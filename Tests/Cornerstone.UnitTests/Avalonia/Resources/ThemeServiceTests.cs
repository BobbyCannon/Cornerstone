#region References

using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Cornerstone.Extensions;
using Cornerstone.Testing;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Resources;

[TestFixture]
public class ThemeServiceTests : UnitTestApplication
{
	#region Methods

	[AvaloniaTest]
	public void ExtractColors()
	{
		var theme = new ResourceInclude(new Uri("avares://Cornerstone.Avalonia")) { Source = new Uri("Resources/Themes.axaml", UriKind.Relative) };
		var dictionary = (ResourceDictionary) theme.Loaded.ThemeDictionaries[ThemeVariant.Dark];

		dictionary.Keys.OrderBy(x=> x).DumpPrettyJson();
	}

	#endregion
}