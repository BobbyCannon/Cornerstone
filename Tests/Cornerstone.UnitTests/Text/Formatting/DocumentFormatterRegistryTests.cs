#region References

using System;
using System.Collections;
using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Text;
using Cornerstone.Text.Formatting;
using Cornerstone.Text.Parsing;
using Cornerstone.Text.Parsing.CSharp;
using Cornerstone.Text.Parsing.Json;
using Cornerstone.Text.Parsing.Markdown;
using Cornerstone.Text.Parsing.PowerShell;
using Cornerstone.Text.Parsing.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text.Formatting;

[TestClass]
public class DocumentFormatterRegistryTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void MapsCSharpTokenizer()
	{
		AssertFormatter<CSharpDocumentFormatter>(new CSharpTokenizer(new StringGapBuffer(""), new SpeedyQueue<Token>()));
	}

	[TestMethod]
	public void MapsJsonTokenizer()
	{
		AssertFormatter<JsonDocumentFormatter>(new JsonTokenizer(new StringGapBuffer(""), new SpeedyQueue<Token>()));
	}

	[TestMethod]
	public void MapsMarkdownTokenizer()
	{
		AssertFormatter<MarkdownDocumentFormatter>(new MarkdownTokenizer(new StringGapBuffer(""), new SpeedyQueue<Token>()));
	}

	[TestMethod]
	public void MapsPowerShellTokenizer()
	{
		AssertFormatter<PowerShellDocumentFormatter>(new PowerShellTokenizer(new StringGapBuffer(""), new SpeedyQueue<Token>()));
	}

	[TestMethod]
	public void MapsXmlTokenizer()
	{
		AssertFormatter<XmlDocumentFormatter>(new XmlTokenizer(new StringGapBuffer(""), new SpeedyQueue<Token>()));
	}

	[TestMethod]
	public void TryFormatReturnsFalseWhenTokenEnumerationThrows()
	{
		var buffer = new StringGapBuffer("{}");
		var tokenizer = new JsonTokenizer(buffer, new SpeedyQueue<Token>());
		IsFalse(DocumentFormatter.TryFormat(tokenizer, buffer, new ThrowingTokens(), new DocumentFormatOptions(), out var formatted));
		IsNull(formatted);
	}

	[TestMethod]
	public void TryFormatSucceedsForAllKnownTokenizers()
	{
		var buffer = new StringGapBuffer("x");
		Tokenizer[] tokenizers =
		[
			new JsonTokenizer(buffer, new SpeedyQueue<Token>()),
			new CSharpTokenizer(buffer, new SpeedyQueue<Token>()),
			new XmlTokenizer(buffer, new SpeedyQueue<Token>()),
			new MarkdownTokenizer(buffer, new SpeedyQueue<Token>()),
			new PowerShellTokenizer(buffer, new SpeedyQueue<Token>())
		];

		foreach (var tokenizer in tokenizers)
		{
			IsTrue(DocumentFormatter.TryFormat(tokenizer, buffer, new DocumentFormatOptions(), out var formatted));
			IsNotNull(formatted);
		}
	}

	[TestMethod]
	public void UnknownTokenizerReturnsFalse()
	{
		var buffer = new StringGapBuffer("x");
		var tokenizer = new Tokenizer(buffer, new SpeedyQueue<Token>());
		IsFalse(DocumentFormatter.TryFormat(tokenizer, buffer, new DocumentFormatOptions(), out _));
	}

	private void AssertFormatter<T>(Tokenizer tokenizer)
		where T : IDocumentFormatter
	{
		var formatter = DocumentFormatter.GetFormatter(tokenizer);
		IsTrue(formatter is T);
		IsTrue(DocumentFormatter.TryFormat(tokenizer, tokenizer.Buffer, new DocumentFormatOptions(), out _));
	}

	#endregion

	#region Classes

	private sealed class ThrowingTokens : IEnumerable<Token>
	{
		#region Methods

		public IEnumerator<Token> GetEnumerator()
		{
			throw new InvalidOperationException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}

	#endregion
}