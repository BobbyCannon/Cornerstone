#region References

using System.Collections.Generic;
using Cornerstone.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Shared.Storage.Sync;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class SyncModelTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Account()
	{
		var scenarios = new Dictionary<UpdateableAction, string[]>
		{
			{ UpdateableAction.SyncIncomingAdd, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.SyncIncomingUpdate, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.SyncOutgoing, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.PartialUpdate, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.PropertyChangeTracking, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.UnwrapProxyEntity, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] },
			{ UpdateableAction.Updateable, ["AddressSyncId", "CreatedOn", "EmailAddress", "IsDeleted", "ModifiedOn", "Name", "Roles", "SyncId"] }
		};

		ValidateGetDefaultIncludedProperties<Account>(scenarios);
	}

	[TestMethod]
	public void Address()
	{
		var scenarios = new Dictionary<UpdateableAction, string[]>
		{
			{ UpdateableAction.SyncIncomingAdd, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.SyncIncomingUpdate, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.SyncOutgoing, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.PartialUpdate, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.PropertyChangeTracking, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.UnwrapProxyEntity, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] },
			{ UpdateableAction.Updateable, ["City", "CreatedOn", "IsDeleted", "Line1", "Line2", "ModifiedOn", "Postal", "State", "SyncId"] }
		};

		ValidateGetDefaultIncludedProperties<Address>(scenarios);
	}

	[TestMethod]
	public void LogEventSync()
	{
		var scenarios = new Dictionary<UpdateableAction, string[]>
		{
			{ UpdateableAction.SyncIncomingAdd, ["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"] },
			{ UpdateableAction.SyncIncomingUpdate, ["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"] },
			{ UpdateableAction.SyncOutgoing, ["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"] },
			{ UpdateableAction.UnwrapProxyEntity, ["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"] },
			{ UpdateableAction.Updateable, ["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"] },
			{ UpdateableAction.PropertyChangeTracking, ["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"] },
			{ UpdateableAction.PartialUpdate, ["CreatedOn", "IsDeleted", "Level", "Message", "ModifiedOn", "SyncId"] }
		};

		ValidateGetDefaultIncludedProperties<LogEvent>(scenarios);
	}

	[TestMethod]
	public void SettingSync()
	{
		var scenarios = new Dictionary<UpdateableAction, string[]>
		{
			{ UpdateableAction.SyncIncomingAdd, ["CreatedOn", "IsDeleted", "ModifiedOn", "Name", "SyncId", "Value"] },
			{ UpdateableAction.SyncIncomingUpdate, ["CreatedOn", "IsDeleted", "ModifiedOn", "Name", "SyncId", "Value"] },
			{ UpdateableAction.SyncOutgoing, ["CreatedOn", "IsDeleted", "ModifiedOn", "Name", "SyncId", "Value"] },
			{ UpdateableAction.UnwrapProxyEntity, ["CreatedOn", "IsDeleted", "ModifiedOn", "Name", "SyncId", "Value"] },
			{ UpdateableAction.Updateable, ["CreatedOn", "IsDeleted", "ModifiedOn", "Name", "SyncId", "Value"] },
			{ UpdateableAction.PropertyChangeTracking, ["CreatedOn", "IsDeleted", "ModifiedOn", "Name", "SyncId", "Value"] },
			{ UpdateableAction.PartialUpdate, ["CreatedOn", "IsDeleted", "ModifiedOn", "Name", "SyncId", "Value"] }
		};

		ValidateGetDefaultIncludedProperties<Setting>(scenarios);
	}

	#endregion
}