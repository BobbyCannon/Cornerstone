#region References

using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class RectTests
{
	#region Methods

	[PresentationTestMethod]
	public void NormalizeShouldMakeInvalidRectsEmpty()
	{
		var result = new Rect(
				double.NegativeInfinity, double.PositiveInfinity,
				double.PositiveInfinity, double.PositiveInfinity)
			.Normalize();

		CornerstoneTest.AreEqual(default, result);
	}

	[PresentationTestMethod]
	public void NormalizeShouldReverseNegativeSize()
	{
		var result = new Rect(new Point(100, 100), new Point(0, 0)).Normalize();

		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), result);
	}

	[PresentationTestMethod]
	public void UnionShouldIgnoreEmptyOtherrect()
	{
		var result = new Rect(0, 0, 100, 100).Union(new Rect(150, 150, 0, 0));

		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), result);
	}

	[PresentationTestMethod]
	public void UnionShouldIgnoreEmptyThisrect()
	{
		var result = new Rect(0, 0, 0, 0).Union(new Rect(150, 150, 100, 100));

		CornerstoneTest.AreEqual(new Rect(150, 150, 100, 100), result);
	}

	[PresentationTestMethod]
	public void UnionShouldReturnCorrectValueForIntersectingRects()
	{
		var result = new Rect(0, 0, 100, 100).Union(new Rect(50, 50, 100, 100));

		CornerstoneTest.AreEqual(new Rect(0, 0, 150, 150), result);
	}

	[PresentationTestMethod]
	public void UnionShouldReturnCorrectValueForNonIntersectingRects()
	{
		var result = new Rect(0, 0, 100, 100).Union(new Rect(150, 150, 100, 100));

		CornerstoneTest.AreEqual(new Rect(0, 0, 250, 250), result);
	}

	#endregion
}