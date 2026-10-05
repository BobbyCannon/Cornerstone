#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class TextDecorationTests
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldParseTextDecorations()
	{
		var baseline = TextDecorationCollection.Parse("baseline");

		CornerstoneTest.AreEqual(TextDecorationLocation.Baseline, baseline[0].Location);

		var underline = TextDecorationCollection.Parse("underline");

		CornerstoneTest.AreEqual(TextDecorationLocation.Underline, underline[0].Location);

		var overline = TextDecorationCollection.Parse("overline");

		CornerstoneTest.AreEqual(TextDecorationLocation.Overline, overline[0].Location);

		var strikethrough = TextDecorationCollection.Parse("strikethrough");

		CornerstoneTest.AreEqual(TextDecorationLocation.Strikethrough, strikethrough[0].Location);
	}

	#endregion
}