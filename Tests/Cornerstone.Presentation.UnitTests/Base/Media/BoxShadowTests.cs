#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class BoxShadowTests
{
	#region Methods

	[PresentationTestMethod]
	[TestData(nameof(ParseGetData))]
	public void BoxShadowShouldParse(string source, bool isInset, double offsetX, double offsetY, double blur, double spread, string color)
	{
		var parsed = BoxShadow.Parse(source);
		CornerstoneTest.AreEqual(isInset, parsed.IsInset);
		CornerstoneTest.AreEqual(offsetX, parsed.OffsetX);
		CornerstoneTest.AreEqual(offsetY, parsed.OffsetY);
		CornerstoneTest.AreEqual(blur, parsed.Blur);
		CornerstoneTest.AreEqual(spread, parsed.Spread);
		CornerstoneTest.AreEqual(Color.Parse(color), parsed.Color);
	}

	[PresentationTestMethod]
	[DataRow(-15, 20, 0, 5, "red", false, "-15 20 0 5 red")]
	[DataRow(-15, 20, 0, 5, "red", true, "inset -15 20 0 5 red")]
	[DataRow(-15, 20, 5, 0, "red", false, "-15 20 5 red")]
	public void BoxShadowsShouldToString(double offsetX, double offsetY, double blur, double spread, string color, bool isInset, string expected)
	{
		var source = new BoxShadows(new BoxShadow
		{
			IsInset = isInset,
			OffsetX = offsetX,
			OffsetY = offsetY,
			Blur = blur,
			Spread = spread,
			Color = Color.Parse(color)
		});
		CornerstoneTest.AreEqual(expected, source.ToString(), true);
	}

	[PresentationTestMethod]
	public void BoxShadowsShouldToStringMultipleShadows()
	{
		var source = new BoxShadows(
			new BoxShadow
			{
				OffsetX = -20,
				OffsetY = -20,
				Blur = 60,
				Color = Color.Parse("#CCFFFFFF")
			},
			[
				new BoxShadow
				{
					OffsetX = 20,
					OffsetY = 20,
					Blur = 60,
					Color = Color.Parse("#33000000")
				}
			]);
		CornerstoneTest.AreEqual("-20 -20 60 #CCFFFFFF, 20 20 60 #33000000", source.ToString(), true);
	}

	public static IEnumerable<object[]> ParseGetData()
	{
		foreach (var extraSpaces in new[] { false, true })
		foreach (var inset in new[] { false, true })
		foreach (var color in new[] { "red", "#FF122403" })
		{
			for (var componentCount = 2; componentCount < 5; componentCount++)
			{
				var s = (inset ? "inset " : "") + "10 20";
				var blur = 0d;
				var spread = 0d;
				if (componentCount > 2)
				{
					s += " 30";
					blur = 30;
				}

				if (componentCount > 3)
				{
					s += " 40";
					spread = 40;
				}

				s += " " + color;

				if (extraSpaces)
				{
					s = " " + s.Replace(" ", "  ") + "   ";
				}

				yield return [s, inset, 10d, 20d, blur, spread, color];
			}
		}
	}

	#endregion
}