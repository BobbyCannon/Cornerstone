#region References

using System;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class EffectTests
{
	#region Constants

	private const uint Black = 0xff000000;

	#endregion

	#region Methods

	[PresentationTestMethod]
	[DataRow("blur")]
	[DataRow("blur(")]
	[DataRow("blur()")]
	[DataRow("blur(123")]
	[DataRow("blur(aaab)")]
	[DataRow("drop-shadow(-10  -20 -30)")]
	public void InvalidEffectParseFails(string b)
	{
		Assert.Throws<ArgumentException>(() => Effect.Parse(b));
	}

	[PresentationTestMethod]
	[DataRow("blur(2.5)", 4, 4, 4, 4)]
	[DataRow("blur(0)", 0, 0, 0, 0)]
	[DataRow("drop-shadow(10 15)", 0, 0, 10, 15)]
	[DataRow("drop-shadow(10 15 5)", 0, 0, 16, 21)]
	[DataRow("drop-shadow(0 0 5)", 6, 6, 6, 6)]
	[DataRow("drop-shadow(3 3 5)", 3, 3, 9, 9)]
	public void PaddingIsCorrectlyCalculated(string effect, double left, double top, double right, double bottom)
	{
		var padding = Effect.Parse(effect).GetEffectOutputPadding();
		CornerstoneTest.AreEqual(left, padding.Left);
		CornerstoneTest.AreEqual(top, padding.Top);
		CornerstoneTest.AreEqual(right, padding.Right);
		CornerstoneTest.AreEqual(bottom, padding.Bottom);
	}

	[PresentationTestMethod]
	public void ParseParsesBlur()
	{
		var effect = (ImmutableBlurEffect) Effect.Parse("blur(123.34)");
		CornerstoneTest.AreEqual(123.34, effect.Radius);
	}

	[PresentationTestMethod]
	[DataRow("drop-shadow(10 20)", 10, 20, 0, Black)]
	[DataRow("drop-shadow( 10  20 ) ", 10, 20, 0, Black)]
	[DataRow("drop-shadow( 10  20 30 ) ", 10, 20, 30, Black)]
	[DataRow("drop-shadow(10  20 30)", 10, 20, 30, Black)]
	[DataRow("drop-shadow(-10  -20 30)", -10, -20, 30, Black)]
	[DataRow("drop-shadow(10 20 30 #ffff00ff)", 10, 20, 30, 0xffff00ff)]
	[DataRow("drop-shadow ( 10 20 30 #ffff00ff ) ", 10, 20, 30, 0xffff00ff)]
	[DataRow("drop-shadow(10 20 30 red)", 10, 20, 30, 0xffff0000)]
	[DataRow("drop-shadow ( 10   20   30 red  ) ", 10, 20, 30, 0xffff0000)]
	[DataRow("drop-shadow(10 20 30 rgba(100, 30, 45, 90%))", 10, 20, 30, 0xe6641e2d)]
	[DataRow("drop-shadow(10 20 30  rgba(100, 30, 45, 90%) ) ", 10, 20, 30, 0xe6641e2d)]
	public void ParseParsesDropShadow(string s, double x, double y, double r, uint color)
	{
		var effect = (ImmutableDropShadowEffect) Effect.Parse(s);
		CornerstoneTest.AreEqual(x, effect.OffsetX);
		CornerstoneTest.AreEqual(y, effect.OffsetY);
		CornerstoneTest.AreEqual(r, effect.BlurRadius);
		CornerstoneTest.AreEqual(1, effect.Opacity);
		CornerstoneTest.AreEqual(color, effect.Color.ToUInt32());
	}

	#endregion
}