#region References

using System;
using Cornerstone.Web;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// A single sync round-trip: begin (if needed), apply client changes, get server changes, end (if complete).
/// Session id is in this payload, not the URL.
/// </summary>
public class SyncOperation
{
	#region Constructors

	/// <summary>
	/// Instantiates a sync operation.
	/// </summary>
	public SyncOperation()
	{
		Changes = new ServiceRequest<SyncObject>();
		Issues = new ServiceRequest<SyncIssue>();
		Settings = new SyncSettings();
		SessionId = Guid.Empty;
		GetChangesSkip = 0;
		EndSession = false;
		ClientHasNoChanges = false;
		ResumeStatistics = null;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Client changes to apply on the server. Empty for an idle heartbeat.
	/// </summary>
	public ServiceRequest<SyncObject> Changes { get; set; }

	/// <summary>
	/// When true, the server ends the session on this call. An end-only call does not query.
	/// </summary>
	public bool EndSession { get; set; }

	/// <summary>
	/// The client will not push after this pull. Pull-only sets this even when the client database has rows.
	/// The server then ends the session on the last pull page, instead of waiting for a later end call.
	/// </summary>
	public bool ClientHasNoChanges { get; set; }

	/// <summary>
	/// Number of changes already returned. GetChanges continues from this Skip across repositories.
	/// </summary>
	public int GetChangesSkip { get; set; }

	/// <summary>
	/// Optional issues for a correction round.
	/// </summary>
	public ServiceRequest<SyncIssue> Issues { get; set; }

	/// <summary>
	/// Statistics to restore after BeginSync on a continued session.
	/// </summary>
	public SyncStatistics ResumeStatistics { get; set; }

	/// <summary>
	/// The sync session id.
	/// </summary>
	public Guid SessionId { get; set; }

	/// <summary>
	/// Settings for the first call of a session. Server sanitizes these.
	/// </summary>
	public SyncSettings Settings { get; set; }

	#endregion
}