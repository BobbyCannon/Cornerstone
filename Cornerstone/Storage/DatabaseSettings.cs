#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Storage;

/// <summary>
/// Represents options for a Cornerstone database.
/// </summary>
[SourceReflection]
[Updateable(UpdateableAction.All, ["*"])]
public partial class DatabaseSettings : CornerstoneObject<DatabaseSettings>
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
	public (string entity, string sync)[] SyncOrder { get; set; }

	/// <summary>
	/// Gets or sets the list of entities to ignore during maintenance updates.
	/// </summary>
	public Type[] UnmaintainedEntities { get; set; }

	#endregion
}