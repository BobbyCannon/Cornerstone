#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents options to be used during a sync.
/// </summary>
public class SyncSettings : Bindable<SyncSettings>
{
	#region Fields

	private readonly Dictionary<string, SyncRepositoryFilter> _filters;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes an instance of the class.
	/// </summary>
	public SyncSettings() : this(null)
	{
	}

	/// <summary>
	/// Initializes an instance of the class.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public SyncSettings(IDispatcher dispatcher) : base(dispatcher)
	{
		_filters = new Dictionary<string, SyncRepositoryFilter>();

		Reset();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Include the detail of the exception in the SyncIssue(s) returned.
	/// </summary>
	public bool IncludeIssueDetails { get; set; }

	/// <summary>
	/// Gets or sets the number of objects to be processed per sync request.
	/// </summary>
	public int ItemsPerSyncRequest { get; set; }

	/// <summary>
	/// Gets or sets the client was last synced on date and time.
	/// </summary>
	public DateTime LastSyncedOnClient { get; set; }

	/// <summary>
	/// Gets or sets the server was last synced on date and time.
	/// </summary>
	public DateTime LastSyncedOnServer { get; set; }

	/// <summary>
	/// If true the sync will actually delete entities marked for deletion. Defaults to false where IsDeleted will be marked "true".
	/// </summary>
	public bool PermanentDeletions { get; set; }

	/// <summary>
	/// The direction to sync.
	/// </summary>
	public SyncDirection SyncDirection { get; set; }

	/// <summary>
	/// The type of the sync.
	/// </summary>
	public string SyncType { get; set; }

	/// <summary>
	/// Additional values for synchronizing.
	/// </summary>
	public Dictionary<string, string> Values { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Adds a syncable filter to the options.
	/// </summary>
	public void AddSyncableFilter<T>(Expression<Func<T, bool>> outgoingFilter = null,
		Expression<Func<T, bool>> incomingFilter = null,
		Func<T, Expression<Func<T, bool>>> lookupFilter = null,
		bool skipDeletedItemsOnInitialSync = true)
	{
		AddSyncableFilter(new SyncRepositoryFilter<T>(outgoingFilter, incomingFilter, lookupFilter, skipDeletedItemsOnInitialSync));
	}

	/// <summary>
	/// Adds a syncable filter to the options.
	/// </summary>
	/// <param name="filter"> The syncable filter to be added. </param>
	public void AddSyncableFilter(SyncRepositoryFilter filter)
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
	/// Check to see if a repository has been excluded from syncing.
	/// </summary>
	/// <param name="type"> The type to check for. </param>
	/// <returns> True if the type is filter or false if otherwise. </returns>
	public bool ShouldExcludeRepository(Type type)
	{
		//
		// If we do not have a filter then consider the repository as excluded.
		//
		return ShouldExcludeRepository(type?.ToAssemblyName());
	}

	/// <summary>
	/// Check to see if a repository has been excluded from syncing.
	/// </summary>
	/// <param name="typeAssemblyName"> The type name to check for. Should be in assembly name format. </param>
	/// <returns> True if the type is filter or false if otherwise. </returns>
	public bool ShouldExcludeRepository(string typeAssemblyName)
	{
		//
		// If we do not have a filter then consider the repository as excluded.
		//
		return (_filters.Count > 0) && !_filters.ContainsKey(typeAssemblyName);
	}

	/// <summary>
	/// Update the SyncOptions with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	public override bool UpdateWith(SyncSettings update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			IncludeIssueDetails = update.IncludeIssueDetails;
			ItemsPerSyncRequest = update.ItemsPerSyncRequest;
			LastSyncedOnClient = update.LastSyncedOnClient;
			LastSyncedOnServer = update.LastSyncedOnServer;
			PermanentDeletions = update.PermanentDeletions;
			SyncDirection = update.SyncDirection;
			Values.Reconcile(update.Values);
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IncludeIssueDetails)), x => x.IncludeIssueDetails = update.IncludeIssueDetails);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(ItemsPerSyncRequest)), x => x.ItemsPerSyncRequest = update.ItemsPerSyncRequest);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(LastSyncedOnClient)), x => x.LastSyncedOnClient = update.LastSyncedOnClient);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(LastSyncedOnServer)), x => x.LastSyncedOnServer = update.LastSyncedOnServer);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(PermanentDeletions)), x => x.PermanentDeletions = update.PermanentDeletions);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(SyncDirection)), x => x.SyncDirection = update.SyncDirection);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Values)), x => x.Values.Reconcile(update.Values));
		}

		foreach (var lookup in update._filters)
		{
			AddSyncableFilter(lookup.Value);
		}

		return true;
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
		if (filter is not { HasIncomingFilter: true })
		{
			return false;
		}

		// Find the "ShouldFilterEntity" method, so we can invoke it
		var methods = filter.GetType().GetCachedMethods(BindingFlags.Public | BindingFlags.Instance);
		var method = methods.First(x => x.Name == nameof(ShouldFilterIncomingEntity));
		return (bool) method.Invoke(filter, [entity]);
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