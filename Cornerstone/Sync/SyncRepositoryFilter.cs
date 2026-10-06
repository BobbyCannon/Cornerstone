#region References

using System;
using System.Linq.Expressions;
using Cornerstone.Collections;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a repository filter
/// </summary>
/// <typeparam name="T"> The type for the filter. </typeparam>
public class SyncRepositoryFilter<T> : SyncRepositoryFilter
{
	#region Fields

	private readonly Func<T, bool> _incomingCompiled;
	private readonly Func<T, bool> _scopeCompiled;

	#endregion

	#region Constructors

	/// <summary>
	/// Instantiates a repository filter.
	/// </summary>
	/// <param name="outgoingFilter"> Travel keep-test for GetChanges/GetCorrections. </param>
	/// <param name="incomingFilter"> Travel keep-test for ApplyChanges/ApplyCorrections. </param>
	/// <param name="lookupFilter"> Find this incoming row by a business key instead of SyncId. </param>
	/// <param name="skipDeletedItemsOnInitialSync"> Skip SyncEntity.IsDeleted on the first GetChanges (since is DateTime.MinValue). </param>
	/// <param name="scopeFilter"> Store fence. ANDed into GetChanges and lookup. The stored row from the apply SyncId read is tested in memory. The incoming image is not. </param>
	/// <param name="orderBy"> Optional GetChanges order, applied before ModifiedOn then Id. Hosts use this so a parent row is sent before its children. </param>
	public SyncRepositoryFilter(
		Expression<Func<T, bool>> outgoingFilter = null,
		Expression<Func<T, bool>> incomingFilter = null,
		Func<T, Expression<Func<T, bool>>> lookupFilter = null,
		bool skipDeletedItemsOnInitialSync = true,
		Expression<Func<T, bool>> scopeFilter = null,
		params OrderBy<T>[] orderBy
	) : base(typeof(T).ToAssemblyName(), outgoingFilter, incomingFilter,
		lookupFilter, skipDeletedItemsOnInitialSync, orderBy, scopeFilter)
	{
		_incomingCompiled = incomingFilter?.Compile();
		_scopeCompiled = scopeFilter?.Compile();
	}

	#endregion

	#region Properties

	public override bool HasLookupFilter => LookupFilter != null;

	/// <summary>
	/// The incoming filter for the type.
	/// </summary>
	public Expression<Func<T, bool>> IncomingFilter => IncomingExpression as Expression<Func<T, bool>>;

	/// <summary>
	/// The lookup expression for the type
	/// </summary>
	public Func<T, Expression<Func<T, bool>>> LookupFilter => LookupExpression as Func<T, Expression<Func<T, bool>>>;

	/// <summary>
	/// Optional order by values for outgoing changes.
	/// </summary>
	public OrderBy<T>[] OrderBys => OrderBy as OrderBy<T>[] ?? [];

	/// <summary>
	/// The outgoing filter for the type.
	/// </summary>
	public Expression<Func<T, bool>> OutgoingFilter => OutgoingExpression as Expression<Func<T, bool>>;

	/// <summary>
	/// The scope filter for the type.
	/// </summary>
	public Expression<Func<T, bool>> ScopeFilter => ScopeExpression as Expression<Func<T, bool>>;

	#endregion

	#region Methods

	/// <summary>
	/// True when this stored row is outside scope. A null scope passes.
	/// </summary>
	public override bool FailsScope(object entity)
	{
		if ((_scopeCompiled == null) || (entity is not T tEntity))
		{
			return false;
		}

		return !_scopeCompiled.Invoke(tEntity);
	}

	/// <summary>
	/// True when apply should skip this entity (fails the incoming keep-test).
	/// </summary>
	public override bool ShouldFilterIncomingEntity(object entity)
	{
		if (entity is not T tEntity)
		{
			return false;
		}

		return !(_incomingCompiled?.Invoke(tEntity) ?? true);
	}

	#endregion
}

/// <summary>
/// Represents a repository filter
/// </summary>
public abstract class SyncRepositoryFilter
{
	#region Constructors

	/// <summary>
	/// Instantiates a repository filter.
	/// </summary>
	/// <param name="type"> The type this filter is for. </param>
	/// <param name="outgoingFilter"> The outgoing filter for the type. </param>
	/// <param name="incomingFilter"> The incoming filter for the type. </param>
	/// <param name="lookupFilter"> The lookup filter for the type. </param>
	/// <param name="skipDeletedItemsOnInitialSync"> The option to skipped SyncEntity.IsDeleted on initial sync. </param>
	/// <param name="orderBy"> An optional set of values to order by. </param>
	/// <param name="scopeFilter"> The scope filter for the type. </param>
	protected SyncRepositoryFilter(string type, object outgoingFilter, object incomingFilter, object lookupFilter,
		bool skipDeletedItemsOnInitialSync, object orderBy, object scopeFilter)
	{
		RepositoryType = type;
		OutgoingExpression = outgoingFilter;
		IncomingExpression = incomingFilter;
		LookupExpression = lookupFilter;
		SkipDeletedItemsOnInitialSync = skipDeletedItemsOnInitialSync;
		OrderBy = orderBy;
		ScopeExpression = scopeFilter;
	}

	#endregion

	#region Properties

	/// <summary>
	/// True when apply must run the incoming keep-test.
	/// </summary>
	public bool HasApplyKeepTest => HasIncomingFilter;

	/// <summary>
	/// Returns true if incoming expression is not null otherwise false.
	/// </summary>
	public virtual bool HasIncomingFilter => IncomingExpression != null;

	/// <summary>
	/// Returns true if lookup expression is not null otherwise false.
	/// </summary>
	public virtual bool HasLookupFilter => LookupExpression != null;

	/// <summary>
	/// Returns true if scope expression is not null otherwise false.
	/// </summary>
	public virtual bool HasScopeFilter => ScopeExpression != null;

	/// <summary>
	/// The incoming filter as a generic object.
	/// </summary>
	public object IncomingExpression { get; }

	/// <summary>
	/// The lookup filter as a generic object.
	/// </summary>
	public object LookupExpression { get; }

	/// <summary>
	/// An optional set of values to order by.
	/// </summary>
	public object OrderBy { get; }

	/// <summary>
	/// The outgoing filter as a generic object.
	/// </summary>
	public object OutgoingExpression { get; }

	/// <summary>
	/// The scope filter as a generic object.
	/// </summary>
	public object ScopeExpression { get; }

	/// <summary>
	/// The type contained in the repository.
	/// </summary>
	public string RepositoryType { get; }

	/// <summary>
	/// The option to skipped SyncEntity.IsDeleted on initial sync.
	/// </summary>
	public bool SkipDeletedItemsOnInitialSync { get; }

	#endregion

	#region Methods

	/// <summary>
	/// True when this stored row is outside the scope predicate.
	/// </summary>
	/// <param name="entity"> The stored row already loaded by SyncId. </param>
	/// <returns> True when the row is outside scope. </returns>
	public virtual bool FailsScope(object entity)
	{
		return false;
	}

	/// <summary>
	/// A test to validate if an incoming entity should be filtered.
	/// </summary>
	/// <param name="entity"> The entity to be tested. </param>
	/// <returns> True if apply should skip the entity. </returns>
	public abstract bool ShouldFilterIncomingEntity(object entity);

	#endregion
}