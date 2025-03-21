#region References

using System;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Storage;

/// <summary>
/// Represents options for a Cornerstone database.
/// </summary>
public class DatabaseSettings : Notifiable<DatabaseSettings>
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of the database options class.
	/// </summary>
	public DatabaseSettings()
	{
		DisableEntityValidations = false;
		MaintainCreatedOn = true;
		MaintainModifiedOn = true;
		MaintainSyncId = true;
		PermanentSyncEntityDeletions = false;
		UnmaintainedEntities = [];
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the flag to disable entity validations.
	/// </summary>
	public bool DisableEntityValidations { get; set; }

	/// <summary>
	/// Gets or sets the flag to manage the optional CreatedOn property.
	/// </summary>
	public bool MaintainCreatedOn { get; set; }

	/// <summary>
	/// Gets or sets the flag to manage the optional ModifiedOn properties.
	/// </summary>
	public bool MaintainModifiedOn { get; set; }

	/// <summary>
	/// Gets or sets the flag to manage the sync ID for sync entities.
	/// </summary>
	public bool MaintainSyncId { get; set; }

	/// <summary>
	/// If true the sync entities will actually delete entities marked for deletion. Defaults to false where IsDeleted will be marked "true".
	/// </summary>
	/// todo: update saving of modified entities to ignore changes to deleted sync entities?
	public bool PermanentSyncEntityDeletions { get; set; }

	/// <summary>
	/// Gets or sets the sync order of the syncable repositories.
	/// </summary>
	public string[] SyncOrder { get; set; }

	/// <summary>
	/// Gets or sets the list of entities to ignore during maintenance updates.
	/// </summary>
	public Type[] UnmaintainedEntities { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Update the DatabaseSettings with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The settings for controlling the updating of the entity. </param>
	public override bool UpdateWith(DatabaseSettings update, IncludeExcludeSettings settings)
	{
		// Code Generated - UpdateWith

		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** This code has been auto generated, do not edit this. ******

		UpdateProperty(DisableEntityValidations, update.DisableEntityValidations, settings.ShouldProcessProperty(nameof(DisableEntityValidations)), x => DisableEntityValidations = x);
		UpdateProperty(MaintainCreatedOn, update.MaintainCreatedOn, settings.ShouldProcessProperty(nameof(MaintainCreatedOn)), x => MaintainCreatedOn = x);
		UpdateProperty(MaintainModifiedOn, update.MaintainModifiedOn, settings.ShouldProcessProperty(nameof(MaintainModifiedOn)), x => MaintainModifiedOn = x);
		UpdateProperty(MaintainSyncId, update.MaintainSyncId, settings.ShouldProcessProperty(nameof(MaintainSyncId)), x => MaintainSyncId = x);
		UpdateProperty(PermanentSyncEntityDeletions, update.PermanentSyncEntityDeletions, settings.ShouldProcessProperty(nameof(PermanentSyncEntityDeletions)), x => PermanentSyncEntityDeletions = x);
		UpdateProperty(SyncOrder, update.SyncOrder, settings.ShouldProcessProperty(nameof(SyncOrder)), x => SyncOrder = x);
		UpdateProperty(UnmaintainedEntities, update.UnmaintainedEntities, settings.ShouldProcessProperty(nameof(UnmaintainedEntities)), x => UnmaintainedEntities = x);

		// Code Generated - /UpdateWith

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			DatabaseSettings value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}


	#endregion
}