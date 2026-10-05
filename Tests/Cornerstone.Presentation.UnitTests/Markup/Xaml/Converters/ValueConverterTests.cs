#nullable enable

#region References

using System;
using System.Globalization;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Converters;

[TestClass]
public class ValueConverterTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ValueConverterSpecialValuesWork()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:c='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Converters;assembly=Cornerstone.Presentation.UnitTests'>
    <TextBlock Name='textBlock' Text='{Binding Converter={x:Static c:TestConverter.Instance}, FallbackValue=bar}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			window.ApplyTemplate();

			window.DataContext = 2;
			CornerstoneTest.AreEqual("foo", textBlock.Text);

			window.DataContext = -3;
			CornerstoneTest.AreEqual("foo", textBlock.Text);

			window.DataContext = 0;
			CornerstoneTest.AreEqual("bar", textBlock.Text);
		}
	}

	#endregion
}

public class TestConverter : IValueConverter
{
	#region Fields

	public static readonly TestConverter Instance = new();

	#endregion

	#region Methods

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is int i)
		{
			if (i > 0)
			{
				return "foo";
			}

			if (i == 0)
			{
				return PresentationProperty.UnsetValue;
			}

			return BindingOperations.DoNothing;
		}

		return "(default)";
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	#endregion
}