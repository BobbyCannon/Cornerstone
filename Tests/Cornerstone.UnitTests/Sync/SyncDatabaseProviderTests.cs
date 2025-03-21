#region References

using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

#endregion

namespace Cornerstone.UnitTests.Sync;

/// <summary>
/// Summary description for SyncDatabaseProviderTests.
/// </summary>
[TestClass]
public class SyncDatabaseProviderTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetDatabase()
	{
		var database = new Mock<ISyncableDatabase>();
		var provider = new SyncableDatabaseProvider2<ISyncableDatabase>((x, y) => database.Object, null, null, this);

		IsTrue(database.Object == provider.GetSyncableDatabase());
	}

	#endregion
}