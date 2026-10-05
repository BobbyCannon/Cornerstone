#region References

using System.Globalization;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Converters;

[TestClass]
public class ColorOpacityConverterTests
{
	#region Methods

	[TestMethod]
	public void ClampsOpacityToZeroAndOne()
	{
		var converter = new ColorOpacityConverter();
		var source = Colors.White;
		var high = (Color) converter.Convert(source, typeof(Color), 2.0, CultureInfo.InvariantCulture);
		var low = (Color) converter.Convert(source, typeof(Color), -1.0, CultureInfo.InvariantCulture);
		Assert.AreEqual(255, high.A);
		Assert.AreEqual(0, low.A);
	}

	[TestMethod]
	public void ColorTargetTypeReturnsColorWithReplacedAlpha()
	{
		var converter = new ColorOpacityConverter();
		var source = Color.FromArgb(255, 10, 20, 30);
		var result = converter.Convert(source, typeof(Color), 0.4, CultureInfo.InvariantCulture);
		var color = (Color) result;
		Assert.AreEqual(102, color.A);
		Assert.AreEqual(10, color.R);
		Assert.AreEqual(20, color.G);
		Assert.AreEqual(30, color.B);
	}

	[TestMethod]
	public void ConvertBackReturnsUnsetValue()
	{
		var converter = new ColorOpacityConverter();
		var result = converter.ConvertBack(Colors.Red, typeof(Color), 0.5, CultureInfo.InvariantCulture);
		Assert.AreSame(PresentationProperty.UnsetValue, result);
	}

	[TestMethod]
	public void GradientBrushReturnsUnsetValue()
	{
		var converter = new ColorOpacityConverter();
		var gradient = new LinearGradientBrush();
		var result = converter.Convert(gradient, typeof(IBrush), 0.5, CultureInfo.InvariantCulture);
		Assert.AreSame(PresentationProperty.UnsetValue, result);
	}

	[TestMethod]
	public void SolidBrushReturnsBrushWithColorAlpha()
	{
		var converter = new ColorOpacityConverter();
		var source = new SolidColorBrush(Color.FromRgb(1, 2, 3));
		var result = converter.Convert(source, typeof(IBrush), "0.5", CultureInfo.InvariantCulture);
		var brush = (SolidColorBrush) result;
		Assert.AreEqual(128, brush.Color.A);
		Assert.AreEqual(1, brush.Color.R);
		Assert.AreEqual(2, brush.Color.G);
		Assert.AreEqual(3, brush.Color.B);
		Assert.AreEqual(1, brush.Opacity);
	}

	[TestMethod]
	public void StaticWithOpacityUsesParameter()
	{
		var result = ColorConverters.WithOpacity.Convert(Colors.Blue, typeof(Color), 0, CultureInfo.InvariantCulture);
		Assert.AreEqual(0, ((Color) result).A);
	}

	[TestMethod]
	public void UsesOpacityPropertyWhenParameterOmitted()
	{
		var converter = new ColorOpacityConverter { Opacity = 0.25 };
		var result = (Color) converter.Convert(Colors.Red, typeof(Color), null, CultureInfo.InvariantCulture);
		Assert.AreEqual(64, result.A);
		Assert.AreEqual(Colors.Red.R, result.R);
	}

	#endregion
}