#region References

using System;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Extensions;

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
	/// Gets or sets the list of entities to ignore during maintenance updates.
	/// </summary>
	public Type[] UnmaintainedEntities { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Update the DatabaseOptions with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	public override bool UpdateWith(DatabaseSettings update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			DisableEntityValidations = update.DisableEntityValidations;
			MaintainCreatedOn = update.MaintainCreatedOn;
			MaintainModifiedOn = update.MaintainModifiedOn;
			MaintainSyncId = update.MaintainSyncId;
			PermanentSyncEntityDeletions = update.PermanentSyncEntityDeletions;
			UnmaintainedEntities = update.UnmaintainedEntities.ToArray();
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(DisableEntityValidations)), x => x.DisableEntityValidations = update.DisableEntityValidations);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(MaintainCreatedOn)), x => x.MaintainCreatedOn = update.MaintainCreatedOn);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(MaintainModifiedOn)), x => x.MaintainModifiedOn = update.MaintainModifiedOn);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(MaintainSyncId)), x => x.MaintainSyncId = update.MaintainSyncId);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(PermanentSyncEntityDeletions)), x => x.PermanentSyncEntityDeletions = update.PermanentSyncEntityDeletions);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(UnmaintainedEntities)), x => x.UnmaintainedEntities = update.UnmaintainedEntities);
		}

		return true;
	}

	#endregion
}