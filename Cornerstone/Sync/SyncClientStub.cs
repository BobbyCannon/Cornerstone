namespace Cornerstone.Sync;

public class SyncClientStub : SyncClient
{
	#region Constructors

	public SyncClientStub()
		: base(null, null, null, null, null)
	{
	}

	#endregion

	#region Methods

	protected override SyncClientIncomingConverter GetIncomingConverter()
	{
		return null;
	}

	protected override SyncClientOutgoingConverter GetOutgoingConverter()
	{
		return null;
	}

	protected override void UpdateSyncSettings()
	{
	}

	#endregion
}