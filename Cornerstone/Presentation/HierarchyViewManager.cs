#region References

using System;
using Cornerstone.Collections;
using Cornerstone.Compare;
using Cornerstone.Data;
using Cornerstone.Runtime;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Represents a manager of a set of views.
/// </summary>
public abstract partial class HierarchyViewManager<T>
	: ReadOnlySpeedyTree<T>, IManager
	where T : class, ISpeedyTree<T>, IHierarchySyncItem, IUpdateable
{
	#region Fields

	private readonly IDependencyProvider _dependencyProvider;

	#endregion

	#region Constructors

	protected HierarchyViewManager(
		IDateTimeProvider dateTimeProvider,
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher,
		Func<T, T, bool> distinctCheck,
		params OrderBy<T>[] orderBy
	) : base(new SpeedyTree<T>(null, orderBy) { DistinctCheck = new GenericEqualityComparer<T>(distinctCheck) })
	{
		_dependencyProvider = dependencyProvider;

		DateTimeProvider = dateTimeProvider;
		Dispatcher = dispatcher;
	}

	#endregion

	#region Properties

	public IDateTimeProvider DateTimeProvider { get; }

	public IDispatcher Dispatcher { get; }

	/// <summary>
	/// The last time this view was updated.
	/// </summary>
	public DateTime LastUpdated { get; set; }

	/// <summary>
	/// Gets the selected view.
	/// </summary>
	[Notify]
	public partial T SelectedView { get; set; }

	/// <summary>
	/// Predicate for removing views from collection
	/// </summary>
	protected virtual Func<T, bool> RemovePredicateByView => _ => false;

	#endregion

	#region Methods

	public T AddOrUpdate(T update)
	{
		var foundView = FirstOrDefaultDescendants(x => Tree.DistinctCheck.Equals(x, update));
		if (foundView == null)
		{
			foundView = update;
			UpdateView(foundView, update);
			var parent = LocateParent(update);
			parent.Add(update);
			OnViewUpdated(update);
			return update;
		}

		if (UpdateView(foundView, update))
		{
			OnViewUpdated(foundView);
		}
		return foundView;
	}

	public void Remove(T item)
	{
		var foundItem = FirstOrDefaultDescendants(x => Tree.DistinctCheck.Equals(x, item));
		foundItem.Parent.Children.Remove(foundItem);
	}

	public virtual void Reset()
	{
		Tree.Clear();

		SelectedView = null;
		LastUpdated = DateTime.MinValue;
	}

	public void Update()
	{
	}

	protected bool CheckIfManagerShouldRefresh(out DateTime until)
	{
		until = DateTimeProvider.UtcNow;
		return until > LastUpdated;
	}

	protected virtual T CreateView()
	{
		return _dependencyProvider.GetInstance<T>();
	}

	protected IPresentationList<T> LocateParent(IHierarchySyncItem update)
	{
		var foundParent = FirstOrDefaultDescendants(x => x.SyncId == update.ParentSyncId);
		return foundParent?.Children ?? Tree.Children;
	}

	protected virtual void OnViewUpdated(T view)
	{
		ViewUpdated?.Invoke(this, view);
	}

	protected virtual bool UpdateView(T view, object update)
	{
		if ((view == null) || (update == null))
		{
			return false;
		}
		view.UpdateWith(update);
		return true;
	}

	#endregion

	#region Events

	public event EventHandler<T> ViewUpdated;

	#endregion
}