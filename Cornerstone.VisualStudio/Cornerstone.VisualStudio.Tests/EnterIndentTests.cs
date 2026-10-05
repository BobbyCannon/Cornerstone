#region References

using System.Threading.Tasks;
using CompletionMetadata = Cornerstone.VisualStudio.Core.AssemblyMetadata.Metadata;
using Cornerstone.VisualStudio.EditorHost;
using Cornerstone.VisualStudio.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests;

[TestClass]
public class EnterIndentTests
{
	#region Methods

	[TestMethod]
	public async Task SpaceIndentedOpenTagAddsOneIndentLevel()
	{
		var line = "                    <SelectableTextBlock";
		var insertion = await InsertAsync(line, line.Length, string.Empty, 4, "\n");
		var leading = EnterIndent.LeadingWhitespace(line);
		Assert.AreEqual("\n" + leading + new string(' ', 8), insertion);
		Assert.AreEqual(20, leading.Length);
	}

	[TestMethod]
	public async Task TabIndentedOpenTagAddsOneTab()
	{
		var line = "\t\t<SelectableTextBlock";
		var insertion = await InsertAsync(line, line.Length, string.Empty, 4, "\n");
		Assert.AreEqual("\n\t\t\t\t", insertion);
	}

	[TestMethod]
	public async Task ContentLineCopiesLeadingWhitespace()
	{
		var line = "\t\tHello";
		var insertion = await InsertAsync(line, line.Length, string.Empty, 4, "\n");
		Assert.AreEqual("\n\t\t", insertion);
	}

	[TestMethod]
	public async Task ClosingTagCopiesLeadingWhitespace()
	{
		var line = "\t\t</Grid>";
		var insertion = await InsertAsync(line, line.Length, string.Empty, 4, "\n");
		Assert.AreEqual("\n\t\t", insertion);
	}

	[TestMethod]
	public async Task FinishedOpeningTagAddsOneLevel()
	{
		var line = "\t\t<Grid>";
		var insertion = await InsertAsync(line, line.Length, string.Empty, 4, "\n");
		Assert.AreEqual("\n\t\t\t", insertion);
	}

	[TestMethod]
	public async Task SelfClosingTagDoesNotAddALevel()
	{
		var line = "\t\t<Grid/>";
		var insertion = await InsertAsync(line, line.Length, string.Empty, 4, "\n");
		Assert.AreEqual("\n\t\t", insertion);
	}

	[TestMethod]
	public async Task MidLineCaretKeepsIndentWithoutTheTail()
	{
		var line = "\t\tHello world";
		var insertion = await InsertAsync(line, 7, string.Empty, 4, "\r\n");
		Assert.AreEqual("\r\n\t\t", insertion);
	}

	[TestMethod]
	public async Task AttributeContinuationKeepsCurrentIndent()
	{
		var line = "\t\t\t\tWidth=\"1\"";
		var insertion = await InsertAsync(line, line.Length, "\t\t", 4, "\n");
		Assert.AreEqual("\n\t\t\t\t", insertion);
	}

	[TestMethod]
	public async Task BlankLineUsesPreviousIndent()
	{
		var insertion = await InsertAsync(string.Empty, 0, "\t\t", 4, "\n");
		Assert.AreEqual("\n\t\t", insertion);
	}

	private static async Task<string> InsertAsync(string line, int caret, string previousLine, int indentSize, string newLine)
	{
		using (var server = new EditorServer(new CompletionMetadata()))
		{
			server.Start();
			using (var connection = await EditorConnection.ConnectAsync(server.Port))
			{
				var ide = new VisualStudioInterface(connection);
				var response = await ide.BuildEnterIndentAsync(line, caret, previousLine, indentSize, newLine);
				Assert.AreEqual(string.Empty, response.Error);
				return response.Insertion;
			}
		}
	}

	#endregion
}
