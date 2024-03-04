#region References

using System;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// The details of the sync session
/// </summary>
public class SyncSessionStart
{
	#region Properties

	/// <summary>
	/// The ID of the session
	/// </summary>
	public Guid Id { get; set; }

	/// <summary>
	/// The date and time the sync was started on the server.
	/// </summary>
	public DateTime StartedOn { get; set; }

	#endregion
}