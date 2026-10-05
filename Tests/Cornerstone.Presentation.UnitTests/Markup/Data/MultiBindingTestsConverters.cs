#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Data;

[TestClass]
public class MultiBindingTestsConverters : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void StringFormatShouldBeApplied()
	{
		var textBlock = new TextBlock
		{
			DataContext = new Class1()
		};

		var format = "{0:0.0} + {1:00}";
		var target = new MultiBinding
		{
			StringFormat = format,
			Bindings =
			{
				new Binding(nameof(Class1.Foo)),
				new Binding(nameof(Class1.Bar))
			}
		};

		textBlock.Bind(TextBlock.TextProperty, target);

		CornerstoneTest.AreEqual(string.Format(format, 1, 2), textBlock.Text);
	}

	[PresentationTestMethod]
	public void StringFormatShouldBeAppliedAfterConverter()
	{
		var textBlock = new TextBlock
		{
			DataContext = new Class1()
		};

		var target = new MultiBinding
		{
			StringFormat = "Foo + Bar = {0}",
			Converter = new SumOfDoublesConverter(),
			Bindings =
			{
				new Binding(nameof(Class1.Foo)),
				new Binding(nameof(Class1.Bar))
			}
		};

		textBlock.Bind(TextBlock.TextProperty, target);

		CornerstoneTest.AreEqual("Foo + Bar = 3", textBlock.Text);
	}

	[PresentationTestMethod]
	public void StringFormatShouldNotBeAppliedWhenBindingToNonStringOrObject()
	{
		var textBlock = new TextBlock
		{
			DataContext = new Class1()
		};

		var target = new MultiBinding
		{
			StringFormat = "Hello {0}",
			Converter = new SumOfDoublesConverter(),
			Bindings =
			{
				new Binding(nameof(Class1.Foo)),
				new Binding(nameof(Class1.Bar))
			}
		};

		textBlock.Bind(Layoutable.WidthProperty, target);

		CornerstoneTest.AreEqual(3.0, textBlock.Width);
	}

	#endregion

	#region Classes

	private class Class1
	{
		#region Properties

		public double Bar { get; set; } = 2;
		public double Foo { get; set; } = 1;

		#endregion
	}

	private class SumOfDoublesConverter : IMultiValueConverter
	{
		#region Methods

		public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
		{
			return values.OfType<double>().Sum();
		}

		#endregion
	}

	#endregion
}