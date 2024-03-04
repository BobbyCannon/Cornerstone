namespace Cornerstone.Data;

/// <summary>
/// Represents a type of update.
/// </summary>
public enum UpdateableAction
{
	/// <summary>
	/// Unknown
	/// </summary>
	Unknown = 0,

	/// <summary>
	/// An incoming new sync object.
	/// </summary>
	SyncIncomingAdd = 1,

	/// <summary>
	/// An incoming modified sync object.
	/// </summary>
	SyncIncomingModified = 2,

	/// <summary>
	/// An outgoing sync object.
	/// </summary>
	SyncOutgoing = 3,

	/// <summary>
	/// Update for unwrapping entity proxies.
	/// </summary>
	UnwrapProxyEntity = 4,

	/// <summary>
	/// A general update for <see cref="IUpdateable" />
	/// </summary>
	Updateable = 5,

	/// <summary>
	/// Update for property change tracking. <see cref="INotifiable" />
	/// </summary>
	PropertyChangeTracking = 6,
	
	/// <summary>
	/// Action for a partial update. <see cref="Data.PartialUpdate"/>
	/// </summary>
	PartialUpdate = 7
}