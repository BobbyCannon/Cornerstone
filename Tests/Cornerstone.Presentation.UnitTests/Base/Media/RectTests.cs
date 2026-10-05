#region References

using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class RectTests
{
	#region Methods

	[PresentationTestMethod]
	public void ParseParses()
	{
		var rect = Rect.Parse("1,2 3,-4");
		var expected = new Rect(1, 2, 3, -4);
		CornerstoneTest.AreEqual(expected, rect);
	}

	#endregion
}