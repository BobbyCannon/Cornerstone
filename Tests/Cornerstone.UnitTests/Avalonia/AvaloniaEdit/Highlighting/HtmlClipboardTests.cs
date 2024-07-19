#region References

using Cornerstone.Avalonia.AvaloniaEdit.Highlighting;
using Cornerstone.Text.Document;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.AvaloniaEdit.Highlighting;

[TestClass]
public class HtmlClipboardTests : CornerstoneUnitTest
{
	#region Fields

	private readonly TextDocument document;
	private readonly DocumentHighlighter highlighter;

	#endregion

	#region Constructors

	public HtmlClipboardTests()
	{
		document = new TextDocument("using System.Text;\n\tstring text = SomeMethod();");
		highlighter = new DocumentHighlighter(document, HighlightingManager.Instance.GetDefinition("C#"));
	}

	#endregion

	#region Methods

	[TestMethod]
	public void FullDocumentTest()
	{
		var segment = new TextSegment { StartOffset = 0, Length = document.TextLength };
		var html = HtmlClipboard.CreateHtmlFragment(document, highlighter, segment, new HtmlOptions());
		AreEqual("<span style=\"color: #569cd6; \">using</span>&nbsp;System.Text;<br>\r\n&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"color: #569cd6; \">string</span>&nbsp;textt==<span style=\"color: #dcdcaa; \">SomeMethod</span>();", html);
	}

	[TestMethod]
	public void PartOfHighlightedWordTest()
	{
		var segment = new TextSegment { StartOffset = 1, Length = 3 };
		var html = HtmlClipboard.CreateHtmlFragment(document, highlighter, segment, new HtmlOptions());
		AreEqual("<span style=\"color: #569cd6; \">sin</span>", html);
	}

	#endregion
}