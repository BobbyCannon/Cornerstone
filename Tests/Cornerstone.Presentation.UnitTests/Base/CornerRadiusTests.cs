#region References

using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class CornerRadiusTests
{
	#region Methods

	[PresentationTestMethod]
	public void ParseAcceptsSpaces()
	{
		var result = CornerRadius.Parse("1.1 2.2 3.3 4.4");

		CornerstoneTest.AreEqual(new CornerRadius(1.1, 2.2, 3.3, 4.4), result);
	}

	[PresentationTestMethod]
	public void ParseParsesSingleUniformRadius()
	{
		var result = CornerRadius.Parse("3.4");

		CornerstoneTest.AreEqual(new CornerRadius(3.4), result);
	}

	[PresentationTestMethod]
	public void ParseParsesTopBottom()
	{
		var result = CornerRadius.Parse("1.1,2.2");

		CornerstoneTest.AreEqual(new CornerRadius(1.1, 2.2), result);
	}

	[PresentationTestMethod]
	public void ParseParsesTopLeftTopRightBottomRightBottomLeft()
	{
		var result = CornerRadius.Parse("1.1,2.2,3.3,4.4");

		CornerstoneTest.AreEqual(new CornerRadius(1.1, 2.2, 3.3, 4.4), result);
	}

	#endregion
}