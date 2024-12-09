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
public abstract class HierarchyViewManager<T, TEntity, TEntityKey>
	: ViewManager<T, TEntity, TEntityKey>
	where T : HierarchyListItem<T>
	where TEntity : SyncEntity<TEntityKey>
{
	#region Constructors

	/// <inheritdoc />
	protected HierarchyViewManager(
		IDateTimeProvider dateTimeProvider,
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher,
		Func<T, T, bool> distinctCheck,
		params OrderBy<T>[] orderBy)
		: base(dateTimeProvider, dependencyProvider, dispatcher, distinctCheck, orderBy)
	{
	}

	#endregion

	#region Methods

	public bool AnyDescendants(Func<T, bool> check)
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

	public override T FirstOrDefault(Func<T, bool> check)
	{
		try
		{
			EnterReadLock();

			foreach (var item in this)
			{
				var response = item.FirstOrDefault(check);
				if (response != null)
				{
					return response;
				}
			}

			return default;
		}
		finally
		{
			ExitReadLock();
		}
	}

	#endregion
}