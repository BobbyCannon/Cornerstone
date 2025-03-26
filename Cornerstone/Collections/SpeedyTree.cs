#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cornerstone.Attributes;
using Cornerstone.Presentation;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Collections;

/// <summary>
/// Represents a hierarchy of data.
/// </summary>
public class SpeedyTree<T> : SyncModel, IDispatchable
	where T : SpeedyTree<T>
{
	#region Fields

	private IDispatcher _dispatcher;
	private Func<T, bool> _filterCheck;

	#endregion

	#region Constructors

	public SpeedyTree() : this(null, null)
	{
	}

	public SpeedyTree(T parent, IDispatcher dispatcher, params OrderBy<T>[] orderBy)
	{
		_dispatcher = dispatcher;

		Parent = parent;
		Children = new SpeedyList<T>(dispatcher, orderBy);

		WeakEventManager.AddSpeedyListUpdated<SpeedyList<T>, T, SpeedyTree<T>>(Children, this, ChildrenOnListUpdated);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Children for this item.
	/// </summary>
	public SpeedyList<T> Children { get; }

	/// <summary>
	/// An optional filter to restrict the collection. Applies to this node and all descendants.
	/// </summary>
	[SerializationIgnore]
	public Func<T, bool> FilterCheck
	{
		get => _filterCheck;
		set
		{
			_filterCheck = value;
			ApplyFilterRecursively(value);
		}
	}

	/// <summary>
	/// True if this item is expanded otherwise false.
	/// </summary>
	public bool IsExpanded { get; set; }

	/// <summary>
	/// The expression to order this collection by.
	/// </summary>
	[SerializationIgnore]
	public OrderBy<T>[] OrderBy
	{
		get => Children.OrderBy;
		set => ApplyOrderRecursively(value);
	}

	/// <summary>
	/// The parent of this item.
	/// </summary>
	public SpeedyTree<T> Parent { get; private set; }

	#endregion

	#region Methods

	public void Add(T child)
	{
		Children.Add(child);
	}

	public bool AnyDescendants(Func<T, bool> predicate)
	{
		if (predicate((T) this))
		{
			return true;
		}

		foreach (var child in Children)
		{
			if (child.AnyDescendants(predicate))
			{
				return true;
			}
		}

		return false;
	}

	public virtual bool CanHaveChildren()
	{
		return true;
	}

	public void Clear()
	{
		Children.Clear();
	}

	public T FirstOrDefaultDescendants(Func<T, bool> predicate)
	{
		return TryFindDescendants((T) this, predicate);
	}

	public void ForEachDescendants(Action<T> predicate)
	{
		if (this is T value)
		{
			predicate(value);
		}

		foreach (var child in Children)
		{
			child.ForEachDescendants(predicate);
		}
	}

	public ISpeedyList<T> GetChildren()
	{
		return Children;
	}

	public IDispatcher GetDispatcher()
	{
		return _dispatcher;
	}

	public SpeedyTree<T> GetParent()
	{
		return Parent;
	}

	public void Load(IEnumerable<T> items)
	{
		Children.Load(items);
		ApplyFilterRecursively(FilterCheck);
	}

	public void RefreshFilter()
	{
		var stack = new Stack<ISpeedyList<T>>();
		stack.Push(Children);

		while (stack.Count > 0)
		{
			var parent = stack.Pop();
			parent.RefreshFilter();

			foreach (var child in parent)
			{
				if (child is not { } childItem)
				{
					continue;
				}

				stack.Push(childItem.GetChildren());
			}
		}
	}

	public void UpdateDispatcher(IDispatcher dispatcher)
	{
		_dispatcher = dispatcher;
	}

	protected virtual void OnChildAdded(T e)
	{
		ChildAdded?.Invoke(this, e);
	}

	protected virtual void OnChildRemoved(T e)
	{
		ChildRemoved?.Invoke(this, e);
	}

	protected virtual void OnParentChanged(T e)
	{
		ParentChanged?.Invoke(this, e);
	}

	private void ApplyFilterRecursively(Func<T, bool> filter)
	{
		Func<T, bool> recursiveFilter = item =>
		{
			var result = ComputeFilterResult(item, filter);
			Debug.WriteLine($"Filter check for {item}: {result}");
			return result;
		};


		// Apply to this node's children
		Children.FilterCheck = recursiveFilter;
		//Children.FilterCheck = x => ComputeFilterResult(x, filter);

		// Recursively apply to all child trees
		foreach (var child in Children)
		{
			child?.ApplyFilterRecursively(recursiveFilter);
		}
	}

	private void ApplyOrderRecursively(OrderBy<T>[] orderBys)
	{
		// Apply the filter to this node's children
		Children.OrderBy = orderBys;

		// Recursively apply to all child trees
		foreach (var child in Children)
		{
			child?.ApplyOrderRecursively(orderBys);
		}
	}

	private void ChildrenOnListUpdated(object sender, SpeedyListUpdatedEventArg<T> e)
	{
		if (e.Removed != null)
		{
			foreach (var item in e.Removed)
			{
				OnChildRemoved(item);
				Parent?.GetChildren().RefreshFilter();
			}
		}

		if (e.Added != null)
		{
			foreach (var item in e.Added)
			{
				item.Parent = this;
				item.FilterCheck = _filterCheck;
				OnChildAdded(item);
			}
		}
	}

	private bool ComputeFilterResult(T node, Func<T, bool> filter)
	{
		var passesFilter = filter?.Invoke(node) ?? true;
		if (passesFilter)
		{
			return true;
		}

		var stack = new Stack<T>();
		stack.Push(node);

		while (stack.Count > 0)
		{
			var current = stack.Pop();
			foreach (var child in current.Children)
			{
				if (child == null)
				{
					continue;
				}
				if (filter.Invoke(child))
				{
					return true;
				}
				stack.Push(child);
			}
		}

		return false;
	}

	private static T TryFindDescendants(T list, Func<T, bool> predicate)
	{
		if (predicate(list))
		{
			return list;
		}

		foreach (var child in list.Children)
		{
			var found = TryFindDescendants(child, predicate);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}

	#endregion

	#region Events

	public event EventHandler<T> ChildAdded;
	public event EventHandler<T> ChildRemoved;
	public event EventHandler<T> ParentChanged;

	#endregion
}