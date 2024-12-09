#region References

using System.Linq;
using Cornerstone.EntityFramework;
using Cornerstone.Extensions;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Shared.Storage.Client;

#endregion

namespace Cornerstone.IntegrationTests.Storage;

[TestClass]
public class ClientDatabaseTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Add()
	{
		ForEachClientDatabaseProvider(provider =>
		{
			var expected = GetModel<ClientAddress>();

			using (var database = provider.GetDatabase())
			{
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

	[TestMethod]
	public void Relationships()
	{
		ForEachClientDatabaseProvider(provider =>
		{
			var address = GetModel<ClientAddress>();
			var account = GetModel<ClientAccount>();
			address.Accounts.Add(account);

			using (var database = provider.GetDatabase())
			{
				database.Addresses.Add(address);
				database.SaveChanges();
			}

			using (var database = provider.GetDatabase())
			{
				var addresses = database.Addresses.ToList();
				IsNotNull(addresses);
				AreEqual(address.Unwrap(), addresses[0].Unwrap());

				var accounts = database.Accounts.ToList();
				IsNotNull(accounts);
				AreEqual(account.Unwrap(), accounts[0].Unwrap());
			}
		});
	}

	#endregion
}