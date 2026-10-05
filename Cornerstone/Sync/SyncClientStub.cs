#region References

using System;
using Cornerstone.Runtime;
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

	protected internal override ServiceResult<SyncIssue> ApplyChanges(Guid sessionId, ServiceRequest<SyncObject> changes)
	{
		return new ServiceResult<SyncIssue>();
	}

	protected internal override ServiceResult<SyncIssue> ApplyCorrections(Guid sessionId, ServiceRequest<SyncObject> corrections)
	{
		return new ServiceResult<SyncIssue>();
	}

	protected internal override ServiceResult<SyncObject> GetChanges(Guid sessionId, SyncRequest request)
	{
		return new ServiceResult<SyncObject>();
	}

	protected internal override ServiceResult<SyncObject> GetCorrections(Guid sessionId, ServiceRequest<SyncIssue> issues)
	{
		return new ServiceResult<SyncObject>();
	}

	protected override SyncClientConverter GetConverter()
	{
		return null;
	}

	protected override void SetSyncSettings()
	{
	}

	#endregion
}