#region References

using System;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class BrushTests
{
	#region Methods

	[PresentationTestMethod]
	public void ChangingOpacityRaisesInvalidated()
	{
		var target = new SolidColorBrush();

		RenderResourceTestHelper.AssertResourceInvalidation(target, () => { target.Opacity = 0.5; });
	}

	[PresentationTestMethod]
	public void ParseHexValueDoesntAcceptInvalidNumber()
	{
		Assert.Throws<FormatException>(() => Brush.Parse("#ff808g80"));
	}

	[PresentationTestMethod]
	public void ParseHexValueDoesntAcceptTooFewChars()
	{
		Assert.Throws<FormatException>(() => Brush.Parse("#ff"));
	}

	[PresentationTestMethod]
	public void ParseHexValueDoesntAcceptTooManyChars()
	{
		Assert.Throws<FormatException>(() => Brush.Parse("#ff5555555"));
	}

	[PresentationTestMethod]
	public void ParseParsesARGBHashBrush()
	{
		var result = (ISolidColorBrush) Brush.Parse("#40ff8844");

		CornerstoneTest.AreEqual(0xff, result.Color.R);
		CornerstoneTest.AreEqual(0x88, result.Color.G);
		CornerstoneTest.AreEqual(0x44, result.Color.B);
		CornerstoneTest.AreEqual(0x40, result.Color.A);
	}

	[PresentationTestMethod]
	[DataRow("rgb(255, 128, 64)")]
	[DataRow("rgba(255, 128, 64, 0.5)")]
	[DataRow("hsl(120, 100%, 50%)")]
	[DataRow("hsla(120, 100%, 50%, 0.5)")]
	[DataRow("hsv(300, 100%, 25%)")]
	[DataRow("hsva(300, 100%, 25%, 0.75)")]
	[DataRow("#40ff8844")]
	[DataRow("Green")]
	public void ParseParsesAllColorFormatBrushes(string input)
	{
		var brush = Brush.Parse(input);
		CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(brush);

		// The ColorTests already validate all color formats are parsed properly
		// Since Brush.Parse() forwards to Color.Parse() we don't need to repeat this
		// We can simply check if the parsed Brush's color matches what Color.Parse provides
		var expected = Color.Parse(input);
		CornerstoneTest.AreEqual(expected, (brush as ISolidColorBrush)?.Color);
	}

	[PresentationTestMethod]
	public void ParseParsesNamedBrushLowercase()
	{
		var result = (ISolidColorBrush) Brush.Parse("red");

		CornerstoneTest.AreEqual(0xff, result.Color.R);
		CornerstoneTest.AreEqual(0x00, result.Color.G);
		CornerstoneTest.AreEqual(0x00, result.Color.B);
		CornerstoneTest.AreEqual(0xff, result.Color.A);
	}

	[PresentationTestMethod]
	public void ParseParsesNamedBrushUppercase()
	{
		var result = (ISolidColorBrush) Brush.Parse("RED");

		CornerstoneTest.AreEqual(0xff, result.Color.R);
		CornerstoneTest.AreEqual(0x00, result.Color.G);
		CornerstoneTest.AreEqual(0x00, result.Color.B);
		CornerstoneTest.AreEqual(0xff, result.Color.A);
	}

	[PresentationTestMethod]
	public void ParseParsesRGBHashBrush()
	{
		var result = (ISolidColorBrush) Brush.Parse("#ff8844");

		CornerstoneTest.AreEqual(0xff, result.Color.R);
		CornerstoneTest.AreEqual(0x88, result.Color.G);
		CornerstoneTest.AreEqual(0x44, result.Color.B);
		CornerstoneTest.AreEqual(0xff, result.Color.A);
	}

	[PresentationTestMethod]
	public void ParseToStringNamedBrushRoundtrip()
	{
		const string expectedName = "Red";
		var brush = (ISolidColorBrush) Brush.Parse(expectedName);
		var name = brush.ToString();

		CornerstoneTest.AreEqual(expectedName, name);
	}

	#endregion
}