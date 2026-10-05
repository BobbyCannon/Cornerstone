#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Controls.Documents;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.TextFormatting;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class InlineTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldInheritBackgroundInNestedInlines()
	{
		var backgroundBrush = Brushes.Red;
		var span = new Span();
		var innerSpan = new Span();
		var run = new Run("Test");

		span.Background = backgroundBrush;
		innerSpan.Inlines.Add(run);
		span.Inlines.Add(innerSpan);

		var textRuns = new List<TextRun>();
		span.BuildTextRun(textRuns);

		var runProperties = textRuns[0].Properties;
		CornerstoneTest.IsNotNull(runProperties);
		CornerstoneTest.AreEqual(backgroundBrush, runProperties.BackgroundBrush);
	}

	[PresentationTestMethod]
	public void ShouldInheritFontStretchInNestedInlines()
	{
		var span = new Span();
		var innerSpan = new Span();
		var run = new Run("Test");
		span.FontStretch = FontStretch.Condensed;
		innerSpan.Inlines.Add(run);
		span.Inlines.Add(innerSpan);

		var textRuns = new List<TextRun>();
		span.BuildTextRun(textRuns);

		var runProperties = textRuns[0].Properties;
		CornerstoneTest.IsNotNull(runProperties);
		CornerstoneTest.AreEqual(FontStretch.Condensed, runProperties.Typeface.Stretch);
	}

	[PresentationTestMethod]
	public void ShouldInheritFontStyleInNestedInlines()
	{
		var italic = new Italic();
		var span = new Span();
		var run = new Run("Test");
		span.Inlines.Add(run);
		italic.Inlines.Add(span);

		var textRuns = new List<TextRun>();
		italic.BuildTextRun(textRuns);

		var runProperties = textRuns[0].Properties;
		CornerstoneTest.IsNotNull(runProperties);
		CornerstoneTest.AreEqual(FontStyle.Italic, runProperties.Typeface.Style);
	}

	[PresentationTestMethod]
	public void ShouldInheritFontWeightInNestedInlines()
	{
		var bold = new Bold();
		var span = new Span();
		var run = new Run("Test");
		span.Inlines.Add(run);
		bold.Inlines.Add(span);

		var textRuns = new List<TextRun>();
		bold.BuildTextRun(textRuns);

		var runProperties = textRuns[0].Properties;
		CornerstoneTest.IsNotNull(runProperties);
		CornerstoneTest.AreEqual(FontWeight.Bold, runProperties.Typeface.Weight);
	}

	#endregion
}