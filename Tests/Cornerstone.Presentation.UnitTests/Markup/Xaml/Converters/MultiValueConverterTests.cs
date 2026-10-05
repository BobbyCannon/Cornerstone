#nullable enable

#region References

using System;
using System.Collections.Generic;
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
public class MultiValueConverterTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void MultiValueConverterSpecialValuesWork()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:c='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Converters;assembly=Cornerstone.Presentation.UnitTests'>
    <TextBlock Name='textBlock'>
        <TextBlock.Tag>
            <MultiBinding Converter='{x:Static c:TestMultiValueConverter.Instance}' FallbackValue='bar'>
                <Binding Path='Item1' />
                <Binding Path='Item2' />
            </MultiBinding>
        </TextBlock.Tag>
    </TextBlock>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			window.ApplyTemplate();

			window.DataContext = Tuple.Create(2, 2);
			CornerstoneTest.AreEqual("foo", textBlock.Tag);

			window.DataContext = Tuple.Create(-3, 3);
			CornerstoneTest.AreEqual("foo", textBlock.Tag);

			window.DataContext = Tuple.Create(0, 2);
			CornerstoneTest.AreEqual("bar", textBlock.Tag);
		}
	}

	#endregion
}

public class TestMultiValueConverter : IMultiValueConverter
{
	#region Fields

	public static readonly TestMultiValueConverter Instance = new();

	#endregion

	#region Methods

	public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
	{
		if (values[0] is int i && values[1] is int j)
		{
			var p = i * j;

			if (p > 0)
			{
				return "foo";
			}

			if (p == 0)
			{
				return PresentationProperty.UnsetValue;
			}

			return BindingOperations.DoNothing;
		}

		return "(default)";
	}

	#endregion
}