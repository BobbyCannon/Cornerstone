#region References

using Cornerstone.Web;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Result of a single Sync operation.
/// </summary>
public class SyncOperationResult
{
	#region Constructors

	/// <summary>
	/// Instantiates a sync operation result.
	/// </summary>
	public SyncOperationResult()
	{
		AppliedIssues = new ServiceResult<SyncIssue>();
		Changes = new ServiceResult<SyncObject>();
		Corrections = new ServiceResult<SyncObject>();
		SessionEnded = false;
		SessionStart = new SyncSessionStart();
		Statistics = new SyncStatistics();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Issues from applying the request changes (or corrections).
	/// </summary>
	public ServiceResult<SyncIssue> AppliedIssues { get; set; }

	/// <summary>
	/// Server outgoing changes for this page.
	/// </summary>
	public ServiceResult<SyncObject> Changes { get; set; }

	/// <summary>
	/// Server corrections when the request included issues.
	/// </summary>
	public ServiceResult<SyncObject> Corrections { get; set; }

	/// <summary>
	/// True when the server ended the session on this call.
	/// </summary>
	public bool SessionEnded { get; set; }

	/// <summary>
	/// The session start for this sync.
	/// </summary>
	public SyncSessionStart SessionStart { get; set; }

	/// <summary>
	/// Statistics when the session ended; otherwise the in-progress totals.
	/// </summary>
	public SyncStatistics Statistics { get; set; }

	#endregion
}