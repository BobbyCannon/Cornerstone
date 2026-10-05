#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class BoxShadowsTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow("5 5 10 0 rgba(10,20,30,0.4)")]
	[DataRow("5 5 10 0 hsla(10,20%,30%,0.4)")]
	[DataRow("5 5 10 0 hsva(10,20%,30%,0.4)")]
	public void ParseColorFunctionIsHandled(string input)
	{
		var bs = BoxShadows.Parse(input);
		CornerstoneTest.AreEqual(1, bs.Count);
		var reparsed = BoxShadows.Parse(bs.ToString());
		CornerstoneTest.AreEqual(bs, reparsed);
	}

	[PresentationTestMethod]
	[DataRow("1 2 3 0 #FF0000", 1)]
	[DataRow("10 20 30 5 rgba(0,0,0,0.5)", 1)]
	[DataRow("1 2 3 0 #FF0000, 1 2 3 0 #FF0000", 2)]
	[DataRow("10 20 30 5 rgba(0,0,0,0.5), 1 2 3 0 #FF0000", 2)]
	[DataRow("10 20 30 5 rgba(0,0,0,0.5), 10 20 30 5 rgba(0,0,0,0.5)", 2)]
	[DataRow("10 20 30 5 rgba(0,0,0,0.5), 10 20 30 5 rgba(0,0,0,0.5), 10 20 30 5 rgba(0,0,0,0.5)", 3)]
	[DataRow("10 20 30 5 rgba(0,0,0,0.5), 10 20 30 5 #ffffff, 10 20 30 5 Red", 3)]
	[DataRow("  10 20 30 5 rgba(0, 0, 0, 0.5), 10 20 30 5 rgba(0, 0, 0, 0.5), 10 20 30 5 rgba(0, 0, 0, 0.5)  ", 3)]
	[DataRow("  10 20 30 5 rgba(0, 0, 0, 0.5), 10 20 30 5 #ffffff, 10 20 30 5 Red  ", 3)]
	public void ParseMultipleShadows(string input, int count)
	{
		var bs = BoxShadows.Parse(input);
		CornerstoneTest.AreEqual(count, bs.Count);
		var reparsed = BoxShadows.Parse(bs.ToString());
		CornerstoneTest.AreEqual(bs, reparsed);
	}

	[PresentationTestMethod]
	[DataRow("none")]
	[DataRow(" none ")]
	public void ParseNoneReturnsEmpty(string input)
	{
		var bs = BoxShadows.Parse(input);
		CornerstoneTest.AreEqual(0, bs.Count);
		CornerstoneTest.AreEqual(default, bs);
		CornerstoneTest.AreEqual("none", bs.ToString());
	}

	[PresentationTestMethod]
	[DataRow("0 0 5 0 #FF0000")]
	[DataRow("10 20 30 5 rgba(0,0,0,0.5)")]
	[DataRow("10 20 30 5 rgba(0, 0, 0, 0.5)")]
	[DataRow("  10  20  30  5  rgba(0,  0,  0,  0.5)  ")]
	public void ParseSingleShadowToStringRoundTrip(string input)
	{
		var bs = BoxShadows.Parse(input);
		CornerstoneTest.AreEqual(1, bs.Count);
		var str = bs.ToString();
		var reparsed = BoxShadows.Parse(str);
		CornerstoneTest.AreEqual(bs, reparsed);
	}

	[PresentationTestMethod]
	[DataRow("0 0 5 0 #FF0000", 10.0)]
	[DataRow("0 0 10 0 rgba(0,0,0,0.5)", 20.0)]
	public void TransformBoundsIncludesShadowExpansion(string input, double minExpansion)
	{
		var bs = BoxShadows.Parse(input);
		var rect = new Rect(0, 0, 100, 100);
		var transformed = bs.TransformBounds(rect);
		CornerstoneTest.IsTrue(transformed.Width >= (rect.Width + minExpansion));
		CornerstoneTest.IsTrue(transformed.Height >= (rect.Height + minExpansion));
	}

	#endregion
}