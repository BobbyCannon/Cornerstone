#region References

using Cornerstone.Avalonia.AvaloniaEdit.CodeCompletion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.AvaloniaEdit.CodeCompletion;

[TestClass]
public class CompletionProviderTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetCompletionPrefix()
	{
		AreEqual(".\\", CompletionProvider.GetCompletionPrefix("cd .\\", "C:\\Program Files"));
		AreEqual("C:\\Prog", CompletionProvider.GetCompletionPrefix("cd C:\\Prog", "C:\\Program Files"));
		AreEqual("[Guid", CompletionProvider.GetCompletionPrefix("Write-Host [Guid", "[guid]"));
		AreEqual("", CompletionProvider.GetCompletionPrefix("Write-Host [Guid]::", "NewGuid"));
	}

	#endregion
}