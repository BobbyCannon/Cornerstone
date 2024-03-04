#region References

using System.Collections.Generic;
using Cornerstone.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Shared.Sync;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class SyncModelTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AccountSync()
	{
		var scenarios = new Dictionary<UpdateableAction, string[]>
		{
			{ UpdateableAction.SyncIncomingAdd, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.SyncIncomingModified, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.SyncOutgoing, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.PartialUpdate, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.PropertyChangeTracking, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.UnwrapProxyEntity, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.Updateable, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.Unknown, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] }
		};

		ValidateGetDefaultIncludedProperties<AccountSync>(scenarios);
	}

	[TestMethod]
	public void AddressSync()
	{
		var scenarios = new Dictionary<UpdateableAction, string[]>
		{
			{ UpdateableAction.SyncIncomingAdd, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.SyncIncomingModified, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.SyncOutgoing, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.PartialUpdate, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.PropertyChangeTracking, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.UnwrapProxyEntity, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.Updateable, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.Unknown, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] }
		};

		ValidateGetDefaultIncludedProperties<AddressSync>(scenarios);
	}

	#endregion
}