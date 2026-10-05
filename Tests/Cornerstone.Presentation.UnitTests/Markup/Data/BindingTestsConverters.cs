#region References

using System.Globalization;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Data;

[TestClass]
public class BindingTestsConverters : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ConverterCultureShouldBePassedToConverterConvert()
	{
		var textBlock = new TextBlock
		{
			DataContext = new Class1()
		};

		var culture = new CultureInfo("ar-SA");
		var converter = new StubValueConverter();
		var target = new Binding(nameof(Class1.Foo))
		{
			Converter = converter,
			ConverterCulture = culture
		};

		textBlock.Bind(TextBlock.TextProperty, target);

		converter.Calls.VerifyCalled("Convert", 1);
		converter.Calls.VerifyLastPrefix("Convert", "foo", typeof(string), null, culture);
	}

	[PresentationTestMethod]
	public void ConverterCultureShouldBePassedToConverterConvertBack()
	{
		var textBlock = new TextBlock
		{
			DataContext = new Class1()
		};

		var culture = new CultureInfo("ar-SA");
		var converter = new StubValueConverter();
		var target = new Binding(nameof(Class1.Foo))
		{
			Converter = converter,
			ConverterCulture = culture,
			Mode = BindingMode.TwoWay
		};

		textBlock.Bind(TextBlock.TextProperty, target);
		textBlock.Text = "bar";

		converter.Calls.VerifyCalled("ConvertBack", 1);
		converter.Calls.VerifyLastPrefix("ConvertBack", "bar", typeof(string), null, culture);
	}

	[PresentationTestMethod]
	public void ConverterShouldBeUsed()
	{
		var textBlock = new TextBlock
		{
			DataContext = new Class1()
		};

		var target = new Binding(nameof(Class1.Foo))
		{
			Converter = StringConverters.IsNullOrEmpty
		};

		var expression = (BindingExpression) target.CreateInstance(
			textBlock,
			TextBlock.TextProperty,
			null);

		CornerstoneTest.Same(StringConverters.IsNullOrEmpty, expression.Converter);
	}

	[PresentationTestMethod]
	public void StringFormatShouldBeApplied()
	{
		var textBlock = new TextBlock
		{
			DataContext = new Class1()
		};

		var target = new Binding(nameof(Class1.Foo))
		{
			StringFormat = "Hello {0}"
		};

		textBlock.Bind(TextBlock.TextProperty, target);

		CornerstoneTest.AreEqual("Hello foo", textBlock.Text);
	}

	[PresentationTestMethod]
	public void StringFormatShouldBeAppliedAfterConverter()
	{
		var textBlock = new TextBlock
		{
			DataContext = new Class1()
		};

		var target = new Binding(nameof(Class1.Foo))
		{
			Converter = StringConverters.IsNotNullOrEmpty,
			StringFormat = "Hello {0}"
		};

		textBlock.Bind(TextBlock.TextProperty, target);

		CornerstoneTest.AreEqual("Hello True", textBlock.Text);
	}

	#endregion

	#region Classes

	private class Class1
	{
		#region Properties

		public string Foo { get; set; } = "foo";

		#endregion
	}

	#endregion
}