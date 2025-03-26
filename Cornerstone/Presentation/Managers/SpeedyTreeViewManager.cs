#region References

using System;
using Cornerstone.Collections;
using Cornerstone.Runtime;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Presentation.Managers;

/// <summary>
/// Represents a manager of a set of views.
/// </summary>
public abstract class SpeedyTreeViewManager<TModel, TEntity, TEntityKey>
	: ViewManager<TModel, TEntity, TEntityKey>
	where TModel : SpeedyTree<TModel>
	where TEntity : SyncEntity<TEntityKey>
{
	#region Constructors

	/// <inheritdoc />
	protected SpeedyTreeViewManager(
		IDateTimeProvider dateTimeProvider,
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher,
		Func<TModel, TModel, bool> distinctCheck,
		params OrderBy<TModel>[] orderBy)
		: base(dateTimeProvider, dependencyProvider, dispatcher, distinctCheck, orderBy)
	{
	}

	#endregion

	#region Properties

	protected override Func<TModel, TEntity, bool> LookupPredicate => (m, e) => m.SyncId == e.SyncId;

	#endregion

	#region Methods

	public bool AnyDescendants(Func<TModel, bool> check)
	{
		foreach (var item in this)
		{
			if (item.AnyDescendants(check))
			{
				return true;
			}
		}

		return false;
	}

	public override TModel FirstOrDefault(Func<TModel, bool> check)
	{
		try
		{
			EnterReadLock();

			foreach (var item in this)
			{
				var response = item.FirstOrDefaultDescendants(check);
				if (response != null)
				{
					return response;
				}
			}

			return null;
		}
		finally
		{
			ExitReadLock();
		}
	}

	#endregion
}