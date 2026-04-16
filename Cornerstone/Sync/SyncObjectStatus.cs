namespace Cornerstone.Sync;

/// <summary>
/// Represents the sync state of an entity
/// </summary>
public enum SyncObjectStatus
{
	/// <summary>
	/// This entity was added.
	/// </summary>
	Added = 0,

	/// <summary>
	/// This entity was updated.
	/// </summary>
	Updated = 1,

	/// <summary>
	/// This entity was deleted.
	/// </summary>
	Deleted = 2
}