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
/// <remarks>
/// Pack properties 1-4 are in use.
/// </remarks>
[SourceReflection]
public abstract partial class SyncModel
	: Entity, ISyncEntity, IPackable
{
	#region Properties

	[Notify]
	[Pack(1, 1)]
	[UpdateableAction(UpdateableAction.All)]
	public partial DateTime CreatedOn { get; set; }

	[Notify]
	[Pack(1, 2)]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool IsDeleted { get; set; }

	[Notify]
	[Pack(1, 3)]
	[UpdateableAction(UpdateableAction.All)]
	public partial DateTime ModifiedOn { get; set; }

	[Notify]
	[Pack(1, 4)]
	[UpdateableAction(UpdateableAction.All)]
	public partial Guid SyncId { get; set; }

	#endregion
}