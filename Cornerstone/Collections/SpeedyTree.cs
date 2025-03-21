#region References

using System;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Collections;

/// <summary>
/// Represents a hierarchy of data.
/// </summary>
public class SpeedyTree<T> : SpeedyList<T>, ISpeedyTreeItem<T>
	where T : ISpeedyTreeItem<T>
{
	#region Fields

	private T _parent;

	#endregion

	#region Constructors

	public SpeedyTree(T parent, IDispatcher dispatcher, params OrderBy<T>[] orderBy)
		: base(dispatcher, orderBy)
	{
		_parent = parent;
	}

	#endregion

	#region Methods

	public ISpeedyList<T> GetChildren()
	{
		return this;
	}

	public T GetParent()
	{
		return _parent;
	}

	protected virtual void OnChildAdded(T e)
	{
		ChildAdded?.Invoke(this, e);
	}

	protected virtual void OnChildRemoved(T e)
	{
		ChildRemoved?.Invoke(this, e);
	}

	protected override void OnListUpdated(SpeedyListUpdatedEventArg<T> e)
	{
		if (e.Removed != null)
		{
			foreach (var item in e.Removed)
			{
				OnChildRemoved(item);
			}
		}

		if (e.Added != null)
		{
			foreach (var item in e.Added)
			{
				item.UpdateParent((T)(object) this);
				OnChildAdded(item);
			}
		}

		base.OnListUpdated(e);
	}

	protected virtual void OnParentChanged(T e)
	{
		ParentChanged?.Invoke(this, e);
	}

	void ISpeedyTreeItem<T>.RemoveChild(T child)
	{
		Remove(child);
	}

	void ISpeedyTreeItem<T>.UpdateParent(T parent)
	{
		_parent = parent;
		OnParentChanged(_parent);
	}

	#endregion

	#region Events

	public event EventHandler<T> ChildAdded;
	public event EventHandler<T> ChildRemoved;
	public event EventHandler<T> ParentChanged;

	#endregion
}

/// <summary>
/// Represents a tree list of data.
/// </summary>
public interface ISpeedyTreeItem<T>
	where T : ISpeedyTreeItem<T>
{
	#region Methods

	/// <summary>
	/// Get the children for the list item.
	/// </summary>
	/// <returns> </returns>
	ISpeedyList<T> GetChildren();

	/// <summary>
	/// Get the parent of this item.
	/// </summary>
	/// <returns> </returns>
	T GetParent();

	/// <summary>
	/// Removes the child from this parent.
	/// </summary>
	internal void RemoveChild(T child);

	/// <summary>
	/// Update the parent of this item.
	/// </summary>
	internal void UpdateParent(T parent);

	#endregion

	#region Events

	/// <summary>
	/// Event for when a child is added to this list item.
	/// </summary>
	event EventHandler<T> ChildAdded;

	/// <summary>
	/// Event for when a child is removed from this list item.
	/// </summary>
	event EventHandler<T> ChildRemoved;

	/// <summary>
	/// Event for when the parent has changed. The event arg is the old
	/// parent that was removed. The sender is the item that has a
	/// reference to the new parent.
	/// </summary>
	event EventHandler<T> ParentChanged;

	#endregion
}