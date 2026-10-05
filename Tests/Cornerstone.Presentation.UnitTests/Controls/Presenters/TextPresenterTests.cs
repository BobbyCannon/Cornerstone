#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Presenters;

[TestClass]
public class TextPresenterTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void HideCaretShouldKeepTheTextLayout()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var presenter = new TextPresenter { Text = "hello" };
			presenter.Measure(Size.Infinity);

			var textLayout = presenter.TextLayout;
			presenter.HideCaret();

			// The caret blinks over the text rather than taking part in it, so hiding it
			// repaints; rebuilding the layout would reshape the text for nothing, and would
			// dispose a layout its callers may still be holding.
			CornerstoneTest.IsTrue(ReferenceEquals(textLayout, presenter.TextLayout));
		}
	}

	[PresentationTestMethod]
	public void MeasureAndArrangeShouldUseWidthIncludingTrailingWhitespaceForBounds()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var presenter = new TextPresenter
			{
				Text = "fy",
				FontStyle = FontStyle.Italic,
				FontSize = 48,
				UseLayoutRounding = false
			};

			presenter.Measure(Size.Infinity);

			var expectedSize = new Size(presenter.TextLayout.WidthIncludingTrailingWhitespace, presenter.TextLayout.Height);
			CornerstoneTest.AreEqual(expectedSize, presenter.DesiredSize);

			presenter.Arrange(new Rect(default, presenter.DesiredSize));
			CornerstoneTest.AreEqual(new Rect(default, expectedSize), presenter.Bounds);
		}
	}

	[PresentationTestMethod]
	public void TextPresenterCanContainNullWithOutPasswordCharSet()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new TextPresenter();

			CornerstoneTest.IsNotNull(target.TextLayout);
		}
	}

	[PresentationTestMethod]
	public void TextPresenterCanContainNullWithPasswordCharSet()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new TextPresenter { PasswordChar = '*' };
			CornerstoneTest.IsNotNull(target.TextLayout);
		}
	}

	[PresentationTestMethod]
	public void TextPresenterReplacesFormattedTextWithPasswordChar()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new TextPresenter { PasswordChar = '*', Text = "Test" };

			target.Measure(Size.Infinity);

			CornerstoneTest.IsNotNull(target.TextLayout);

			var actual = string.Join(null,
				target.TextLayout.TextLines.SelectMany(x => x.TextRuns).Select(x => x.Text.ToString()));

			CornerstoneTest.AreEqual("****", actual);
		}
	}

	[PresentationTestMethod]
	[DataRow(FontStretch.Condensed)]
	[DataRow(FontStretch.Expanded)]
	[DataRow(FontStretch.Normal)]
	[DataRow(FontStretch.ExtraCondensed)]
	[DataRow(FontStretch.SemiCondensed)]
	[DataRow(FontStretch.ExtraExpanded)]
	[DataRow(FontStretch.SemiExpanded)]
	[DataRow(FontStretch.UltraCondensed)]
	[DataRow(FontStretch.UltraExpanded)]
	public void TextPresenterShouldUseFontStretchProperty(FontStretch fontStretch)
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var presenter = new TextPresenter { FontStretch = fontStretch, Text = "test" };
			CornerstoneTest.IsNotNull(presenter.TextLayout);
			CornerstoneTest.AreEqual(1, presenter.TextLayout.TextLines.Count);
			CornerstoneTest.AreEqual(1, presenter.TextLayout.TextLines[0].TextRuns.Count);
			CornerstoneTest.IsNotNull(presenter.TextLayout.TextLines[0].TextRuns[0].Properties);
			CornerstoneTest.AreEqual(fontStretch, presenter.TextLayout.TextLines[0].TextRuns[0].Properties!.Typeface.Stretch);
		}
	}

	#endregion
}