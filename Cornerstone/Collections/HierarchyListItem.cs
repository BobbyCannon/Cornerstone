#region References

using System;
using System.Collections.Specialized;
using System.Linq;
using Cornerstone.Presentation;
using Cornerstone.Sync;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Collections;

/// <summary>
/// Represents a sync model in a hierarchy list.
/// </summary>
public class HierarchyListItem<T> : SyncModel, IDispatchable
	where T : HierarchyListItem<T>
{
	#region Fields

	private ISpeedyList<T> _children;
	private readonly IDispatcher _dispatcher;
	private T _parent;

	#endregion

	#region Constructors

	public HierarchyListItem() : this(null, null, null)
	{
	}

	public HierarchyListItem(IDispatcher dispatcher) : this(dispatcher, null, null)
	{
	}

	public HierarchyListItem(IDispatcher dispatcher, T parent, ISpeedyList<T> children)
	{
		_dispatcher = dispatcher;
		_parent = parent;
		_children = children ?? new SpeedyList<T>();
	}

	#endregion

	#region Properties

	public ISpeedyList<T> Children => GetChildren();

	public T Parent => GetParent();

	#endregion

	#region Methods

	public T Add(T childItem)
	{
		childItem.UpdateParent((T) this);
		_children.Add(childItem);
		return childItem;
	}

	public bool AnyDescendants(Func<T, bool> check)
	{
		if (check((T) this))
		{
			return true;
		}

		foreach (var child in _children)
		{
			if (child.AnyDescendants(check))
			{
				return true;
			}
		}

		return false;
	}

	public T FirstOrDefault(Func<T, bool> check)
	{
		if (check((T) this))
		{
			return (T) this;
		}

		foreach (var child in _children)
		{
			if (child.AnyDescendants(check))
			{
				return child;
			}
		}

		return default;
	}

	public ISpeedyList<T> GetChildren()
	{
		return _children;
	}

	/// <inheritdoc />
	public IDispatcher GetDispatcher()
	{
		return _dispatcher;
	}

	public T GetParent()
	{
		return _parent;
	}

	public bool HasChildren()
	{
		return GetChildren().Any();
	}

	public virtual bool ShouldBeShown()
	{
		var parent = GetParent();
		return (parent == null)
			|| (parent.ShouldShowChild((T) this)
				&& parent.ShouldShowChildren());
	}

	public virtual bool ShouldShowChild(T child)
	{
		return ShouldShowChildren();
	}

	public virtual bool ShouldShowChildren()
	{
		return true;
	}

	protected void DisconnectParent(T parent)
	{
		DisconnectParentEventSubscriptions(parent);

		if (_parent == parent)
		{
			_parent = null;
		}
	}

	/// <summary>
	/// Trigger the ChildAdded event.
	/// </summary>
	/// <param name="e"> The child added. </param>
	protected virtual void OnChildAdded(T e)
	{
		ChildAdded?.Invoke(this, e);
	}

	/// <summary>
	/// Trigger the ChildRemoved event.
	/// </summary>
	/// <param name="e"> The child removed. </param>
	protected virtual void OnChildRemoved(T e)
	{
		ChildRemoved?.Invoke(this, e);
	}

	/// <summary>
	/// Trigger the ParentChanged event.
	/// </summary>
	/// <param name="e"> The parent that was assigned. </param>
	[SuppressPropertyChangedWarnings]
	protected virtual void OnParentChanged(T e)
	{
		ParentChanged?.Invoke(this, e);
	}

	/// <summary>
	/// Trigger the ShouldBeShownChanged event.
	/// </summary>
	[SuppressPropertyChangedWarnings]
	protected virtual void OnShouldBeShownChanged()
	{
		ShouldBeShownChanged?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Trigger the ShouldShowChildrenChanged event.
	/// </summary>
	[SuppressPropertyChangedWarnings]
	protected virtual void OnShouldShowChildrenChanged()
	{
		ShouldShowChildrenChanged?.Invoke(this, EventArgs.Empty);
	}

	protected void RemoveChild(T child)
	{
		_children?.Remove(child);
	}

	/// <summary>
	/// Initialize the relationship. This should be called in the constructor.
	/// </summary>
	/// <param name="children"> The children for the list item. </param>
	protected void UpdateChildren(ISpeedyList<T> children)
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
	protected void UpdateParent(T parent)
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
				var listItem = (T) item;
				OnChildRemoved(listItem);
			}
		}

		if (e.NewItems != null)
		{
			foreach (var item in e.NewItems)
			{
				var listItem = (T) item;
				OnChildAdded(listItem);
			}
		}
	}

	private ISpeedyList<T> ConnectChildrenEvents(ISpeedyList<T> children)
	{
		if (children != null)
		{
			children.CollectionChanged += ChildrenOnCollectionChanged;
		}

		return children;
	}

	private T ConnectParentEvents(T parent)
	{
		if (parent != null)
		{
			parent.ShouldBeShownChanged += ParentOnShouldBeShownChanged;
			parent.ShouldShowChildrenChanged += ParentShouldShowChildrenChanged;
		}

		return parent;
	}

	private void DisconnectChildrenEventSubscriptions(ISpeedyList<T> children)
	{
		if (children == null)
		{
			return;
		}

		children.CollectionChanged -= ChildrenOnCollectionChanged;
	}

	private void DisconnectParentEventSubscriptions(T parent)
	{
		if (parent == null)
		{
			return;
		}

		parent.ShouldBeShownChanged -= ParentOnShouldBeShownChanged;
		parent.ShouldShowChildrenChanged -= ParentShouldShowChildrenChanged;
	}

	private void ParentOnShouldBeShownChanged(object sender, EventArgs e)
	{
		OnShouldBeShownChanged();
	}

	private void ParentShouldShowChildrenChanged(object sender, EventArgs e)
	{
		OnShouldBeShownChanged();
	}

	#endregion

	#region Events

	public event EventHandler<T> ChildAdded;

	public event EventHandler<T> ChildRemoved;

	public event EventHandler<T> ParentChanged;

	public event EventHandler ShouldBeShownChanged;

	public event EventHandler ShouldShowChildrenChanged;

	#endregion
}