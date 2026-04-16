#region References

using Cornerstone.Data;
using Cornerstone.Storage;
using Cornerstone.Storage.Sql;
using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represent an entity that can be synced.
/// </summary>
/// <typeparam name="TKey"> The type of the entity primary ID. </typeparam>
[Notifiable([nameof(CreatedOn), nameof(IsDeleted), nameof(ModifiedOn), nameof(SyncId)])]
[Updateable(UpdateableAction.All, [nameof(IsDeleted)])]
[Updateable(UpdateableAction.EverythingExceptSync, [nameof(Id)])]
[Updateable(UpdateableAction.EverythingExceptSyncUpdate, [nameof(CreatedOn), nameof(ModifiedOn)])]
public abstract partial class SyncEntity<TKey> : Entity<TKey>, ISyncEntity
{
	#region Properties

	public partial DateTime CreatedOn { get; set; }

	[SqlTableColumn(IsAutoIncrement = true, IsPrimaryKey = true)]
	public sealed override TKey Id
	{
		get;
		set
		{
			if (!EqualityComparer<TKey>.Default.Equals(field, value))
			{
				field = value;
				OnPropertyChanged();
			}
		}
	}

	public partial bool IsDeleted { get; set; }

	public partial DateTime ModifiedOn { get; set; }

	[SqlTableColumn(IsUnique = true)]
	public partial Guid SyncId { get; set; }

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