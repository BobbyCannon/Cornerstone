#region References

using System;
using Cornerstone.Sync;
using Cornerstone.Web;

#endregion

namespace Cornerstone.Sample.Sync;

/// <summary>
/// In-process IWebClient that posts SyncOperation into SampleServerSyncClient.
/// Same WebSyncClient.Sync hop as HTTP api/Sync; no JSON (Sample is AOT).
/// </summary>
public class SampleWebClient : WebClientStub
{
	#region Fields

	private readonly SampleServerSyncClient _serverClient;

	#endregion

	#region Constructors

	public SampleWebClient(SampleServerSyncClient serverClient)
	{
		_serverClient = serverClient;
	}

	#endregion

	#region Methods

	public override TResult Post<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null)
	{
		if (content is not SyncOperation operation)
		{
			throw new NotSupportedException("Sample loopback web client only posts SyncOperation.");
		}

		var result = _serverClient.Sync(operation);
		if (result is TResult typed)
		{
			return typed;
		}

		throw new InvalidOperationException("Sample loopback expected SyncOperationResult.");
	}

	#endregion
}