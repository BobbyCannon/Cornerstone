﻿#region References

using System;
using System.Collections.Generic;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represent an entity that can be synced.
/// </summary>
/// <typeparam name="TKey"> The type of the entity primary ID. </typeparam>
public class SyncEntity<TKey> : Entity<TKey>, ISyncEntity
{
	#region Properties

	/// <inheritdoc />
	public DateTime CreatedOn { get; set; }

	/// <inheritdoc />
	public override TKey Id { get; set; }

	/// <inheritdoc />
	public bool IsDeleted { get; set; }

	/// <inheritdoc />
	public DateTime ModifiedOn { get; set; }

	/// <inheritdoc />
	public Guid SyncId { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override HashSet<string> GetDefaultIncludedProperties(UpdateableAction action)
	{
		var response = base.GetDefaultIncludedProperties(action);

		switch (action)
		{
			case UpdateableAction.SyncIncomingModified:
			{
				response.AddRange(nameof(CreatedOn), nameof(IsDeleted), nameof(ModifiedOn));
				break;
			}
			case UpdateableAction.SyncIncomingAdd:
			case UpdateableAction.SyncOutgoing:
			{
				response.AddRange(nameof(CreatedOn), nameof(IsDeleted), nameof(ModifiedOn), nameof(SyncId));
				break;
			}
		}

		return response;
	}

	/// <inheritdoc />
	public Guid GetEntitySyncId()
	{
		return SyncId;
	}

	/// <inheritdoc />
	public void SetEntitySyncId(Guid syncId)
	{
		SyncId = syncId;
	}

	/// <inheritdoc />
	public SyncObject ToSyncObject()
	{
		return SyncObject.ToSyncObject(this);
	}

	/// <inheritdoc />
	public virtual bool UpdateSyncEntity(ISyncEntity update, UpdateableAction action)
	{
		return UpdateWith(update, GetUpdateableOptions(action));
	}

	#endregion
}

/// <summary>
/// Represent an entity that can be synced.
/// </summary>
public interface ISyncEntity : IModifiableEntity
{
	#region Properties

	/// <summary>
	/// Used to communicate if the sync entity is deleted.
	/// </summary>
	bool IsDeleted { get; set; }

	/// <summary>
	/// The ID of the sync entity.
	/// </summary>
	/// <remarks>
	/// This ID must be globally unique. Never reuse GUIDs.
	/// </remarks>
	Guid SyncId { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets the sync key (ID) of the sync entity. Defaults to SyncId.
	/// This can be overriden by setting the LookupFilter for a sync repository filter.
	/// </summary>
	/// <returns> The sync key value for the sync entity. </returns>
	Guid GetEntitySyncId();

	/// <summary>
	/// Gets the sync key (ID) of the sync entity. Defaults to SyncId.
	/// This can be overriden by setting the LookupFilter for a sync repository filter.
	/// </summary>
	/// <param name="syncId"> The sync key value for the sync entity. </param>
	void SetEntitySyncId(Guid syncId);

	/// <summary>
	/// Converts the entity into an object to transmit.
	/// </summary>
	/// <returns> The sync object for this entity. </returns>
	SyncObject ToSyncObject();

	/// <summary>
	/// Updates the entity with the provided entity for the type of action.
	/// </summary>
	/// <param name="update"> The source of the update. </param>
	/// <param name="action"> The action of the type of update. </param>
	bool UpdateSyncEntity(ISyncEntity update, UpdateableAction action);

	#endregion
}