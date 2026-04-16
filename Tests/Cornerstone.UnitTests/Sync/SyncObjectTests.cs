#region References

using System;
using Cornerstone.Sample.Models;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class SyncObjectTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ToSyncObject()
	{
		var account = new Account
		{
			CreatedOn = StartDateTime,
			EmailAddress = "john@domain.com",
			IsDeleted = false,
			LastLoginDate = StartDateTime,
			ModifiedOn = StartDateTime,
			Name = "John Doe",
			Picture = null,
			Roles = ",,",
			Status = AccountStatus.Enabled,
			SyncId = new Guid("B9825F51-AEC7-4C18-A83F-6B4558B0EA20"),
			TimeZoneId = string.Empty
		};
		var syncObject = SyncObject.ToSyncObject(account);
		AreEqual(84, syncObject.Data.Length);
		AreEqual(SyncObjectStatus.Added, syncObject.Status);

		var syncModel = syncObject.ToSyncModel();
		AreEqual(account, syncModel);
	}

	#endregion
}