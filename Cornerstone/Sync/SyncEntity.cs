#region References

using System;
using Cornerstone.Data;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represent an entity that can be synced.
/// </summary>
/// <typeparam name="TKey"> The type of the entity primary ID. </typeparam>
[Updateable]
public abstract partial class SyncEntity<TKey> : Entity<TKey>, ISyncEntity
{
	#region Properties

	[UpdateableAction(UpdateableAction.EverythingExceptSyncUpdate)]
	public DateTime CreatedOn { get; set; }

	[Column(IsAutoIncrement = true, IsPrimaryKey = true)]
	[UpdateableAction(UpdateableAction.EverythingExceptSync)]
	public sealed override TKey Id { get; set; }

	[UpdateableAction(UpdateableAction.All)]
	public bool IsDeleted { get; set; }

	[UpdateableAction(UpdateableAction.EverythingExceptSyncUpdate)]
	public DateTime ModifiedOn { get; set; }

	[Column(IsUnique = true)]
	[UpdateableAction(UpdateableAction.EverythingExceptSyncUpdate)]
	public Guid SyncId { get; set; }

	#endregion
}

/// <summary>
/// Represent an entity that can be synced.
/// </summary>
public interface ISyncEntity : IModifiableEntity, ISyncEntityId
{
	#region Properties

	/// <summary>
	/// Used to communicate if the sync entity is deleted.
	/// </summary>
	public bool IsDeleted { get; set; }

	#endregion
}

/// <summary>
/// Represent an entity that can be synced.
/// </summary>
public interface ISyncEntityId
{
	#region Properties

	/// <summary>
	/// The ID of the sync entity.
	/// </summary>
	/// <remarks>
	/// This ID must be globally unique. Never reuse GUIDs.
	/// </remarks>
	public Guid SyncId { get; set; }

	#endregion
}