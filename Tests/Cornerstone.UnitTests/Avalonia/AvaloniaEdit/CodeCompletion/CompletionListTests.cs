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
		var data = CompletionProvider.CreateList(null);
		data.Load(
			new CompletionData { DisplayText = "Get-ApplicationHost" },
			new CompletionData { DisplayText = "Get-ServiceHost" },
			new CompletionData { DisplayText = "Get-Host" }
		);

		var list = new CompletionList("Get-", data);
		list.SetFilter("H");

		AreEqual(
			new[]
			{
				new CompletionData { DisplayText = "Get-Host", Priority = 3 },
				new CompletionData { DisplayText = "Get-ServiceHost", Priority = 6.011m },
				new CompletionData { DisplayText = "Get-ApplicationHost", Priority = 6.015m }
			},
			list.Suggestions.Filtered
		);

		list.SetFilter("O");

		AreEqual(
			new[]
			{
				new CompletionData { DisplayText = "Get-Host", Priority = 7.005m },
				new CompletionData { DisplayText = "Get-ServiceHost", Priority = 7.012m },
				new CompletionData { DisplayText = "Get-ApplicationHost", Priority = 7.013m }
			},
			list.Suggestions.Filtered
		);
	}

	[TestMethod]
	public void OrderAndFilter()
	{
		var data = CompletionProvider.CreateList(null);
		data.Load(
			new CompletionData { DisplayText = "Get-Application" },
			new CompletionData { DisplayText = "Get-ServiceHome" },
			new CompletionData { DisplayText = "Get-Host" }
		);
		
		var list = new CompletionList("Get-", data);
		list.SetFilter("h");

		AreEqual(
			new[]
			{
				new CompletionData { DisplayText = "Get-Host", Priority = 4 },
				new CompletionData { DisplayText = "Get-ServiceHome", Priority = 7.011m }
			},
			list.Suggestions.Filtered
		);
	}

	#endregion
}