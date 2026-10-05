#region References

using System.Linq;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class UnicodeRangeTests
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldParseSegments()
	{
		var range = UnicodeRange.Parse("U+0, U+1, U+2, U+3");

		CornerstoneTest.IsNotNull(range.Segments);
		CornerstoneTest.AreEqual(new[] { 0, 1, 2, 3 }, range.Segments.Select(x => x.Start));
	}

	#endregion
}