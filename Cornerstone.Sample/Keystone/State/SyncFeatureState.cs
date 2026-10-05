#region References

using System;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Diagnostics;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Storage;

#endregion

namespace Cornerstone.Sample.Keystone.State;

[SourceReflection]
[Notifiable(["*"])]
[DependencyInjected]
public partial class SyncFeatureState : CornerstoneObject
{
	#region Constructors

	[DependencyInjectionConstructor]
	public SyncFeatureState()
	{
		CanRunWork = false;
		ClientAccountCount = 0;
		ClientAddressCount = 0;
		ClientApplyChangesData = new SeriesDataProvider(30);
		ClientDatabaseKind = SampleSyncDatabaseKind.SqliteMapper;
		ClientGetChangesData = new SeriesDataProvider(30);
		ClientProcessSyncObjectData = new SeriesDataProvider(30);
		ClientProfilerScopes = new SpeedyList<ProfilerScopeModel>(32, true);
		ClientSaveChangesData = new SeriesDataProvider(30);
		DatabaseKinds =
		[
			SampleSyncDatabaseKind.SqliteMapper,
			SampleSyncDatabaseKind.EfSqlite,
			SampleSyncDatabaseKind.SqlServerMapper,
			SampleSyncDatabaseKind.EfSqlServer
		];
		InjectCount = 10000;
		IsAvailable = false;
		IsRunning = false;
		LastElapsed = TimeSpan.Zero;
		LastStatus = "Not started.";
		LastSyncedOn = DateTime.MinValue;
		ModifyCount = 1000;
		ServerAccountCount = 0;
		ServerAddressCount = 0;
		ServerApplyChangesData = new SeriesDataProvider(30);
		ServerDatabaseKind = SampleSyncDatabaseKind.SqliteMapper;
		ServerGetChangesData = new SeriesDataProvider(30);
		ServerProcessSyncObjectData = new SeriesDataProvider(30);
		ServerProfilerScopes = new SpeedyList<ProfilerScopeModel>(32, true);
		ServerSaveChangesData = new SeriesDataProvider(30);
		WorkSides = [SampleSyncWorkSide.Client, SampleSyncWorkSide.Server];
		WorkSide = SampleSyncWorkSide.Client;
	}

	#endregion

	#region Properties

	public partial bool CanRunWork { get; set; }

	public partial int ClientAccountCount { get; set; }

	public partial int ClientAddressCount { get; set; }

	public SeriesDataProvider ClientApplyChangesData { get; }

	public partial SampleSyncDatabaseKind ClientDatabaseKind { get; set; }

	public SeriesDataProvider ClientGetChangesData { get; }

	public SeriesDataProvider ClientProcessSyncObjectData { get; }

	public SpeedyList<ProfilerScopeModel> ClientProfilerScopes { get; }

	public SeriesDataProvider ClientSaveChangesData { get; }

	public SampleSyncDatabaseKind[] DatabaseKinds { get; }

	public partial int InjectCount { get; set; }

	public partial bool IsAvailable { get; set; }

	public partial bool IsRunning { get; set; }

	public partial TimeSpan LastElapsed { get; set; }

	public partial string LastStatus { get; set; }

	public partial DateTime LastSyncedOn { get; set; }

	public partial int ModifyCount { get; set; }

	public partial int ServerAccountCount { get; set; }

	public partial int ServerAddressCount { get; set; }

	public SeriesDataProvider ServerApplyChangesData { get; }

	public partial SampleSyncDatabaseKind ServerDatabaseKind { get; set; }

	public SeriesDataProvider ServerGetChangesData { get; }

	public SeriesDataProvider ServerProcessSyncObjectData { get; }

	public SpeedyList<ProfilerScopeModel> ServerProfilerScopes { get; }

	public SeriesDataProvider ServerSaveChangesData { get; }

	public partial SampleSyncWorkSide WorkSide { get; set; }

	public SampleSyncWorkSide[] WorkSides { get; }

	#endregion
}

public enum SampleSyncWorkSide
{
	Client,
	Server
}
