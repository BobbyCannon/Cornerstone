#region References

using System;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a provider to get a sync client.
/// </summary>
public class SyncClientProvider
{
	#region Fields

	private readonly Func<ISyncClient> _getClient;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a provider to get a sync client.
	/// </summary>
	public SyncClientProvider(Func<ISyncClient> getClient)
	{
		_getClient = getClient;
	}

	#endregion

	#region Methods

	/// <summary>
	/// Return a client by the provided name and credential.
	/// </summary>
	/// <returns> The sync client. </returns>
	public ISyncClient GetClient()
	{
		return _getClient.Invoke();
	}

	#endregion
}