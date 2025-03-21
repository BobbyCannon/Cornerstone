#region References

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Client.Data;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class SyncableDatabaseTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetSyncableRepositories()
	{
		using var database = GetInstance<ClientMemoryDatabase>();
		var syncableRepositories = database.GetSyncableRepositories();
		var expected = new[]
		{
			"Cornerstone.Logging.TrackerPathConfigurationEntity,Cornerstone",
			"Cornerstone.Logging.TrackerPathEntity,Cornerstone",
			"Sample.Shared.Storage.Client.ClientAccount,Sample.Shared",
			"Sample.Shared.Storage.Client.ClientAddress,Sample.Shared",
			"Sample.Shared.Storage.Client.ClientLogEvent,Sample.Shared",
			"Sample.Shared.Storage.Client.ClientSetting,Sample.Shared"
		};

		var actual = syncableRepositories.Select(x => x.TypeName).ToArray();
		AreEqual(expected, actual);
	}

	#endregion
}