#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Compare;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Represents a manager of a set of views.
/// </summary>
public abstract class ViewManager<TView, TEntity, TEntityKey>
	: ViewManager<TView>
	where TView : class, IUpdateable, new()
	where TEntity : SyncEntity<TEntityKey>
{
	#region Constructors

	protected ViewManager(
		IDateTimeProvider dateTimeProvider,
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher,
		Func<TView, TView, bool> distinctCheck,
		params OrderBy<TView>[] orderBy
	) : base(dateTimeProvider, dependencyProvider, dispatcher, distinctCheck, orderBy)
	{
	}

	#endregion

	#region Properties

	protected abstract Func<TView, TEntity, bool> LookupPredicate { get; }

	protected virtual Func<TEntity, bool> RemovePredicateByEntity => x => x.IsDeleted;

	protected virtual Func<TEntity, bool> UpdatePredicate => x => !x.IsDeleted;

	#endregion

	#region Methods

	/// <summary>
	/// Add or update the view by using the entity.
	/// </summary>
	/// <param name="value"> The entity update. </param>
	/// <returns> The view that was added or updated. </returns>
	public virtual TView AddOrUpdate(TEntity value)
	{
		// Locate account view to update, or see if our account is a view,
		// or build a new account view from the account
		var foundView = FirstOrDefault(x => LookupPredicate.Invoke(x, value));

		if (foundView == null)
		{
			foundView = CreateView();
			UpdateView(foundView, value);
			List.Add(foundView);
			OnViewUpdated(foundView);
			return foundView;
		}

		if (UpdateView(foundView, value))
		{
			OnViewUpdated(foundView);
		}
		return foundView;
	}

	public virtual IEnumerable<TView> AddOrUpdate(params TEntity[] updates)
	{
		return List.ProcessThenOrder(() =>
		{
			// Remove view that should be removed
			RemoveViews();

			// Remove entities that should be removed
			updates
				.Where(RemovePredicateByEntity)
				.ForEach(x => List.Remove(v => LookupPredicate(v, x)));

			// Add or update new items
			var updatedViews = updates
				.Where(UpdatePredicate)
				.Select(AddOrUpdate)
				.ToList();

			return updatedViews;
		});
	}

	public override bool Remove(TView item)
	{
		return List.Remove(item);
	}

	protected virtual TView Convert(TEntity entity)
	{
		var view = CreateView();
		UpdateView(view, entity);
		return view;
	}

	protected virtual bool UpdateView(TView view, TEntity update)
	{
		return base.UpdateView(view, update);
	}

	/// <summary>
	/// Sealed to Hide / limit overriding of the generic "object" update.
	/// If you need a customer override then use the TEntity update override.
	/// </summary>
	protected sealed override bool UpdateView(TView view, object update)
	{
		if (update is TEntity entity)
		{
			return UpdateView(view, entity);
		}

		return base.UpdateView(view, update);
	}

	#endregion
}

/// <summary>
/// Represents a manager of a set of views.
/// </summary>
public abstract partial class ViewManager<T>
	: ReadOnlyPresentationList<T>, IManager
	where T : class, IUpdateable, new()
{
	#region Constructors

	protected ViewManager(
		IDateTimeProvider dateTimeProvider,
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher,
		Func<T, T, bool> distinctCheck,
		params OrderBy<T>[] orderBy
	) : base(new PresentationList<T>(dispatcher, orderBy) { DistinctCheck = new GenericEqualityComparer<T>(distinctCheck) })
	{
		DateTimeProvider = dateTimeProvider;
		DependencyProvider = dependencyProvider;
		ItemBeingEdited = new T();
	}

	#endregion

	#region Properties

	public IDateTimeProvider DateTimeProvider { get; }

	public IDependencyProvider DependencyProvider { get; }

	public T ItemBeingEdited { get; }

	/// <summary>
	/// The last time this view was updated.
	/// </summary>
	[Notify]
	public partial DateTime LastUpdated { get; protected set; }

	/// <summary>
	/// Gets the selected view.
	/// </summary>
	[Notify]
	public partial T SelectedView { get; set; }

	[Notify]
	public partial string ViewFilterInput { get; set; }

	/// <summary>
	/// Predicate for removing views from collection
	/// </summary>
	protected virtual Func<T, bool> RemovePredicateByView => _ => false;

	#endregion

	#region Methods

	/// <summary>
	/// NOTE: Be careful when using this because it does not perform as well as "AddOrUpdateViews"
	/// </summary>
	/// <param name="update"> The update. </param>
	/// <returns> </returns>
	public T AddOrUpdate(T update)
	{
		var foundView = FirstOrDefault(x => List.DistinctCheck.Equals(x, update));
		if (foundView == null)
		{
			foundView = update;
			UpdateView(foundView, update);
			List.Add(update);
			OnViewUpdated(update);
			return update;
		}

		if (UpdateView(foundView, update))
		{
			OnViewUpdated(foundView);
		}
		return foundView;
	}

	[RelayCommand]
	public void BeginEditItem(object value)
	{
		if (value is not T itemToEdit)
		{
			return;
		}

		ItemBeingEdited.UpdateWith(itemToEdit, UpdateableAction.Updateable);
		OnBeginEditItem();
		SaveEditItemCommand?.Refresh();
	}

	[RelayCommand]
	public void BeginNewItem()
	{
		if (ItemBeingEdited is ISyncEntity syncEntity
			&& (syncEntity.SyncId == Guid.Empty))
		{
			syncEntity.SyncId = Guid.NewGuid();
		}
		OnBeginEditItem();
		SaveEditItemCommand?.Refresh();
	}

	public virtual bool CanSaveEditItem()
	{
		return true;
	}

	[RelayCommand]
	public void CancelEditItem()
	{
		var emptyState = new T();
		ItemBeingEdited.UpdateWith(emptyState);
		(ItemBeingEdited as ITrackPropertyChanges)?.ResetHasChanges();
		DisposableExtensions.TryDispose(emptyState);
		OnCancelEditItem();
	}

	public override void Clear()
	{
		CancelEditItem();

		List.Clear();
		LastUpdated = DateTime.MinValue;
	}

	public virtual T FirstOrDefault(Func<T, bool> check)
	{
		return List.FirstOrDefault(check);
	}

	public override bool HasChanges(IncludeExcludeSettings settings)
	{
		return List.HasChanges(settings)
			|| base.HasChanges(settings);
	}

	public override void InitializeLifecycle()
	{
		List.FilterCheck = ViewFilterCheck;
		if (ItemBeingEdited is INotifyPropertyChanged npc)
		{
			npc.PropertyChanged += ItemBeingEditedOnPropertyChanged;
		}
		base.InitializeLifecycle();
	}

	public override bool Remove(T item)
	{
		return List.Remove(item);
	}

	public virtual void Reset()
	{
		Clear();

		this.Dispatch(() =>
		{
			SelectedView = null;
			LastUpdated = DateTime.MinValue;
		});
	}

	public override void ResetHasChanges()
	{
		List.ResetHasChanges();
		base.ResetHasChanges();
	}

	[RelayCommand(CanExecuteMethod = nameof(CanSaveEditItem))]
	public virtual void SaveEditItem()
	{
		CancelEditItem();
	}

	public override void UninitializeLifecycle()
	{
		List.FilterCheck = null;
		if (ItemBeingEdited is INotifyPropertyChanged npc)
		{
			npc.PropertyChanged -= ItemBeingEditedOnPropertyChanged;
		}
		base.UninitializeLifecycle();
	}

	public virtual void Update()
	{
	}

	protected bool CheckIfManagerShouldRefresh(out DateTime until)
	{
		until = DateTimeProvider.UtcNow;
		return until > LastUpdated;
	}

	protected bool Contains(Func<T, bool> filter)
	{
		var foundView = List.FirstOrDefault(filter);
		return foundView != null;
	}

	protected virtual T CreateView()
	{
		return DependencyProvider.GetInstance<T>();
	}

	protected virtual void OnBeginEditItem()
	{
	}

	protected virtual void OnCancelEditItem()
	{
	}

	protected virtual void OnListUpdated(PresentationListUpdatedEventArg<T> e)
	{
	}

	protected override void OnPropertyChanged<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		if (propertyName == nameof(ViewFilterInput))
		{
			List.RefreshFilter();
		}

		base.OnPropertyChanged(propertyName, oldValue, newValue);
	}

	protected virtual void OnViewUpdated(T view)
	{
		List.RefreshFilter();
		List.RefreshOrder();
		ViewUpdated?.Invoke(this, view);
	}

	protected void RemoveViews()
	{
		var itemsToRemove = List.Where(RemovePredicateByView).ToList();
		if (itemsToRemove.Count <= 0)
		{
			return;
		}

		itemsToRemove.ForEach(x => List.Remove(x));
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

	protected virtual bool ViewFilterCheck(T account)
	{
		return true;
	}

	private void ItemBeingEditedOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		BeginNewItemCommand?.Refresh();
		SaveEditItemCommand?.Refresh();
		CancelEditItemCommand?.Refresh();
	}

	#endregion

	#region Events

	public event EventHandler<T> ViewUpdated;

	#endregion
}