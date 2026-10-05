#region References

using Cornerstone.Collections;
using Cornerstone.Text;
using Cornerstone.Text.Formatting;
using Cornerstone.Text.Parsing;
using Cornerstone.Text.Parsing.CSharp;
using Cornerstone.Text.Parsing.Markdown;
using Cornerstone.Text.Parsing.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text.Formatting;

[TestClass]
public class IdentityFormatterTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CSharpReturnsOriginal()
	{
		AssertIdentity(new CSharpTokenizer(new StringGapBuffer("class Foo { }"), new SpeedyQueue<Token>()));
	}

	[TestMethod]
	public void MarkdownReturnsOriginal()
	{
		AssertIdentity(new MarkdownTokenizer(new StringGapBuffer("# Title"), new SpeedyQueue<Token>()));
	}

	[TestMethod]
	public void PowerShellReturnsOriginal()
	{
		AssertIdentity(new PowerShellTokenizer(new StringGapBuffer("Get-ChildItem"), new SpeedyQueue<Token>()));
	}

	private void AssertIdentity(Tokenizer tokenizer)
	{
		var original = tokenizer.Buffer.ToString();
		IsTrue(DocumentFormatter.TryFormat(tokenizer, tokenizer.Buffer, new DocumentFormatOptions(), out var formatted));
		AreEqual(original, formatted);
	}

	#endregion
}