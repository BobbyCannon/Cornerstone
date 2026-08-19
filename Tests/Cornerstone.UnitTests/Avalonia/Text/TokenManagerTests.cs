#region References

using System.Diagnostics.CodeAnalysis;
using Cornerstone.Avalonia.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Text;

[TestClass]
public class TokenManagerTests : CornerstoneUnitTest
{
	#region Methods

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

	#endregion
}