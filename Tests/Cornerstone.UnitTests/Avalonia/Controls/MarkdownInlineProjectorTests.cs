#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Avalonia.Text;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Markdown;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Controls;

[TestClass]
public class MarkdownInlineProjectorTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ProjectFragmentStripsInlineCodeMarkers()
	{
		var (text, tokens) = Project("Every `AppKeystone` exposes");
		AreEqual("Every AppKeystone exposes", text);
		IsTrue(tokens.Any(t => t.Type == MarkdownTokenizer.TokenTypeInlineCode));
		var code = tokens.First(t => t.Type == MarkdownTokenizer.TokenTypeInlineCode);
		AreEqual("AppKeystone", text.Substring(code.StartOffset, code.Length));
	}

	[TestMethod]
	public void ProjectFragmentAppliesBold()
	{
		var (text, tokens) = Project("**State** is truth");
		AreEqual("State is truth", text);
		IsTrue(tokens.Any(t => t.Bold || (t.Type == MarkdownTokenizer.TokenTypeBold)));
	}

	[TestMethod]
	public void ProjectFragmentBoldAndInlineCodeTogether()
	{
		var (text, tokens) = Project("**Compiler Safety:** uses `Nullable`");
		AreEqual("Compiler Safety: uses Nullable", text);
		IsTrue(tokens.Any(t => t.Bold || (t.Type == MarkdownTokenizer.TokenTypeBold)));
		IsTrue(tokens.Any(t => t.Type == MarkdownTokenizer.TokenTypeInlineCode));
	}

	[TestMethod]
	public void ProjectTableCellBold()
	{
		// Same path as MarkdownTablePresenter cells
		var (text, tokens) = Project("**State**");
		AreEqual("State", text);
		IsTrue(tokens.Any(t => t.Bold || (t.Type == MarkdownTokenizer.TokenTypeBold)));
	}

	[TestMethod]
	public void ProjectUnorderedListProjectsItemInlines()
	{
		var list = """
			*   **Compiler Safety:** uses `Nullable`
			*   **Warning Generation:** when NRTs are enabled
			""";
		var renderer = new TextRenderer();
		renderer.ViewModel.ViewMetrics.CharacterHeight = 20;
		renderer.ViewModel.ViewMetrics.CharacterWidth = 10;
		var links = MarkdownInlineProjector.ProjectUnorderedList(list.AsSpan(), renderer, view: null);
		var text = renderer.Text ?? string.Empty;

		IsTrue(text.Contains("• "));
		IsFalse(text.Contains("**"));
		IsFalse(text.Contains('`'));
		IsTrue(text.Contains("Compiler Safety:"));
		IsTrue(text.Contains("Nullable"));
		IsTrue(renderer.ViewModel.TokenManager.Any(t => t.Bold || (t.Type == MarkdownTokenizer.TokenTypeBold)));
		IsTrue(renderer.ViewModel.TokenManager.Any(t => t.Type == MarkdownTokenizer.TokenTypeInlineCode));
		AreEqual(0, links.Count);
	}

	[TestMethod]
	public void ProjectFragmentCapturesRelativeMarkdownLinks()
	{
		var (text, _, links) = ProjectWithLinks("See [Other](Other.md) and [up](../Keystone.md#phases).");
		AreEqual("See Other and up.", text);
		AreEqual(2, links.Count);
		AreEqual("Other.md", links[0].Href);
		AreEqual("Other", links[0].Text);
		AreEqual("../Keystone.md#phases", links[1].Href);
		AreEqual("up", links[1].Text);
		IsTrue(links[0].Contains(links[0].StartOffset));
		IsFalse(links[0].Contains(links[0].EndOffset));
	}

	[TestMethod]
	public void ProjectUnorderedListCapturesItemLinks()
	{
		var list = """
			*   Open [Keystone](Keystone.md)
			*   Then [Lifecycle](Lifecycle.md#phases)
			""";
		var renderer = new TextRenderer();
		renderer.ViewModel.ViewMetrics.CharacterHeight = 20;
		renderer.ViewModel.ViewMetrics.CharacterWidth = 10;
		var links = MarkdownInlineProjector.ProjectUnorderedList(list.AsSpan(), renderer, view: null);

		AreEqual(2, links.Count);
		AreEqual("Keystone.md", links[0].Href);
		AreEqual("Lifecycle.md#phases", links[1].Href);
		IsFalse((renderer.Text ?? string.Empty).Contains('['));
	}

	[TestMethod]
	public void TrySplitListItemParsesMarkerAndBody()
	{
		IsTrue(MarkdownInlineProjector.TrySplitListItem("* item", out var indent, out var body));
		AreEqual(0, indent);
		AreEqual("item", body.ToString());

		IsTrue(MarkdownInlineProjector.TrySplitListItem("  -  **Bold**", out indent, out body));
		AreEqual(2, indent);
		AreEqual("**Bold**", body.ToString());

		IsFalse(MarkdownInlineProjector.TrySplitListItem("not a list", out _, out _));
	}

	[TestMethod]
	public void TrimTrailingDisplayWhitespaceRemovesNewlinesAndSpaces()
	{
		var viewModel = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };
		viewModel.TokenManager.Initialize(new MarkdownViewTokenizer());
		var content = new StringBuffer();
		content.Add("Hello");
		content.Add("  \r\n\n");
		// Token covering "Hello" only
		var token = new MarkdownViewTokenizer().CreateOrUpdateSection(
			MarkdownTokenizer.TokenTypeBold, 0, 5, bold: true);
		viewModel.TokenManager.Add(token);

		MarkdownInlineProjector.TrimTrailingDisplayWhitespace(content, viewModel.TokenManager, null);
		AreEqual("Hello", content.ToString());
		AreEqual(1, viewModel.TokenManager.Count);
		AreEqual(5, viewModel.TokenManager[0].EndOffset);
	}

	[TestMethod]
	public void ProjectStripsTrailingWhitespaceFromFragment()
	{
		var renderer = new TextRenderer();
		renderer.ViewModel.ViewMetrics.CharacterHeight = 20;
		renderer.ViewModel.ViewMetrics.CharacterWidth = 10;
		MarkdownInlineProjector.Project("AppKeystone  \n\n", renderer, view: null);
		AreEqual("AppKeystone", renderer.Text);
	}

	private static (string text, List<Token> tokens) Project(string markdown)
	{
		var (text, tokens, _) = ProjectWithLinks(markdown);
		return (text, tokens);
	}

	private static (string text, List<Token> tokens, List<MarkdownProjectedLink> links) ProjectWithLinks(string markdown)
	{
		var viewModel = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };
		// Initialize with MarkdownViewTokenizer so projected tokens are retained (SupportsRebuilding = false).
		viewModel.TokenManager.Initialize(new MarkdownViewTokenizer());
		var content = new StringBuffer();
		var links = new List<MarkdownProjectedLink>();
		MarkdownInlineProjector.ProjectFragment(markdown, content, viewModel.TokenManager, links);
		MarkdownInlineProjector.TrimTrailingDisplayWhitespace(content, viewModel.TokenManager, links);
		return (content.ToString(), viewModel.TokenManager.ToList(), links);
	}

	#endregion
}