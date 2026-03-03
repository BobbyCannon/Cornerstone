#region References

using System;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Represents a type of update.
/// </summary>
/// <remarks>
/// ****** NOTE ******
/// If this changes be sure to update the Cornerstone.Generators
/// </remarks>
[Flags]
[SourceReflection]
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
	/// An incoming new or update sync object.
	/// </summary>
	SyncIncomingAddOrUpdate = 0b0000_0011,

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
	/// A general update for IUpdateable.
	/// </summary>
	Updateable = 0b0001_0000,

	/// <summary>
	/// Update for property change tracking.
	/// </summary>
	PropertyChangeTracking = 0b0010_0000,

	/// <summary>
	/// Action for a partial update.
	/// </summary>
	PartialUpdate = 0b0100_0000,

	/// <summary>
	/// Everything except sync add and update
	/// </summary>
	EverythingExceptSyncAddAndUpdate = 0b0111_1100,

	/// <summary>
	/// Everything except sync update
	/// </summary>
	EverythingExceptSyncUpdate = 0b0111_1101,

	/// <summary>
	/// Everything except sync
	/// </summary>
	EverythingExceptSync = 0b0111_1000,

	/// <summary>
	/// Action all update types
	/// </summary>
	All = 0b0111_1111
}