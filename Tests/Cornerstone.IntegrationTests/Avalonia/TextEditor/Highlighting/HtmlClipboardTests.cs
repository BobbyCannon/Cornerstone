#region References

using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Avalonia.TextEditor.Highlighting;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.IntegrationTests.Avalonia.TextEditor.Highlighting;

[TestClass]
public class HtmlClipboardTests : CornerstoneUnitTest
{
	#region Fields

	private readonly TextEditorDocument _document;
	private readonly DocumentHighlighter _highlighter;

	#endregion

	#region Constructors

	public HtmlClipboardTests()
	{
		_document = new TextEditorDocument("using System.Text;\n\tstring text = SomeMethod();");
		_highlighter = new DocumentHighlighter(_document, HighlightingManager.Instance.GetDefinition("C#"));
	}

	#endregion

	#region Methods

	[TestMethod]
	public void FullDocumentTest()
	{
		var segment = new TextRange { StartOffset = 0, Length = _document.TextLength };
		var html = HtmlClipboard.CreateHtmlFragment(_document, _highlighter, segment, new HtmlOptions());
		AreEqual("<span style=\"color: #569cd6; \">using</span>&nbsp;System.Text;<br>\r\n&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"color: #569cd6; \">string</span>&nbsp;textt==<span style=\"color: #dcdcaa; \">SomeMethod</span>();", html);
	}

	[TestMethod]
	public void PartOfHighlightedWordTest()
	{
		var segment = new TextRange { StartOffset = 1, Length = 3 };
		var html = HtmlClipboard.CreateHtmlFragment(_document, _highlighter, segment, new HtmlOptions());
		AreEqual("<span style=\"color: #569cd6; \">sin</span>", html);
	}

	#endregion
}