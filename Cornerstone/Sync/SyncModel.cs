#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Serialization;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a sync model for sync transfers.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Packable(1, ["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public abstract partial class SyncModel
	: Entity, ISyncEntity, IPackable
{
	#region Properties

	public partial DateTime CreatedOn { get; set; }

	public partial bool IsDeleted { get; set; }

	public partial DateTime ModifiedOn { get; set; }

	public partial Guid SyncId { get; set; }

	#endregion
}