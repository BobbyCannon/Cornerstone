#region References

using System;
using System.Collections.Generic;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Collections;

/// <summary>
/// Represents a hierarchy of data.
/// </summary>
public partial class SpeedyTree<T>
	: SyncModel, ISpeedyTree<T>, IHierarchyItem
	where T : class, ISpeedyTree<T>
{
	#region Fields

	private Func<T, bool> _filterCheck;

	#endregion

	#region Constructors

	public SpeedyTree() : this(null)
	{
	}

	public SpeedyTree(T parent, params OrderBy<T>[] orderBy)
	{
		Parent = parent;
		Children = new PresentationList<T>(orderBy);
		Children.ListUpdated += ChildrenOnListUpdated;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Children for this item.
	/// </summary>
	public IPresentationList<T> Children { get; }

	[Notify]
	public partial IEqualityComparer<T> DistinctCheck { get; set; }

	/// <summary>
	/// An optional filter to restrict the collection. Applies to this node and all descendants.
	/// </summary>
	public Func<T, bool> FilterCheck
	{
		get => _filterCheck;
		set
		{
			var oldValue = _filterCheck;
			_filterCheck = value;
			ApplyFilterRecursively(value);
			OnPropertyChanged(nameof(FilterCheck), oldValue, value);
		}
	}

	/// <summary>
	/// True if this item is expanded otherwise false.
	/// </summary>
	[Notify]
	public partial bool IsExpanded { get; set; }

	/// <summary>
	/// The expression to order this collection by.
	/// </summary>
	public OrderBy<T>[] OrderBy
	{
		get => Children.OrderBy;
		set
		{
			var oldValue = OrderBy;
			ApplyOrderRecursively(value);
			OnPropertyChanged(nameof(OrderBy), oldValue, value);
		}
	}

	/// <summary>
	/// The parent of this item.
	/// </summary>
	[Notify]
	public partial T Parent { get; set; }

	#endregion

	#region Methods

	public void Add(T child)
	{
		Children.Add(child);
	}

	public bool AnyDescendants(Func<T, bool> predicate)
	{
		return SpeedyTree.AnyDescendants(this, predicate);
	}

	public void ApplyFilterRecursively(Func<T, bool> filter)
	{
		// Apply to this node's children
		Children.FilterCheck = x => ComputeFilterResult(x, filter);

		// Recursively apply to all child trees
		foreach (var child in Children)
		{
			child?.ApplyFilterRecursively(filter);
		}
	}

	public void ApplyOrderRecursively(OrderBy<T>[] orderBys)
	{
		// Apply the filter to this node's children
		Children.OrderBy = orderBys;

		// Recursively apply to all child trees
		foreach (var child in Children)
		{
			child?.ApplyOrderRecursively(orderBys);
		}
	}

	public virtual bool CanHaveChildren()
	{
		return true;
	}

	public bool CanOrder()
	{
		return false;
	}

	public void Clear()
	{
		Children.Clear();
	}

	public T FirstOrDefaultDescendants(Func<T, bool> predicate)
	{
		return this.TryFindDescendants<T>(predicate, out var found) ? found : null;
	}

	public T2 FirstOrDefaultDescendants<T2>(Func<T2, bool> predicate) where T2 : class
	{
		return this.TryFindDescendants(predicate, out var found) ? found : null;
	}

	public IPresentationList GetChildren()
	{
		return (IPresentationList) Children;
	}

	public virtual int GetOrder()
	{
		return 0;
	}

	public IHierarchyItem GetParent()
	{
		return (IHierarchyItem) Parent;
	}

	public void Load(IEnumerable<T> items)
	{
		Children.Load(items);
		ApplyFilterRecursively(FilterCheck);
	}

	public void Reconcile(IList<T> items, Func<T, T, bool> hasChanged = null)
	{
		Children.ReconcileListAndItems(items, DistinctCheck, hasChanged);
		ApplyFilterRecursively(FilterCheck);
	}

	public void RefreshFilter()
	{
		var stack = new Stack<IPresentationList<T>>();
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

				stack.Push(childItem.Children);
			}
		}
	}

	/// <summary>
	/// Remove all entries that match predicate
	/// </summary>
	/// <param name="predicate"> The predicate to find entries to remove. </param>
	public void Remove(Func<T, bool> predicate)
	{
		var found = FirstOrDefaultDescendants(predicate);
		(found?.Parent?.Children ?? Children)?.Remove(found);
	}

	public virtual void SetOrder(int value)
	{
	}

	public virtual void SetParent(IHierarchyItem parent)
	{
		if (parent is T tParent)
		{
			Parent = tParent;
		}
	}

	public IEnumerable<T2> WhereDescendants<T2>()
	{
		return this.WhereDescendants<T, T2>(_ => true);
	}

	public IEnumerable<T2> WhereDescendants<T2>(Func<T2, bool> predicate)
	{
		return SpeedyTree.WhereDescendants(this, predicate);
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

	private void ChildrenOnListUpdated(object sender, PresentationListUpdatedEventArg<T> e)
	{
		if (e.Removed != null)
		{
			foreach (var item in e.Removed)
			{
				OnChildRemoved(item);
				Parent?.Children.RefreshFilter();
			}
		}

		if (e.Added != null)
		{
			foreach (var item in e.Added)
			{
				item.Parent = this as T;
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

	#endregion

	#region Events

	public event EventHandler<T> ChildAdded;
	public event EventHandler<T> ChildRemoved;
	public event EventHandler<T> ParentChanged;

	#endregion
}

public static class SpeedyTree
{
	#region Methods

	public static bool AnyDescendants<T>(this ISpeedyTree<T> item, Func<T, bool> predicate)
		where T : class, ISpeedyTree<T>
	{
		if (item is T tItem && predicate(tItem))
		{
			return true;
		}

		foreach (var child in item.Children)
		{
			if (AnyDescendants(child, predicate))
			{
				return true;
			}
		}

		return false;
	}

	public static void ForEachDescendants<T>(this ISpeedyTree<T> item, Action<T> action)
		where T : class, ISpeedyTree<T>
	{
		if (item is T value)
		{
			action(value);
		}

		foreach (var child in item.Children)
		{
			child.ForEachDescendants(action);
		}
	}

	public static bool TryFindDescendants<T>(this ISpeedyTree<T> item, Func<T, bool> predicate, out T foundItem)
		where T : class, ISpeedyTree<T>
	{
		if (item is T tItem && predicate(tItem))
		{
			foundItem = tItem;
			return true;
		}

		foreach (var child in item.Children)
		{
			if (TryFindDescendants<T>(child, predicate, out foundItem))
			{
				return true;
			}
		}

		foundItem = null;
		return false;
	}

	public static bool TryFindDescendants<T, T2>(this ISpeedyTree<T> item, Func<T2, bool> predicate, out T2 foundItem)
		where T : class, ISpeedyTree<T>
		where T2 : class
	{
		if (item is T2 tItem && predicate(tItem))
		{
			foundItem = tItem;
			return true;
		}

		foreach (var child in item.Children)
		{
			if (TryFindDescendants(child, predicate, out foundItem))
			{
				return true;
			}
		}

		foundItem = null;
		return false;
	}

	public static IEnumerable<T2> WhereDescendants<T, T2>(this ISpeedyTree<T2> item, Func<T2, bool> predicate)
		where T : class, ISpeedyTree<T2>
		where T2 : class, ISpeedyTree<T2>
	{
		if (item is T2 value && predicate(value))
		{
			yield return value;
		}

		foreach (var child in item.Children)
		{
			foreach (var d in WhereDescendants<T, T2>(child, predicate))
			{
				yield return d;
			}
		}
	}

	public static IEnumerable<T2> WhereDescendants<T, T2>(this ISpeedyTree<T> item, Func<T2, bool> predicate)
		where T : class, ISpeedyTree<T>
	{
		if (item is T2 value && predicate(value))
		{
			yield return value;
		}

		foreach (var child in item.Children)
		{
			foreach (var d in WhereDescendants(child, predicate))
			{
				yield return d;
			}
		}
	}

	#endregion
}

public interface ISpeedyTree<T>
	where T : class, ISpeedyTree<T>
{
	#region Properties

	/// <summary>
	/// Get the children for this item.
	/// </summary>
	IPresentationList<T> Children { get; }

	/// <summary>
	/// An optional comparer to use if you want a distinct list.
	/// </summary>
	IEqualityComparer<T> DistinctCheck { get; set; }

	/// <summary>
	/// An optional filter to restrict the collection.
	/// </summary>
	Func<T, bool> FilterCheck { get; set; }

	/// <summary>
	/// True if this item is expanded otherwise false.
	/// </summary>
	bool IsExpanded { get; set; }

	/// <summary>
	/// The expression to order this collection by.
	/// </summary>
	OrderBy<T>[] OrderBy { get; set; }

	/// <summary>
	/// Gets or set the parent of this item.
	/// </summary>
	T Parent { get; set; }

	#endregion

	#region Methods

	void ApplyFilterRecursively(Func<T, bool> filter);
	void ApplyOrderRecursively(OrderBy<T>[] orderBys);
	T FirstOrDefaultDescendants(Func<T, bool> predicate);
	T2 FirstOrDefaultDescendants<T2>(Func<T2, bool> predicate) where T2 : class;
	void Load(IEnumerable<T> items);
	void Reconcile(IList<T> items, Func<T, T, bool> hasChanged = null);
	void RefreshFilter();

	#endregion

	#region Events

	event EventHandler<T> ChildAdded;
	event EventHandler<T> ChildRemoved;
	event EventHandler<T> ParentChanged;

	#endregion
}