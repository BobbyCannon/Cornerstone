#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents settings to be used during a sync.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class SyncSettings : CornerstoneObject<SyncSettings>
{
	#region Fields

	private readonly Dictionary<string, SyncRepositoryFilter> _filters;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes an instance of the class.
	/// </summary>
	public SyncSettings()
	{
		_filters = new Dictionary<string, SyncRepositoryFilter>();

		Reset();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Include the detail of the exception in the SyncIssue(s) returned.
	/// </summary>
	public partial bool IncludeIssueDetails { get; set; }

	/// <summary>
	/// Gets or sets the number of objects to be processed per sync request.
	/// </summary>
	public partial int ItemsPerSyncRequest { get; set; }

	/// <summary>
	/// Gets or sets the client was last sync was attempted.
	/// </summary>
	public partial DateTime LastSyncAttemptedOn { get; set; }

	/// <summary>
	/// Gets or sets the client was last synced on date and time.
	/// </summary>
	public partial DateTime LastSyncedOnClient { get; set; }

	/// <summary>
	/// Gets or sets the server was last synced on date and time.
	/// </summary>
	public partial DateTime LastSyncedOnServer { get; set; }

	/// <summary>
	/// If true the sync will actually delete entities marked for deletion. Defaults to false where IsDeleted will be marked "true".
	/// </summary>
	public partial bool PermanentDeletions { get; set; }

	/// <summary>
	/// The direction to sync.
	/// </summary>
	public partial SyncDirection SyncDirection { get; set; }

	/// <summary>
	/// The type of the sync.
	/// </summary>
	public partial string SyncType { get; set; }

	/// <summary>
	/// Additional values for synchronizing.
	/// </summary>
	public partial Dictionary<string, string> Values { get; set; }

	/// <summary>
	/// True when at least one repository filter is registered. An empty set syncs nothing.
	/// </summary>
	public bool HasFilters => _filters.Count > 0;

	#endregion

	#region Methods

	/// <summary>
	/// Adds a syncable filter to the options.
	/// </summary>
	/// <param name="scopeFilter"> Ownership keep-test ANDed into GetChanges, apply, lookup, and related *SyncId checks. </param>
	/// <param name="lookupFilter"> Find this incoming time by a business key instead of SyncId. </param>
	/// <param name="incomingFilter"> Travel keep-test for apply. </param>
	/// <param name="outgoingFilter"> Travel keep-test for GetChanges. </param>
	/// <param name="skipDeletedItemsOnInitialSync"> Skip tombstones on the first GetChanges. </param>
	/// <param name="orderBy"> Optional order for host queries. GetChanges order is ModifiedOn then Id. </param>
	public void AddFilter<T>(
		Expression<Func<T, bool>> scopeFilter = null,
		Func<T, Expression<Func<T, bool>>> lookupFilter = null,
		Expression<Func<T, bool>> incomingFilter = null,
		Expression<Func<T, bool>> outgoingFilter = null,
		bool skipDeletedItemsOnInitialSync = true,
		params OrderBy<T>[] orderBy)
	{
		AddFilter(new SyncRepositoryFilter<T>(outgoingFilter, incomingFilter, lookupFilter, skipDeletedItemsOnInitialSync, scopeFilter, orderBy));
	}

	/// <summary>
	/// Adds a syncable filter to the options.
	/// </summary>
	/// <param name="filter"> The syncable filter to be added. </param>
	public void AddFilter(SyncRepositoryFilter filter)
	{
		if (_filters.ContainsKey(filter.RepositoryType))
		{
			// Update an existing filter
			_filters[filter.RepositoryType] = filter;
			return;
		}

		// Add a new filter.
		_filters.Add(filter.RepositoryType, filter);
	}

	/// <summary>
	/// Resets the sync options.
	/// </summary>
	public void Reset()
	{
		LastSyncedOnClient = DateTime.MinValue;
		LastSyncedOnServer = DateTime.MinValue;
		ItemsPerSyncRequest = 10000;
		SyncDirection = SyncDirection.PullDownThenPushUp;
		Values ??= new();
		Values.Clear();

		ResetFilters();
	}

	/// <summary>
	/// Resets the syncable filters
	/// </summary>
	public void ResetFilters()
	{
		_filters.Clear();
	}

	/// <summary>
	/// True when a filter is registered for this entity type.
	/// </summary>
	public bool HasFilter(Type type)
	{
		return HasFilter(type?.ToAssemblyName());
	}

	/// <summary>
	/// True when a filter is registered for this entity type assembly name.
	/// </summary>
	public bool HasFilter(string typeAssemblyName)
	{
		return (typeAssemblyName != null) && _filters.ContainsKey(typeAssemblyName);
	}

	/// <summary>
	/// True when this repository should be skipped. A type syncs only when a filter is registered for it.
	/// </summary>
	public bool ShouldExcludeRepository(Type type)
	{
		return ShouldExcludeRepository(type?.ToAssemblyName());
	}

	/// <summary>
	/// True when this repository should be skipped. A type syncs only when a filter is registered for it.
	/// </summary>
	public bool ShouldExcludeRepository(string typeAssemblyName)
	{
		return string.IsNullOrEmpty(typeAssemblyName) || !_filters.ContainsKey(typeAssemblyName);
	}

	/// <summary>
	/// Check to see if a repository has been included in syncing.
	/// </summary>
	/// <param name="type"> The type to check for. </param>
	/// <returns> True if the repository should sync. </returns>
	public bool ShouldSyncRepository(Type type)
	{
		return ShouldSyncRepository(type?.ToAssemblyName());
	}

	/// <summary>
	/// Check to see if a repository has been included in syncing.
	/// </summary>
	/// <param name="typeAssemblyName"> The type name to check for. Should be in assembly name format. </param>
	/// <returns> True if the repository should sync. </returns>
	public bool ShouldSyncRepository(string typeAssemblyName)
	{
		return !ShouldExcludeRepository(typeAssemblyName);
	}

	/// <summary>
	/// Find a filter for the provided repository.
	/// </summary>
	/// <param name="repository"> The repository to process. </param>
	/// <returns> The filter if found or null otherwise. </returns>
	internal SyncRepositoryFilter GetFilter(ISyncableRepository repository)
	{
		return GetFilter(repository?.TypeName);
	}

	/// <summary>
	/// Find the repository filter and check the entity to see if it should be filtered.
	/// </summary>
	/// <param name="typeAssemblyName"> The type of the entity in assembly format. </param>
	/// <param name="entity"> The entity to be tested. </param>
	/// <returns> True if the sync entity should be filtered or false if otherwise. </returns>
	internal bool ShouldFilterIncomingEntity(string typeAssemblyName, ISyncEntity entity)
	{
		var filter = GetFilter(typeAssemblyName);
		if (filter is not { HasApplyKeepTest: true })
		{
			return false;
		}

		// Find the "ShouldFilterEntity" method, so we can invoke it
		//var methods = filter.GetType().GetCachedMethods(BindingFlags.Public | BindingFlags.Instance);
		//var method = methods.First(x => x.Name == nameof(ShouldFilterIncomingEntity));
		//return (bool) method.Invoke(filter, [entity]);
		return filter.ShouldFilterIncomingEntity(entity);
	}

	/// <summary>
	/// Find a filter for the provided repository.
	/// </summary>
	/// <param name="typeAssemblyName"> The repository type assembly name to process. </param>
	/// <returns> The filter if found or null otherwise. </returns>
	[SuppressMessage("ReSharper", "CanSimplifyDictionaryTryGetValueWithGetValueOrDefault")]
	private SyncRepositoryFilter GetFilter(string typeAssemblyName)
	{
		return _filters.TryGetValue(typeAssemblyName, out var filter) ? filter : null;
	}

	#endregion
}