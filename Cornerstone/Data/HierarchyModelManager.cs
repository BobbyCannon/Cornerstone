#region References

using System;
using Cornerstone.Collections;
using Cornerstone.Compare;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Manages a hierarchy of state models.
/// </summary>
public abstract partial class HierarchyModelManager<T>
	: ReadOnlySpeedyTree<T>
	where T : class, ISpeedyTree<T>, IHierarchySyncItem, IUpdateable
{
	#region Fields

	private readonly IDependencyProvider _dependencyProvider;

	#endregion

	#region Constructors

	protected HierarchyModelManager(
		IDateTimeProvider dateTimeProvider,
		IDependencyProvider dependencyProvider,
		Func<T, T, bool> distinctCheck,
		params OrderBy<T>[] orderBy
	) : base(new SpeedyTree<T>(null, orderBy) { DistinctCheck = new GenericEqualityComparer<T>(distinctCheck) })
	{
		_dependencyProvider = dependencyProvider;
		DateTimeProvider = dateTimeProvider;
	}

	#endregion

	#region Properties

	public IDateTimeProvider DateTimeProvider { get; }

	public DateTime LastUpdated { get; set; }

	[Notify]
	public partial T SelectedItem { get; set; }

	protected virtual Func<T, bool> RemovePredicateByModel => _ => false;

	#endregion

	#region Methods

	public T AddOrUpdate(T update)
	{
		var foundModel = FirstOrDefaultDescendants(x => Tree.DistinctCheck.Equals(x, update));
		if (foundModel == null)
		{
			foundModel = update;
			UpdateModel(foundModel, update);
			var parent = LocateParent(update);
			parent.Add(update);
			OnModelUpdated(update);
			return update;
		}

		if (UpdateModel(foundModel, update))
		{
			OnModelUpdated(foundModel);
		}
		return foundModel;
	}

	public void Remove(T item)
	{
		var foundItem = FirstOrDefaultDescendants(x => Tree.DistinctCheck.Equals(x, item));
		foundItem.Parent.Children.Remove(foundItem);
	}

	public virtual void Reset()
	{
		Tree.Clear();
		SelectedItem = null;
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

	protected virtual T CreateModel()
	{
		return _dependencyProvider.GetInstance<T>();
	}

	protected IPresentationList<T> LocateParent(IHierarchySyncItem update)
	{
		var foundParent = FirstOrDefaultDescendants(x => x.SyncId == update.ParentSyncId);
		return foundParent?.Children ?? Tree.Children;
	}

	protected virtual void OnModelUpdated(T model)
	{
		ModelUpdated?.Invoke(this, model);
	}

	protected virtual bool UpdateModel(T model, object update)
	{
		if ((model == null) || (update == null))
		{
			return false;
		}
		model.UpdateWith(update);
		return true;
	}

	#endregion

	#region Events

	public event EventHandler<T> ModelUpdated;

	#endregion
}
