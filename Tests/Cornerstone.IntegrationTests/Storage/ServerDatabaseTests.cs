#region References

using System;
using System.Linq;
using Cornerstone.EntityFramework;
using Cornerstone.Extensions;
using Cornerstone.Sync;
using Cornerstone.Testing;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Server;

#endregion

namespace Cornerstone.IntegrationTests.Storage;

[TestClass]
public class ServerDatabaseTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Add()
	{
		ForEachServerDatabaseProvider(provider =>
		{
			var expected = GetServerModel<AddressEntity>();

			using (var database = provider.GetDatabase())
			{
				database.EnsureMigrated();
				database.Addresses.Add(expected);
				database.SaveChanges();
			}

			using (var database = provider.GetDatabase())
			{
				var actual = database.Addresses.FirstOrDefault(x => x.Id == expected.Id);
				IsNotNull(actual);
				AreEqual(expected, actual.Unwrap());
			}
		});
	}

	public void ForEachServerDatabaseProvider(Action<SyncableDatabaseProvider<IServerDatabase>> test)
	{
		var providers = GetServerDatabaseProviders(false, false, false);

		foreach (var provider in providers)
		{
			provider.GetType().FullName.Dump();
			test(provider);
		}
	}

	#endregion
}