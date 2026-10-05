namespace Cornerstone.Sync;

/// <summary>
/// Represents a server proxy to communicate between a sync client and a sync engine.
/// </summary>
public interface ISyncServerProxy
{
	#region Methods

	/// <summary>
	/// Run one sync round-trip. Session id is on the operation, not the URL.
	/// </summary>
	SyncOperationResult Sync(SyncOperation operation);

	#endregion
}