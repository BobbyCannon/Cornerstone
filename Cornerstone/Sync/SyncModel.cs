#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a sync model for sync transfers.
/// </summary>
public class SyncModel : Entity, ISyncEntity
{
	#region Constructors

	protected SyncModel()
	{
		ResetHasChanges();
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public DateTime CreatedOn { get; set; }

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
		//
		// SyncModel: Always process all properties.
		//
		var response = base.GetDefaultIncludedProperties(action);
		var properties = GetRealType().GetCachedProperties();
		response.Add(properties.Select(x => x.Name));
		return response;
	}

	/// <inheritdoc />
	public override object GetEntityId()
	{
		return null;
	}

	/// <inheritdoc />
	public Guid GetEntitySyncId()
	{
		return SyncId;
	}

	/// <inheritdoc />
	public override bool IdIsSet()
	{
		return false;
	}

	public sealed override void ResetHasChanges()
	{
		base.ResetHasChanges();
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
	public override bool TrySetId(string id)
	{
		return false;
	}

	/// <inheritdoc />
	public bool UpdateSyncEntity(ISyncEntity update, UpdateableAction action)
	{
		return UpdateWith(update, action);
	}

	#endregion
}