﻿#region References

using System;
using System.Collections.Concurrent;
using Cornerstone.Net;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sync;
using Microsoft.AspNetCore.Mvc;
using Sample.Server.Data;
using Sample.Server.Website.Sync;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Server;

#endregion

namespace Sample.Server.Website.WebApi;

/// <summary>
/// Any entity that gets modified during the life cycle of the controller will never get synced. Syncing is based on time and the client will request a sync
/// of the server based on the time in which it started. Meaning if a controller touches a syncable entity before allowing the sync to occur will basically
/// ensure that the modified entity will never sync.
/// An example: When a user is authenticated we update the LastLoginDate on the UserEntity. Previously this would also update the ModifiedOn because the entity
/// was technically modified. The problem is that then that UserEntity would never sync as a Personnel model.
/// </summary>
[Route("api/Sync")]
public class SyncController : BaseController, ISyncServerProxy
{
	#region Fields

	private static readonly ConcurrentDictionary<Guid, ServerSyncClient> _sessions;

	#endregion

	#region Constructors

	public SyncController(ServerDatabaseProvider provider, IDateTimeProvider dateTimeProvider)
		: base(provider.GetDatabase())
	{
		DatabaseProvider = provider;
		DateTimeProvider = dateTimeProvider;
	}

	static SyncController()
	{
		_sessions = new ConcurrentDictionary<Guid, ServerSyncClient>();
	}

	#endregion

	#region Properties

	public ServerDatabaseProvider DatabaseProvider { get; }

	public IDateTimeProvider DateTimeProvider { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	[HttpPost]
	[Route("ApplyChanges/{sessionId}")]
	public ServiceResult<SyncIssue> ApplyChanges(Guid sessionId, [FromBody] ServiceRequest<SyncObject> changes)
	{
		var client = GetSyncClient(sessionId, GetSyncingAccount());
		var issues = client.ApplyChanges(sessionId, changes);
		return issues;
	}

	/// <inheritdoc />
	[HttpPost]
	[Route("ApplyCorrections/{sessionId}")]
	public ServiceResult<SyncIssue> ApplyCorrections(Guid sessionId, [FromBody] ServiceRequest<SyncObject> corrections)
	{
		var client = GetSyncClient(sessionId, GetSyncingAccount());
		var issues = client.ApplyCorrections(sessionId, corrections);
		return issues;
	}

	/// <inheritdoc />
	[HttpPost]
	[Route("BeginSync/{sessionId}")]
	public SyncSessionStart BeginSync(Guid sessionId, [FromBody] SyncSettings settings)
	{
		var name = $"Server: {sessionId}";
		var syncClient = new SampleServerSyncClient(name, GetSyncingAccount(), DatabaseProvider,
			DateTimeProvider, new SyncStatistics(), new Profiler(name)
		);

		// The server should always maintain dates as they are the "Master" dataset
		if (!_sessions.TryAdd(sessionId, syncClient))
		{
			throw new InvalidOperationException(Constants.SyncSessionAlreadyActive);
		}

		var session = syncClient.BeginSync(sessionId, settings);
		return session;
	}

	/// <inheritdoc />
	[HttpPost]
	[Route("EndSync/{sessionId}")]
	public SyncStatistics EndSync(Guid sessionId)
	{
		// The server should always maintain dates as they are the "Master" dataset
		if (!_sessions.TryRemove(sessionId, out var syncClient))
		{
			throw new InvalidOperationException(Constants.SyncSessionAlreadyActive);
		}

		var statistics = syncClient.EndSync(sessionId);
		return statistics;
	}

	/// <inheritdoc />
	[HttpPost]
	[Route("GetChanges/{sessionId}")]
	public ServiceResult<SyncObject> GetChanges(Guid sessionId, [FromBody] SyncRequest request)
	{
		var client = GetSyncClient(sessionId, GetSyncingAccount());
		var changes = client.GetChanges(sessionId, request);
		return changes;
	}

	/// <inheritdoc />
	[HttpPost]
	[Route("GetCorrections/{sessionId}")]
	public ServiceResult<SyncObject> GetCorrections(Guid sessionId, [FromBody] ServiceRequest<SyncIssue> issues)
	{
		var client = GetSyncClient(sessionId, GetSyncingAccount());
		var corrections = client.GetCorrections(sessionId, issues);
		return corrections;
	}

	public AccountEntity GetSyncingAccount()
	{
		return GetAuthenticatedAccount(x => new AccountEntity { Id = x.Id, Roles = x.Roles, SyncId = x.SyncId }, false);
	}

	private ServerSyncClient GetSyncClient(Guid sessionId, AccountEntity account)
	{
		if (!_sessions.TryGetValue(sessionId, out var syncClient))
		{
			throw new Exception("Could not find the sync session.");
		}

		return syncClient;
	}

	#endregion
}