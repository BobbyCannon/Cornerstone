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
			{ UpdateableAction.SyncIncomingUpdate, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.SyncOutgoing, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.SyncAll, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.PartialUpdate, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.PropertyChangeTracking, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.UnwrapProxyEntity, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.Updateable, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.None, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.All, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] }
		};

		ValidateGetDefaultIncludedProperties<AccountSync>(scenarios);
	}

	[TestMethod]
	public void AddressSync()
	{
		var scenarios = new Dictionary<UpdateableAction, string[]>
		{
			{ UpdateableAction.SyncIncomingAdd, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.SyncIncomingUpdate, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.SyncOutgoing, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.SyncAll, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.PartialUpdate, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.PropertyChangeTracking, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.UnwrapProxyEntity, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.Updateable, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.None, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.All, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] }
		};

		ValidateGetDefaultIncludedProperties<AddressSync>(scenarios);
	}
	
	[TestMethod]
	public void LogEventSync()
	{
		var scenarios = new Dictionary<UpdateableAction, string[]>
		{
			{ UpdateableAction.None, ["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"] },
			{ UpdateableAction.SyncIncomingAdd, ["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"] },
			{ UpdateableAction.SyncIncomingUpdate, ["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"] },
			{ UpdateableAction.SyncOutgoing, ["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"] },
			{ UpdateableAction.SyncAll, ["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"] },
			{ UpdateableAction.UnwrapProxyEntity, ["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"] },
			{ UpdateableAction.Updateable, ["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"] },
			{ UpdateableAction.PropertyChangeTracking, ["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"] },
			{ UpdateableAction.PartialUpdate, ["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"] },
			{ UpdateableAction.All, ["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"] }
		};

		ValidateGetDefaultIncludedProperties<LogEventSync>(scenarios);
	}

	#endregion
}