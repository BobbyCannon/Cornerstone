#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Collections;

#endregion

namespace Avalonia.Diagnostics.ViewModels;

public abstract class TreeNodeCollection : IAvaloniaReadOnlyList<TreeNode>, IList, IDisposable
{
	#region Fields

	public static readonly TreeNodeCollection Empty;
	private AvaloniaList<TreeNode> _inner;

	#endregion

	#region Constructors

	public TreeNodeCollection(TreeNode owner)
	{
		Owner = owner;
	}

	static TreeNodeCollection()
	{
		Empty = new EmptyTreeNodeCollection();
	}

	#endregion

	#region Properties

	public int Count => EnsureInitialized().Count;

	public TreeNode this[int index] => EnsureInitialized()[index];

	protected TreeNode Owner { get; }
	
	bool IList.IsFixedSize => false;
	
	bool IList.IsReadOnly => true;
	
	bool ICollection.IsSynchronized => false;

	object IList.this[int index]
	{
		get => this[index];
		set => throw new NotImplementedException();
	}

	object ICollection.SyncRoot => this;

	#endregion

	#region Methods

	public virtual void Dispose()
	{
		if (_inner is object)
		{
			foreach (var node in _inner)
			{
				node.Dispose();
			}
		}
	}

	public IEnumerator<TreeNode> GetEnumerator()
	{
		return EnsureInitialized().GetEnumerator();
	}

	protected abstract void Initialize(AvaloniaList<TreeNode> nodes);

	int IList.Add(object value)
	{
		throw new NotImplementedException();
	}

	void IList.Clear()
	{
		throw new NotImplementedException();
	}

	bool IList.Contains(object value)
	{
		return EnsureInitialized().Contains((TreeNode) value!);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		throw new NotImplementedException();
	}

	private AvaloniaList<TreeNode> EnsureInitialized()
	{
		if (_inner is null)
		{
			_inner = [];
			Initialize(_inner);
		}
		return _inner;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	int IList.IndexOf(object value)
	{
		return EnsureInitialized().IndexOf((TreeNode) value!);
	}

	void IList.Insert(int index, object value)
	{
		throw new NotImplementedException();
	}

	void IList.Remove(object value)
	{
		throw new NotImplementedException();
	}

	void IList.RemoveAt(int index)
	{
		throw new NotImplementedException();
	}

	#endregion

	#region Events

	public event NotifyCollectionChangedEventHandler CollectionChanged
	{
		add => EnsureInitialized().CollectionChanged += value;
		remove => EnsureInitialized().CollectionChanged -= value;
	}

	public event PropertyChangedEventHandler PropertyChanged
	{
		add => EnsureInitialized().PropertyChanged += value;
		remove => EnsureInitialized().PropertyChanged -= value;
	}

	#endregion

	#region Classes

	private class EmptyTreeNodeCollection : TreeNodeCollection
	{
		#region Constructors

		public EmptyTreeNodeCollection() : base(null!)
		{
		}

		#endregion

		#region Methods

		protected override void Initialize(AvaloniaList<TreeNode> nodes)
		{
		}

		#endregion
	}

	#endregion
}