#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Compare;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Manages a set of state models projected from storage entities.
/// </summary>
public abstract class ModelManager<TModel, TEntity, TEntityKey>
	: ModelManager<TModel>
	where TModel : class, IUpdateable, new()
	where TEntity : SyncEntity<TEntityKey>
{
	#region Constructors

	protected ModelManager(
		IDateTimeProvider dateTimeProvider,
		IDependencyProvider dependencyProvider,
		Func<TModel, TModel, bool> distinctCheck,
		params OrderBy<TModel>[] orderBy
	) : base(dateTimeProvider, dependencyProvider, distinctCheck, orderBy)
	{
	}

	#endregion

	#region Properties

	protected abstract Func<TModel, TEntity, bool> LookupPredicate { get; }

	protected virtual Func<TEntity, bool> RemovePredicateByEntity => x => x.IsDeleted;

	protected virtual Func<TEntity, bool> UpdatePredicate => x => !x.IsDeleted;

	#endregion

	#region Methods

	/// <summary>
	/// Add or update the model from the entity.
	/// </summary>
	public virtual TModel AddOrUpdate(TEntity value)
	{
		var foundModel = FirstOrDefault(x => LookupPredicate.Invoke(x, value));

		if (foundModel == null)
		{
			foundModel = CreateModel();
			UpdateModel(foundModel, value);
			List.Add(foundModel);
			OnModelUpdated(foundModel);
			return foundModel;
		}

		if (UpdateModel(foundModel, value))
		{
			OnModelUpdated(foundModel);
		}
		return foundModel;
	}

	public virtual IEnumerable<TModel> AddOrUpdate(params TEntity[] updates)
	{
		return List.ProcessThenOrder(() =>
		{
			RemoveModels();

			updates
				.Where(RemovePredicateByEntity)
				.ForEach(x => List.Remove(v => LookupPredicate(v, x)));

			var updatedModels = updates
				.Where(UpdatePredicate)
				.Select(AddOrUpdate)
				.ToList();

			return updatedModels;
		});
	}

	public override bool Remove(TModel item)
	{
		return List.Remove(item);
	}

	protected virtual TModel Convert(TEntity entity)
	{
		var model = CreateModel();
		UpdateModel(model, entity);
		return model;
	}

	protected virtual bool UpdateModel(TModel model, TEntity update)
	{
		return base.UpdateModel(model, update);
	}

	protected sealed override bool UpdateModel(TModel model, object update)
	{
		if (update is TEntity entity)
		{
			return UpdateModel(model, entity);
		}

		return base.UpdateModel(model, update);
	}

	#endregion
}

/// <summary>
/// Manages a set of state models.
/// </summary>
public abstract partial class ModelManager<T>
	: ReadOnlyPresentationList<T>
	where T : class, IUpdateable, new()
{
	#region Constructors

	protected ModelManager(
		IDateTimeProvider dateTimeProvider,
		IDependencyProvider dependencyProvider,
		Func<T, T, bool> distinctCheck,
		params OrderBy<T>[] orderBy
	) : base(new PresentationList<T>(null, orderBy) { DistinctCheck = new GenericEqualityComparer<T>(distinctCheck) })
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

	[Notify]
	public partial DateTime LastUpdated { get; protected set; }

	[Notify]
	public partial T SelectedItem { get; set; }

	[Notify]
	public partial string FilterInput { get; set; }

	protected virtual Func<T, bool> RemovePredicateByModel => _ => false;

	#endregion

	#region Methods

	public T AddOrUpdate(T update)
	{
		var foundModel = FirstOrDefault(x => List.DistinctCheck.Equals(x, update));
		if (foundModel == null)
		{
			foundModel = update;
			UpdateModel(foundModel, update);
			List.Add(update);
			OnModelUpdated(update);
			return update;
		}

		if (UpdateModel(foundModel, update))
		{
			OnModelUpdated(foundModel);
		}
		return foundModel;
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
		List.FilterCheck = MatchesFilter;
		List.ListUpdated += ListOnListUpdated;
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
		SelectedItem = null;
		LastUpdated = DateTime.MinValue;
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
		List.ListUpdated -= ListOnListUpdated;
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
		var foundModel = List.FirstOrDefault(filter);
		return foundModel != null;
	}

	protected virtual T CreateModel()
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
		if (propertyName == nameof(FilterInput))
		{
			List.RefreshFilter();
		}

		base.OnPropertyChanged(propertyName, oldValue, newValue);
	}

	protected virtual void OnModelUpdated(T model)
	{
		List.RefreshFilter();
		List.RefreshOrder();
		ModelUpdated?.Invoke(this, model);
	}

	protected void RemoveModels()
	{
		var itemsToRemove = List.Where(RemovePredicateByModel).ToList();
		if (itemsToRemove.Count <= 0)
		{
			return;
		}

		itemsToRemove.ForEach(x => List.Remove(x));
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

	protected virtual bool MatchesFilter(T item)
	{
		return true;
	}

	private void ItemBeingEditedOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		BeginNewItemCommand?.Refresh();
		SaveEditItemCommand?.Refresh();
		CancelEditItemCommand?.Refresh();
	}

	private void ListOnListUpdated(object sender, PresentationListUpdatedEventArg<T> e)
	{
		OnListUpdated(e);
	}

	#endregion

	#region Events

	public event EventHandler<T> ModelUpdated;

	#endregion
}
