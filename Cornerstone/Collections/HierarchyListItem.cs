﻿#region References

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Collections;

/// <summary>
/// Represents a sync model in a hierarchy list.
/// </summary>
public class HierarchyListItem : Bindable, IHierarchyListItem
{
	#region Fields

	private ISpeedyList _children;
	private IHierarchyListItem _parent;

	#endregion

	#region Constructors

	/// <inheritdoc />
	public HierarchyListItem(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public bool IsInitialized { get; private set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public IEnumerable<IHierarchyListItem> GetChildren()
	{
		if (_children != null)
		{
			return _children.Cast<IHierarchyListItem>();
		}

		return Array.Empty<IHierarchyListItem>();
	}

	/// <inheritdoc />
	public IHierarchyListItem GetParent()
	{
		return _parent;
	}

	/// <inheritdoc />
	public bool HasChildren()
	{
		return GetChildren().Any();
	}

	/// <inheritdoc />
	public void Initialize()
	{
		IsInitialized = true;
	}

	/// <inheritdoc />
	public virtual bool ShouldBeShown()
	{
		var parent = GetParent();
		return (parent == null)
			|| (parent.ShouldShowChild(this)
				&& parent.ShouldShowChildren());
	}

	/// <inheritdoc />
	public virtual bool ShouldShowChild(IHierarchyListItem child)
	{
		return ShouldShowChildren();
	}

	/// <inheritdoc />
	public virtual bool ShouldShowChildren()
	{
		return true;
	}

	/// <inheritdoc />
	public void Uninitialize()
	{
		DisconnectParentEventSubscriptions(_parent);
		DisconnectChildrenEventSubscriptions(_children);
		IsInitialized = false;
	}

	/// <summary>
	/// Trigger the ChildAdded event.
	/// </summary>
	/// <param name="e"> The child added. </param>
	protected virtual void OnChildAdded(IHierarchyListItem e)
	{
		ChildAdded?.Invoke(this, e);
	}

	/// <summary>
	/// Trigger the ChildRemoved event.
	/// </summary>
	/// <param name="e"> The child removed. </param>
	protected virtual void OnChildRemoved(IHierarchyListItem e)
	{
		ChildRemoved?.Invoke(this, e);
	}

	/// <summary>
	/// Trigger the ParentChanged event.
	/// </summary>
	/// <param name="e"> The parent that was assigned. </param>
	protected virtual void OnParentChanged(IHierarchyListItem e)
	{
		ParentChanged?.Invoke(this, e);
	}

	/// <summary>
	/// Trigger the ShouldBeShownChanged event.
	/// </summary>
	protected virtual void OnShouldBeShownChanged()
	{
		ShouldBeShownChanged?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Trigger the ShouldShowChildrenChanged event.
	/// </summary>
	protected virtual void OnShouldShowChildrenChanged()
	{
		ShouldShowChildrenChanged?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Initialize the relationship. This should be called in the constructor.
	/// </summary>
	/// <param name="children"> The children for the list item. </param>
	protected void UpdateChildren(ISpeedyList children)
	{
		if (children == _children)
		{
			return;
		}

		DisconnectChildrenEventSubscriptions(_children);
		_children = ConnectChildrenEvents(children);
	}

	/// <summary>
	/// Update the parent of this item.
	/// </summary>
	protected void UpdateParent(IHierarchyListItem parent)
	{
		if (parent == _parent)
		{
			return;
		}

		var oldParent = _parent;

		DisconnectParentEventSubscriptions(_parent);
		_parent = ConnectParentEvents(parent);

		OnParentChanged(oldParent);
	}

	private void ChildrenOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.Action == NotifyCollectionChangedAction.Move)
		{
			return;
		}

		if (e.OldItems != null)
		{
			foreach (var item in e.OldItems)
			{
				var listItem = (IHierarchyListItem) item;
				//listItem.CleanupEventSubscriptions();
				OnChildRemoved(listItem);
			}
		}

		if (e.NewItems != null)
		{
			foreach (var item in e.NewItems)
			{
				var listItem = (IHierarchyListItem) item;
				OnChildAdded(listItem);
			}
		}
	}

	private ISpeedyList ConnectChildrenEvents(ISpeedyList children)
	{
		if (children != null)
		{
			children.CollectionChanged += ChildrenOnCollectionChanged;
		}

		return children;
	}

	private IHierarchyListItem ConnectParentEvents(IHierarchyListItem parent)
	{
		if (parent != null)
		{
			parent.ShouldBeShownChanged += ParentOnShouldBeShownChanged;
			parent.ShouldShowChildrenChanged += ParentShouldShowChildrenChanged;
		}

		return parent;
	}

	private void DisconnectChildrenEventSubscriptions(ISpeedyList children)
	{
		if (children == null)
		{
			return;
		}

		children.CollectionChanged -= ChildrenOnCollectionChanged;
	}

	/// <inheritdoc />
	void IHierarchyListItem.DisconnectParent(IHierarchyListItem parent)
	{
		DisconnectParentEventSubscriptions(parent);

		if (_parent == parent)
		{
			_parent = null;
		}
	}

	private void DisconnectParentEventSubscriptions(IHierarchyListItem parent)
	{
		if (parent == null)
		{
			return;
		}

		parent.ShouldBeShownChanged -= ParentOnShouldBeShownChanged;
		parent.ShouldShowChildrenChanged -= ParentShouldShowChildrenChanged;
	}

	/// <inheritdoc />
	void IHierarchyListItem.OnShouldBeShownChanged()
	{
		OnShouldBeShownChanged();
	}

	/// <inheritdoc />
	void IHierarchyListItem.OnShouldShowChildrenChanged()
	{
		OnShouldShowChildrenChanged();
	}

	private void ParentOnShouldBeShownChanged(object sender, EventArgs e)
	{
		OnShouldBeShownChanged();
	}

	private void ParentShouldShowChildrenChanged(object sender, EventArgs e)
	{
		OnShouldBeShownChanged();
	}

	/// <inheritdoc />
	void IHierarchyListItem.RemoveChild(IHierarchyListItem child)
	{
		_children?.Remove(child);
	}

	/// <inheritdoc />
	void IHierarchyListItem.UpdateParent(IHierarchyListItem parent)
	{
		UpdateParent(parent);
	}

	#endregion

	#region Events

	/// <inheritdoc />
	public event EventHandler<IHierarchyListItem> ChildAdded;

	/// <inheritdoc />
	public event EventHandler<IHierarchyListItem> ChildRemoved;

	/// <inheritdoc />
	public event EventHandler<IHierarchyListItem> ParentChanged;

	/// <inheritdoc />
	public event EventHandler ShouldBeShownChanged;

	/// <inheritdoc />
	public event EventHandler ShouldShowChildrenChanged;

	#endregion
}

/// <summary>
/// Represents a hierarchy of data.
/// </summary>
public interface IHierarchyListItem : IViewModel
{
	#region Methods

	/// <summary>
	/// Get the children for the list item.
	/// </summary>
	/// <returns> </returns>
	IEnumerable<IHierarchyListItem> GetChildren();

	/// <summary>
	/// Get the parent of this item.
	/// </summary>
	/// <returns> </returns>
	IHierarchyListItem GetParent();

	/// <summary>
	/// Determine if the list item has a child collections.
	/// </summary>
	/// <returns> True if this list item has children. </returns>
	bool HasChildren();

	/// <summary>
	/// Determine if the item should be shown as a child in the parent collection.
	/// </summary>
	/// <returns> True if this list item should be shown. </returns>
	bool ShouldBeShown();

	/// <summary>
	/// Determine if the items child should be shown.
	/// </summary>
	/// <returns> True if this items child should be shown. </returns>
	bool ShouldShowChild(IHierarchyListItem child);

	/// <summary>
	/// Determine if the items children should be shown.
	/// </summary>
	/// <returns> True if this item children should be shown. </returns>
	bool ShouldShowChildren();

	/// <summary>
	/// Disconnect the parent of this item. This will remove parent events.
	/// It will also clear the parent if the current parent is the provided parent.
	/// </summary>
	internal void DisconnectParent(IHierarchyListItem parent);

	/// <summary>
	/// Trigger the ShouldBeShownChanged event.
	/// </summary>
	internal void OnShouldBeShownChanged();

	/// <summary>
	/// Trigger the OnShouldShowChildrenChanged event.
	/// </summary>
	internal void OnShouldShowChildrenChanged();

	/// <summary>
	/// Removes the child from this parent.
	/// </summary>
	internal void RemoveChild(IHierarchyListItem child);

	/// <summary>
	/// Update the parent of this item.
	/// </summary>
	internal void UpdateParent(IHierarchyListItem parent);

	#endregion

	#region Events

	/// <summary>
	/// Event for when a child is added to this list item.
	/// </summary>
	event EventHandler<IHierarchyListItem> ChildAdded;

	/// <summary>
	/// Event for when a child is removed from this list item.
	/// </summary>
	event EventHandler<IHierarchyListItem> ChildRemoved;

	/// <summary>
	/// Event for when the parent has changed. The event arg is the old
	/// parent that was removed. The sender is the item that has a
	/// reference to the new parent.
	/// </summary>
	event EventHandler<IHierarchyListItem> ParentChanged;

	/// <summary>
	/// Event when this list item shown state is changed.
	/// </summary>
	event EventHandler ShouldBeShownChanged;

	/// <summary>
	/// Event when this list item children shown state is changed.
	/// </summary>
	event EventHandler ShouldShowChildrenChanged;

	#endregion
}