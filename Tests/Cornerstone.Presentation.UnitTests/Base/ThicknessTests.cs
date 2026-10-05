#region References

using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class ThicknessTests
{
	#region Methods

	[PresentationTestMethod]
	public void ParseAcceptsSpaces()
	{
		var result = Thickness.Parse("1.2 3.4 5 6");

		CornerstoneTest.AreEqual(new Thickness(1.2, 3.4, 5, 6), result);
	}

	[PresentationTestMethod]
	public void ParseParsesHorizontalVertical()
	{
		var result = Thickness.Parse("1.2,3.4");

		CornerstoneTest.AreEqual(new Thickness(1.2, 3.4), result);
	}

	[PresentationTestMethod]
	public void ParseParsesLeftTopRightBottom()
	{
		var result = Thickness.Parse("1.2, 3.4, 5, 6");

		CornerstoneTest.AreEqual(new Thickness(1.2, 3.4, 5, 6), result);
	}

	[PresentationTestMethod]
	public void ParseParsesSingleUniformSize()
	{
		var result = Thickness.Parse("1.2");

		CornerstoneTest.AreEqual(new Thickness(1.2), result);
	}

	#endregion
}