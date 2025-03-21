#region References

using Avalonia.Headless.NUnit;
using Cornerstone.Avalonia.TextEditor.CodeCompletion;
using Cornerstone.UnitTests;
using NUnit.Framework;

#endregion

namespace Cornerstone.IntegrationTests.Avalonia.TextEditor.CodeCompletion;

[TestFixture]
public class CompletionListTests : CornerstoneUnitTest
{
	#region Methods

	[AvaloniaTest]
	public void Order()
	{
		UnitTestApplication.InitializeStyles();

		var list = new CompletionList("Get-",
			[
				new CompletionData { DisplayText = "Get-ApplicationHost" },
				new CompletionData { DisplayText = "Get-ServiceHost" },
				new CompletionData { DisplayText = "Get-Host" }
			]
		);

		list.SetFilter("H");

		AreEqual(
			new[]
			{
				new CompletionData { DisplayText = "Get-Host", Priority = 3 },
				new CompletionData { DisplayText = "Get-ServiceHost", Priority = 6.011m },
				new CompletionData { DisplayText = "Get-ApplicationHost", Priority = 6.015m }
			},
			list.CompletionData
		);

		list.SetFilter("O");

		AreEqual(
			new[]
			{
				new CompletionData { DisplayText = "Get-Host", Priority = 7.005m },
				new CompletionData { DisplayText = "Get-ServiceHost", Priority = 7.012m },
				new CompletionData { DisplayText = "Get-ApplicationHost", Priority = 7.013m }
			},
			list.CompletionData
		);
	}

	[AvaloniaTest]
	public void OrderAndFilter()
	{
		UnitTestApplication.InitializeStyles();

		var list = new CompletionList("Get-",
			[
				new CompletionData { DisplayText = "Get-Application" },
				new CompletionData { DisplayText = "Get-ServiceHome" },
				new CompletionData { DisplayText = "Get-Host" }
			]
		);

		list.SetFilter("h");

		AreEqual(
			new[]
			{
				new CompletionData { DisplayText = "Get-Host", Priority = 4 },
				new CompletionData { DisplayText = "Get-ServiceHome", Priority = 7.011m }
			},
			list.CompletionData
		);
	}

	#endregion
}