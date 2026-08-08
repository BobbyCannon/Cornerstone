#region References

using System;

#endregion

namespace Cornerstone.Sync;

public interface IHierarchySyncItem : ISyncEntityId
{
	#region Properties

	bool IsParent { get; set; }

	int Order { get; set; }

	Guid? ParentSyncId { get; set; }

	#endregion
}