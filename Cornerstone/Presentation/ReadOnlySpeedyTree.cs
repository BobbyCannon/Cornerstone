#region References

using System;
using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// A readonly proxy for a speedy tree.
/// </summary>
public partial class ReadOnlySpeedyTree<T> : CornerstoneObject, ISpeedyTree<T>
	where T : class, ISpeedyTree<T>, IHierarchySyncItem
{
	#region Constructors

	/// <summary>
	/// Create an instance of the tree.
	/// </summary>
	public ReadOnlySpeedyTree(SpeedyTree<T> tree)
	{
		Tree = tree;
		Tree.ChildAdded += OnChildAdded;
		Tree.ChildRemoved += OnChildRemoved;
		Tree.ParentChanged += OnParentChanged;
	}

	#endregion

	#region Properties

	public IPresentationList<T> Children => Tree.Children;

	public IEqualityComparer<T> DistinctCheck
	{
		get => Tree.DistinctCheck;
		set => throw new NotSupportedException();
	}

	public Func<T, bool> FilterCheck
	{
		get => Tree.FilterCheck;
		set => throw new NotSupportedException();
	}

	[Notify]
	public partial bool IsExpanded { get; set; }

	public OrderBy<T>[] OrderBy
	{
		get => Tree.OrderBy;
		set => throw new NotSupportedException();
	}

	[Notify]
	public partial T Parent { get; set; }

	public SpeedyTree<T> Tree { get; }

	#endregion

	#region Methods

	public void ApplyFilterRecursively(Func<T, bool> filter)
	{
		Tree.ApplyFilterRecursively(filter);
	}

	public void ApplyOrderRecursively(OrderBy<T>[] orderBys)
	{
		Tree.ApplyOrderRecursively(orderBys);
	}

	public bool CanHaveChildren()
	{
		return Tree.CanHaveChildren();
	}

	public bool CanOrder()
	{
		return Tree.CanOrder();
	}

	public T FirstOrDefaultDescendants(Func<T, bool> predicate)
	{
		return Tree.FirstOrDefaultDescendants(predicate);
	}

	public T2 FirstOrDefaultDescendants<T2>(Func<T2, bool> predicate) where T2 : class
	{
		return Tree.FirstOrDefaultDescendants(predicate);
	}

	public IPresentationList GetChildren()
	{
		return Tree.GetChildren();
	}

	public int GetOrder()
	{
		return Tree.GetOrder();
	}

	public T GetParent()
	{
		return Tree.Parent;
	}

	public void Load(IEnumerable<T> items)
	{
		throw new NotSupportedException();
	}

	public void Reconcile(IList<T> items, Func<T, T, bool> hasChanged = null)
	{
		throw new NotSupportedException();
	}

	public void RefreshFilter()
	{
		Tree.RefreshFilter();
	}

	public void SetOrder(int value)
	{
		throw new NotSupportedException();
	}

	public void SetParent(IHierarchyItem parent)
	{
		throw new NotSupportedException();
	}

	protected virtual void OnChildAdded(object sender, T e)
	{
		ChildAdded?.Invoke(this, e);
	}

	protected virtual void OnChildRemoved(object sender, T e)
	{
		ChildRemoved?.Invoke(this, e);
	}

	private void OnParentChanged(object sender, T e)
	{
		ParentChanged?.Invoke(this, e);
	}

	#endregion

	#region Events

	public event EventHandler<T> ChildAdded;
	public event EventHandler<T> ChildRemoved;
	public event EventHandler<T> ParentChanged;

	#endregion
}