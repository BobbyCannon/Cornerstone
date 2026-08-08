#region References

using System;
using Cornerstone.Web;

#endregion

namespace Cornerstone.Sync;

public class SyncClientStub : SyncClient
{
	#region Constructors

	public SyncClientStub()
		: base(null, null, null, null)
	{
	}

	#endregion

	#region Methods

	public override ServiceResult<SyncIssue> ApplyChanges(Guid sessionId, ServiceRequest<SyncObject> changes)
	{
		throw new NotImplementedException();
	}

	public override ServiceResult<SyncIssue> ApplyCorrections(Guid sessionId, ServiceRequest<SyncObject> corrections)
	{
		throw new NotImplementedException();
	}

	public override ServiceResult<SyncObject> GetChanges(Guid sessionId, SyncRequest request)
	{
		throw new NotImplementedException();
	}

	public override ServiceResult<SyncObject> GetCorrections(Guid sessionId, ServiceRequest<SyncIssue> issues)
	{
		throw new NotImplementedException();
	}

	protected override SyncClientConverter GetConverter()
	{
		return null;
	}

	protected override void UpdateSyncSettings()
	{
	}

	#endregion
}