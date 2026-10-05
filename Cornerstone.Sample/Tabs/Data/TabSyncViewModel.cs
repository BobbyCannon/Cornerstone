#region References

using System;
using Cornerstone.Compare;
using Cornerstone.Data;
using Cornerstone.Diagnostics;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Keystone;
using Cornerstone.Sample.Keystone.State;
using Cornerstone.Sample.Storage;

#endregion

namespace Cornerstone.Sample.Tabs.Data;

/// <summary>
/// Projects SyncFeatureState onto the tab. The view binds these properties only, not State.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[DependencyInjected]
public partial class TabSyncViewModel : DispatchableViewModel
{
	#region Fields

	private static readonly GenericEqualityComparer<ProfilerScopeModel> _scopeComparer;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public TabSyncViewModel(AppBus bus, AppState state)
	{
		Bus = bus;
		ClientApplyChangesData = new SeriesDataProvider(30);
		ClientGetChangesData = new SeriesDataProvider(30);
		ClientProcessSyncObjectData = new SeriesDataProvider(30);
		ClientProfilerScopes = [];
		ClientSaveChangesData = new SeriesDataProvider(30);
		DatabaseKinds =
		[
			SampleSyncDatabaseKind.SqliteMapper,
			SampleSyncDatabaseKind.EfSqlite,
			SampleSyncDatabaseKind.SqlServerMapper,
			SampleSyncDatabaseKind.EfSqlServer
		];
		ServerApplyChangesData = new SeriesDataProvider(30);
		ServerGetChangesData = new SeriesDataProvider(30);
		ServerProcessSyncObjectData = new SeriesDataProvider(30);
		ServerProfilerScopes = [];
		ServerSaveChangesData = new SeriesDataProvider(30);
		WorkSides = [SampleSyncWorkSide.Client, SampleSyncWorkSide.Server];

		TrackProperties(state.Sync)
			.MapTwoWay(nameof(SyncFeatureState.ClientDatabaseKind))
			.MapTwoWay(nameof(SyncFeatureState.ServerDatabaseKind))
			.MapTwoWay(nameof(SyncFeatureState.InjectCount))
			.MapTwoWay(nameof(SyncFeatureState.ModifyCount))
			.MapTwoWay(nameof(SyncFeatureState.WorkSide))
			.MapOneWay(nameof(SyncFeatureState.CanRunWork))
			.MapOneWay(nameof(SyncFeatureState.ClientAccountCount))
			.MapOneWay(nameof(SyncFeatureState.ClientAddressCount))
			.MapOneWay(nameof(SyncFeatureState.IsRunning))
			.MapOneWay(nameof(SyncFeatureState.LastElapsed))
			.MapOneWay(nameof(SyncFeatureState.LastStatus))
			.MapOneWay(nameof(SyncFeatureState.LastSyncedOn))
			.MapOneWay(nameof(SyncFeatureState.ServerAccountCount))
			.MapOneWay(nameof(SyncFeatureState.ServerAddressCount));

		TrackCollection(state.Sync.ClientProfilerScopes, ClientProfilerScopes, _scopeComparer, CollectionReconcileMode.ListAndItems);
		TrackCollection(state.Sync.ServerProfilerScopes, ServerProfilerScopes, _scopeComparer, CollectionReconcileMode.ListAndItems);
		TrackSeries(state.Sync.ClientSaveChangesData, ClientSaveChangesData);
		TrackSeries(state.Sync.ClientProcessSyncObjectData, ClientProcessSyncObjectData);
		TrackSeries(state.Sync.ClientGetChangesData, ClientGetChangesData);
		TrackSeries(state.Sync.ClientApplyChangesData, ClientApplyChangesData);
		TrackSeries(state.Sync.ServerSaveChangesData, ServerSaveChangesData);
		TrackSeries(state.Sync.ServerProcessSyncObjectData, ServerProcessSyncObjectData);
		TrackSeries(state.Sync.ServerGetChangesData, ServerGetChangesData);
		TrackSeries(state.Sync.ServerApplyChangesData, ServerApplyChangesData);
	}

	static TabSyncViewModel()
	{
		_scopeComparer = new((x, y) => (x != null) && (y != null) && (x.Name == y.Name), x => x.Name?.GetHashCode() ?? 0);
	}

	#endregion

	#region Properties

	public AppBus Bus { get; }

	public partial bool CanRunWork { get; set; }

	public partial int ClientAccountCount { get; set; }

	public partial int ClientAddressCount { get; set; }

	public SeriesDataProvider ClientApplyChangesData { get; }

	public partial SampleSyncDatabaseKind ClientDatabaseKind { get; set; }

	public SeriesDataProvider ClientGetChangesData { get; }

	public SeriesDataProvider ClientProcessSyncObjectData { get; }

	public PresentationList<ProfilerScopeModel> ClientProfilerScopes { get; }

	public SeriesDataProvider ClientSaveChangesData { get; }

	public SampleSyncDatabaseKind[] DatabaseKinds { get; }

	public partial int InjectCount { get; set; }

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

	public PresentationList<ProfilerScopeModel> ServerProfilerScopes { get; }

	public SeriesDataProvider ServerSaveChangesData { get; }

	public partial SampleSyncWorkSide WorkSide { get; set; }

	public SampleSyncWorkSide[] WorkSides { get; }

	#endregion
}