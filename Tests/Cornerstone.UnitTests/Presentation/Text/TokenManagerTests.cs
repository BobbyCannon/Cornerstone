#region References

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation.Controls.Text;
using Cornerstone.Text.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Text;

[TestClass]
public class TokenManagerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetOverlappingTokensMatchesGetTokensWithoutAllocatingRange()
	{
		var viewModel = new TextEditorViewModel();
		viewModel.TokenManager.Initialize("json");
		viewModel.Load("[1,2,3]");

		var start = viewModel.TokenManager[0].StartOffset;
		var end = viewModel.TokenManager[0].EndOffset;
		var viaEnumerable = new List<Token>();
		foreach (var token in viewModel.TokenManager.GetTokens(start, end))
		{
			viaEnumerable.Add(token);
		}

		var viaOverlap = new List<Token>();
		foreach (var token in viewModel.TokenManager.GetOverlappingTokens(start, end))
		{
			viaOverlap.Add(token);
		}

		AreEqual(viaEnumerable.Count, viaOverlap.Count);
		AreEqual(viaEnumerable[0], viaOverlap[0]);
	}

	[TestMethod]
	public void GetTokensStopsAtRangeEnd()
	{
		var viewModel = new TextEditorViewModel();
		viewModel.TokenManager.Initialize("json");
		viewModel.Load("[1,2,3,4,5,6,7,8,9,10]");

		IsTrue(viewModel.TokenManager.Count > 4);

		var first = viewModel.TokenManager[0];
		var hits = 0;
		foreach (var token in viewModel.TokenManager.GetTokens(first.StartOffset, first.EndOffset))
		{
			hits++;
			IsTrue(token.StartOffset < first.EndOffset);
		}

		AreEqual(1, hits);
	}

	[TestMethod]
	public void MarkdownIndentAfterListDoesNotThrow()
	{
		var viewModel = new TextEditorViewModel();
		viewModel.TokenManager.Initialize("md");
		viewModel.Load("- Item One\r\n- Item Two\r\n- Item Three\r\n");
		viewModel.Caret.Move(viewModel.DocumentLength);
		viewModel.Insert("\t");
		AreEqual(39, viewModel.DocumentLength);
	}

	[TestMethod]
	[SuppressMessage("ReSharper", "CommentTypo")]
	public void TokensShouldBeReused()
	{
		var viewModel = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };
		viewModel.TokenManager.Initialize("json");

		viewModel.Load("[1]");
		AreEqual(3, viewModel.TokenManager.Count);

		viewModel.Caret.Move(2);
		viewModel.Insert(",2");
		AreEqual(5, viewModel.TokenManager.Count);

		//AreEqual(0, ((IQueue<Token>) viewModel.TokenManager.GetMemberValue("_pool")).Count);

		viewModel.RemoveAt(1, 2);
		AreEqual(3, viewModel.TokenManager.Count);

		//AreEqual(2, ((IQueue<Token>) viewModel.TokenManager.GetMemberValue("_pool")).Count);

		viewModel.Caret.Move(2);
		viewModel.Insert(",3");
		AreEqual(5, viewModel.TokenManager.Count);

		//AreEqual(0, ((IQueue<Token>) viewModel.TokenManager.GetMemberValue("_pool")).Count);
	}

	#endregion
}