#region References

using System.Diagnostics;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.IO;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.IntegrationTests.IO;

[TestClass]
public class FolderListItemTests : HierarchyListItemUnitTest
{
	#region Methods

	[TestMethod]
	public void FolderRefresh()
	{
		var dispatcher = GetDispatcher();
		var samplePath = $@"{SolutionDirectory}\..\Cornerstone\Samples\Sample.Shared";
		var folder = new FolderListItem(samplePath, dispatcher);
		var actual = new TextBuilder();
		var settings = new HierarchySettings();
		settings.Exclusions.AddRange("bin", "obj");
		var watch = Stopwatch.StartNew();
		folder.Refresh(settings);
		watch.Stop();
		watch.Elapsed.Dump();
		var expected = @"+ Sample.Shared
	+ Storage
		+ Client
			- ClientAccount.cs
			- ClientAddress.cs
			- ClientLogEvent.cs
			- ClientSetting.cs
		+ Server
			- AccountEntity.cs
			- AccountRole.cs
			- AddressEntity.cs
			- FoodEntity.cs
			- FoodRelationshipEntity.cs
			- GroupEntity.cs
			- GroupMemberEntity.cs
			- LogEventEntity.cs
			- PathValueDataType.cs
			- PetEntity.cs
			- PetTypeEntity.cs
			- SettingEntity.cs
			- TrackerPathConfigurationEntity.cs
			- TrackerPathEntity.cs
		- IClientDatabase.cs
		- IServerDatabase.cs
	+ Sync
		- AccountSync.cs
		- AddressSync.cs
		- LogEventSync.cs
		- LogLevel.cs
	- FodyWeavers.xml
	- FodyWeavers.xsd
	- Sample.Shared.csproj
	- SharedSyncManager.cs
	- SharedViewModel.cs
	- WebClientTest.cs
";

		GetDetails(actual, folder);
		AreEqual(expected, actual.ToString());
	}

	private void GetDetails(TextBuilder builder, FolderListItem folder)
	{
		builder.AppendLineThenPushIndent($"+ {folder.Name}");

		foreach (var subFolder in folder.Children
					.Where(x => x is FolderListItem)
					.Cast<FolderListItem>())
		{
			GetDetails(builder, subFolder);
		}

		foreach (var file in folder.Children
					.Where(x => x is FileListItem)
					.Cast<FileListItem>())
		{
			builder.AppendLine($"- {file.Name}");
		}

		builder.PopIndent();
	}

	#endregion
}