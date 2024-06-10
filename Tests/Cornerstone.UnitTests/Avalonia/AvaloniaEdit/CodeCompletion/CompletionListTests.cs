#region References

using Cornerstone.Avalonia.AvaloniaEdit.CodeCompletion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.AvaloniaEdit.CodeCompletion;

[TestClass]
public class CompletionListTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Order()
	{
		//var list = new CompletionList("Get-");
		var list = new CompletionList("Get-", [
			new CompletionData { DisplayText = "Get-ApplicationHost" },
			new CompletionData { DisplayText = "Get-ServiceHost" },
			new CompletionData { DisplayText = "Get-Host" }
		]);

		var actual = list.FilterAndOrderList("H");
		AreEqual(
			new[]
			{
				new CompletionData { DisplayText = "Get-Host" },
				new CompletionData { DisplayText = "Get-ServiceHost" },
				new CompletionData { DisplayText = "Get-ApplicationHost" }
			},
			actual
		);

		actual = list.FilterAndOrderList("O");
		AreEqual(
			new[]
			{
				new CompletionData { DisplayText = "Get-Host" },
				new CompletionData { DisplayText = "Get-ServiceHost" },
				new CompletionData { DisplayText = "Get-ApplicationHost" }
			},
			actual
		);
	}

	[TestMethod]
	public void OrderAndFilter()
	{
		//var list = new CompletionList("Get-");
		var list = new CompletionList("Get-", [
			new CompletionData { DisplayText = "Get-Application" },
			new CompletionData { DisplayText = "Get-ServiceHome" },
			new CompletionData { DisplayText = "Get-Host" }
		]);

		var actual = list.FilterAndOrderList("h");
		AreEqual(
			new[]
			{
				new CompletionData { DisplayText = "Get-Host" },
				new CompletionData { DisplayText = "Get-ServiceHome" }
			},
			actual
		);
	}

	#endregion
}