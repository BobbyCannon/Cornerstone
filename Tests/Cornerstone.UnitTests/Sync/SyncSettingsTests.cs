#region References

using Cornerstone.Sync;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Client.Data;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class SyncSettingsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ShouldFilterAllRepositoriesWithoutFilters()
	{
		using var database = GetInstance<ClientMemoryDatabase>();
		var settings = new SyncSettings();
		var syncableRepositories = database.GetSyncableRepositories();

		foreach (var repository in syncableRepositories)
		{
			repository.TypeName.Dump();

			IsFalse(settings.ShouldSyncRepository(repository.TypeName));
		}
	}

	#endregion
}