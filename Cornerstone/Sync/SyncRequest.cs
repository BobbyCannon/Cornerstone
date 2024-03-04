#region References

using System;
using Cornerstone.Net;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// The details to ask a sync client for changes.
/// </summary>
public class SyncRequest : ServiceRequest<SyncObject>
{
	#region Properties

	/// <summary>
	/// The start date and time to get changes for.
	/// </summary>
	public DateTime Since { get; set; }

	/// <summary>
	/// The end date and time to get changes for.
	/// </summary>
	public DateTime Until { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Resets the filter back to defaults.
	/// </summary>
	public void Reset(DateTime utcNow)
	{
		Since = DateTime.MinValue;
		Until = utcNow;
	}

	#endregion
}