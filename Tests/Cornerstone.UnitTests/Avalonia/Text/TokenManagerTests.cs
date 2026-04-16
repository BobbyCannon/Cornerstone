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

	#endregion
}