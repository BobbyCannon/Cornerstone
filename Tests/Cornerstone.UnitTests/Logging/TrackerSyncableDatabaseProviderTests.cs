#region References

using System;
using System.Linq;
using Cornerstone.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Server.Data;
using Sample.Shared.Storage;

#endregion

namespace Cornerstone.UnitTests.Logging;

[TestClass]
public class TrackerProviderTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddTrackerPaths()
	{
		var databaseProvider = new ServerMemoryDatabaseProvider(this);
		var trackerProvider = new TrackerProvider<IServerDatabase>(databaseProvider, this, TimeSpan.FromMinutes(5));
		using var database = databaseProvider.GetDatabase();

		AreEqual(0, database.TrackerPaths.Count);
		AreEqual(0, database.TrackerPathConfigurations.Count);

		trackerProvider.Write(new TrackerPath(this, null)
		{
			Name = "Name",
			Id = new Guid("CC4ADDDD-8B26-45EB-BE02-021CF2EA354E"),
			Values = [
				new TrackerPathValue("One", 1),
				new TrackerPathValue("true", true),
				new TrackerPathValue("false", false)
			]
		});

		var expected = database.TrackerPaths.First();
		AreEqual(new TrackerPathEntity
			{
				StartedOn = Now,
				CompletedOn = Now,
				ConfigurationId = 1,
				CreatedOn = Now,
				Id = 1,
				ModifiedOn = Now,
				SyncId = new Guid("CC4ADDDD-8B26-45EB-BE02-021CF2EA354E"),
				Value01 = "False",
				Value02 = "1",
				Value03 = "True"
			},
			expected
		);

		var expected2 = database.TrackerPathConfigurations.First();
		AreEqual(new TrackerPathConfigurationEntity
			{
				CreatedOn = Now,
				Id = 1,
				ModifiedOn = Now,
				Name01 = "false",
				Name02 = "One",
				Name03 = "true",
				PathName = "Name",
				PathType = "Name",
				SyncId = expected2.SyncId
			},
			expected2,
			null,
			nameof(TrackerPathConfigurationEntity.Paths)
		);
	}

	#endregion
}