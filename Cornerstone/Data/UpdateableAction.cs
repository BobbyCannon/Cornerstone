#region References

using System;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Represents a type of update.
/// </summary>
[Flags]
public enum UpdateableAction
{
	/// <summary>
	/// Unknown
	/// </summary>
	None = 0,

	/// <summary>
	/// An incoming new sync object.
	/// </summary>
	SyncIncomingAdd = 0b0000_0001,

	/// <summary>
	/// An incoming modified sync object.
	/// </summary>
	SyncIncomingUpdate = 0b0000_0010,

	/// <summary>
	/// An outgoing sync object.
	/// </summary>
	SyncOutgoing = 0b0000_0100,

	/// <summary>
	/// All sync flags.
	/// </summary>
	SyncAll = 0b0000_0111,

	/// <summary>
	/// Update for unwrapping entity proxies.
	/// </summary>
	UnwrapProxyEntity = 0b0000_1000,

	/// <summary>
	/// A general update for <see cref="IUpdateable" />
	/// </summary>
	Updateable = 0b0001_0000,

	/// <summary>
	/// Update for property change tracking. <see cref="INotifiable" />
	/// </summary>
	PropertyChangeTracking = 0b0010_0000,

	/// <summary>
	/// Action for a partial update. <see cref="Data.PartialUpdate" />
	/// </summary>
	PartialUpdate = 0b0100_0000,

	/// <summary>
	/// Action all update types
	/// </summary>
	All = 0b0111_1111
}