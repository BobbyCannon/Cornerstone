#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Collections;

/// <summary>
/// A kind of List(T), but more efficient for random insertions/removal.
/// Also has cheap Clone() and SubBuffer() implementations.
/// </summary>
/// <remarks>
/// This class is not thread-safe: multiple concurrent write operations or writes concurrent to reads have undefined behavior.
/// Concurrent reads, however, are safe. However, clones of a buffer are safe to use on other threads even though they share data
/// with the original buffer.
/// </remarks>
public class RopeBuffer<T> : Buffer<T>, ICloneable
{
	#region Fields

	internal RopeBufferNode<T> Root;

	// cached pointer to 'last used node', used to speed up accesses by index that are close together
	[NonSerialized]
	private volatile ImmutableStack<BufferCacheEntry> _lastUsedNodeStack;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new buffer representing the empty buffer.
	/// </summary>
	public RopeBuffer() : this(RopeBufferNode<T>.EmptyBufferNode)
	{
	}

	/// <summary>
	/// Creates a new buffer representing the provided value.
	/// </summary>
	public RopeBuffer(IEnumerable<T> value) : this()
	{
		InternalInsert(0, value);
	}

	/// <summary>
	/// Creates a new buffer representing the provided values.
	/// </summary>
	public RopeBuffer(params T[] value) : this()
	{
		InternalInsert(0, value, 0, value.Length);
	}

	private RopeBuffer(RopeBufferNode<T> root)
	{
		Root = root;
		ResetState();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the length of the buffer.
	/// Runs in O(1).
	/// </summary>
	/// <remarks>
	/// This method counts as a read access and may be called concurrently to other read accesses.
	/// </remarks>
	public override int Count => Root.length;

	/// <summary>
	/// Gets the length of the buffer.
	/// Runs in O(1).
	/// </summary>
	/// <remarks>
	/// This method counts as a read access and may be called concurrently to other read accesses.
	/// </remarks>
	public int End => Root.length <= 0 ? 0 : Root.length - 1;

	/// <inheritdoc />
	public override bool IsReadOnly => false;

	/// <summary>
	/// Gets/Sets a single character.
	/// Runs in O(lg N) for random access. Sequential read-only access benefits from a special optimization and runs in amortized O(1).
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException"> Offset is outside the valid range (0 to Length-1). </exception>
	/// <remarks>
	/// The getter counts as a read access and may be called concurrently to other read accesses.
	/// </remarks>
	public override T this[int index]
	{
		get
		{
			// use unsigned integers - this way negative values for index overflow and can be tested for with the same check
			if (unchecked((uint) index >= (uint) Length))
			{
				throw new ArgumentOutOfRangeException(nameof(index), index, $"0 <= index < {Length}");
			}

			var entry = FindNodeUsingCache(index).PeekOrDefault();
			return entry.Node.contents[index - entry.NodeStartIndex];
		}

		set
		{
			if ((index < 0) || (index >= Length))
			{
				throw new ArgumentOutOfRangeException(nameof(index), index, $"0 <= index < {Length}");
			}

			Root = Root.SetElement(index, value);

			ResetState();
		}
	}

	/// <summary>
	/// Gets the length of the buffer.
	/// Runs in O(1).
	/// </summary>
	/// <remarks>
	/// This method counts as a read access and may be called concurrently to other read accesses.
	/// </remarks>
	public int Length => Root.length;

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Add(T item)
	{
		Insert(Length, [item], 0, 1);
	}

	/// <summary>
	/// Appends another buffer to the end of this buffer.
	/// Runs in O(lg N + lg M), plus a per-node cost as if <c> newElements.Clone() </c> was called.
	/// </summary>
	/// <exception cref="ArgumentNullException"> newElements is null. </exception>
	public void Add(RopeBuffer<T> newElements)
	{
		Insert(Length, newElements);
	}

	/// <summary>
	/// Appends new elements to the end of this buffer.
	/// Runs in O(lg N + M), where N is the length of this buffer and M is the number of new elements.
	/// </summary>
	/// <exception cref="ArgumentNullException"> array is null. </exception>
	public void Add(T[] array, int arrayIndex, int count)
	{
		Insert(Length, array, arrayIndex, count);
	}

	/// <inheritdoc />
	public override void Clear()
	{
		InternalReset();
		ResetState();
	}

	/// <summary>
	/// Clones the buffer.
	/// This operation runs in linear time to the number of buffer nodes touched since the last clone was created.
	/// If you count the per-node cost to the operation modifying the buffer (doing this doesn't increase the complexity of the modification operations);
	/// the remainder of Clone() runs in O(1).
	/// </summary>
	/// <remarks>
	/// This method counts as a read access and may be called concurrently to other read accesses.
	/// </remarks>
	public RopeBuffer<T> Clone()
	{
		// The Publish() call actually modifies this buffer instance; but this modification is thread-safe
		// as long as the tree structure doesn't change during the operation.
		Root.Publish();
		return new RopeBuffer<T>(Root);
	}

	/// <summary>
	/// Concatenates two ropes. The input ropes are not modified.
	/// Runs in O(lg N + lg M).
	/// </summary>
	/// <remarks>
	/// This method counts as a read access and may be called concurrently to other read accesses.
	/// </remarks>
	public static RopeBuffer<T> Concat(RopeBuffer<T> left, RopeBuffer<T> right)
	{
		if (left == null)
		{
			throw new ArgumentNullException(nameof(left));
		}

		if (right == null)
		{
			throw new ArgumentNullException(nameof(right));
		}

		left.Root.Publish();
		right.Root.Publish();

		return new RopeBuffer<T>(RopeBufferNode<T>.Concat(left.Root, right.Root));
	}

	/// <summary>
	/// Concatenates multiple ropes. The input ropes are not modified.
	/// </summary>
	/// <remarks>
	/// This method counts as a read access and may be called concurrently to other read accesses.
	/// </remarks>
	public static RopeBuffer<T> Concat(params RopeBuffer<T>[] ropes)
	{
		if (ropes == null)
		{
			throw new ArgumentNullException(nameof(ropes));
		}

		var result = new RopeBuffer<T>();
		foreach (var r in ropes)
		{
			result.Add(r);
		}

		return result;
	}

	/// <inheritdoc />
	public override bool Contains(T item)
	{
		return IndexOf(item) >= 0;
	}

	/// <inheritdoc />
	public override void CopyTo(T[] array, int arrayIndex)
	{
		CopyTo(0, array, arrayIndex, Length);
	}

	/// <summary>
	/// Copies a part of the buffer into the specified array.
	/// Runs in O(lg N + M).
	/// </summary>
	/// <remarks>
	/// This method counts as a read access and may be called concurrently to other read accesses.
	/// </remarks>
	public void CopyTo(int index, T[] array, int arrayIndex, int count)
	{
		VerifyRange(index, count);
		array.BoundsCheckArray(arrayIndex, count);
		Root.CopyTo(index, array, arrayIndex, count);
	}

	/// <inheritdoc />
	/// <remarks>
	/// This method counts as a read access and may be called concurrently to other read accesses.
	/// </remarks>
	public override IEnumerator<T> GetEnumerator()
	{
		Root.Publish();
		return Enumerate(Root);
	}

	/// <inheritdoc />
	/// <remarks>
	/// This method counts as a read access and may be called concurrently to other read accesses.
	/// </remarks>
	public override int IndexOf(T item)
	{
		return IndexOf(item, 0, Length);
	}

	/// <inheritdoc />
	/// <remarks>
	/// This method counts as a read access and may be called concurrently to other read accesses.
	/// </remarks>
	public override int IndexOf(T item, int index, int length)
	{
		if (!this.ValidRange(index, length))
		{
			return -1;
		}

		VerifyRange(index, length);

		while (length > 0)
		{
			var entry = FindNodeUsingCache(index).PeekOrDefault();
			var contents = entry.Node.contents;
			var startWithinNode = index - entry.NodeStartIndex;
			var nodeLength = Math.Min(entry.Node.length, startWithinNode + length);
			var r = Array.IndexOf(contents, item, startWithinNode, nodeLength - startWithinNode);
			if (r >= 0)
			{
				return entry.NodeStartIndex + r;
			}

			length -= nodeLength - startWithinNode;
			index = entry.NodeStartIndex + nodeLength;
		}

		return -1;
	}

	/// <summary>
	/// Gets the index of the first occurrence of any value in the provided values.
	/// </summary>
	/// <param name="anyOf"> The values to search for. </param>
	/// <returns> The index if found otherwise -1 if not found. </returns>
	public int IndexOfAnyReverse(T[] anyOf)
	{
		return IndexOfAnyReverse(anyOf, Length - 1);
	}

	/// <summary>
	/// Gets the index of the first occurrence of any value in the provided values.
	/// </summary>
	/// <param name="anyOf"> The values to search for. </param>
	/// <param name="index"> The index to start the search. </param>
	/// <returns> The index if found otherwise -1 if not found. </returns>
	public int IndexOfAnyReverse(T[] anyOf, int index)
	{
		if (!this.ValidRange(index, 0))
		{
			return -1;
		}

		if (anyOf.Contains(this[index]))
		{
			return index;
		}

		for (var i = index - 1; i >= 0; i--)
		{
			for (var j = 0; j < anyOf.Length; j++)
			{
				if (Equals(this[i], anyOf[j]))
				{
					return i;
				}
			}
		}

		return -1;
	}

	/// <inheritdoc />
	public override void Insert(int index, T item)
	{
		Insert(index, [item], 0, 1);
	}

	/// <inheritdoc />
	public override void Insert(int index, T[] items)
	{
		Insert(index, items, 0, items.Length);
	}

	/// <summary>
	/// Gets the index of the last occurrence of the specified item in this buffer.
	/// </summary>
	public int LastIndexOf(T item)
	{
		return LastIndexOf(item, 0, Length);
	}

	/// <summary>
	/// Gets the index of the last occurrence of the specified item in this buffer.
	/// </summary>
	/// <param name="item"> The search item </param>
	/// <param name="startIndex"> Start index of the area to search. </param>
	/// <param name="count"> Length of the area to search. </param>
	/// <returns> The last index where the item was found; or -1 if no occurrence was found. </returns>
	/// <remarks>
	/// The search proceeds backwards from (startIndex+count) to startIndex.
	/// This is different from the meaning of the parameters on Array.LastIndexOf!
	/// </remarks>
	public int LastIndexOf(T item, int startIndex, int count)
	{
		VerifyRange(startIndex, count);

		var comparer = System.Collections.Generic.EqualityComparer<T>.Default;
		for (var i = (startIndex + count) - 1; i >= startIndex; i--)
		{
			if (comparer.Equals(this[i], item))
			{
				return i;
			}
		}

		return -1;
	}

	/// <inheritdoc />
	/// <remarks>
	/// This method counts as a read access and may be called concurrently to other read accesses.
	/// </remarks>
	public override T[] Read(int index, int length)
	{
		VerifyRange(index, length);
		var newBuffer = Clone();
		var endIndex = index + length;
		var endLength = newBuffer.Length - endIndex;

		if (endLength > 0)
		{
			newBuffer.InternalRemove(endIndex, endLength);
		}

		if (index > 0)
		{
			newBuffer.InternalRemove(0, index);
		}

		return newBuffer.ToArray();
	}

	/// <inheritdoc />
	public override bool Remove(T item)
	{
		var index = IndexOf(item);
		if (index >= 0)
		{
			RemoveAt(index);
			return true;
		}

		return false;
	}

	/// <inheritdoc />
	public override void RemoveAt(int index)
	{
		RemoveRange(index, 1);
	}

	/// <inheritdoc />
	public override void RemoveRange(int index, int length)
	{
		if (index > End)
		{
			return;
		}

		if ((length + index) > End)
		{
			length = (End - index) + 1;
		}

		base.Replace(index, length, Array.Empty<T>());
	}

	/// <inheritdoc />
	public override void Replace(int index, int length, IEnumerable<T> values)
	{
		switch (values)
		{
			case RopeBuffer<T> buffer:
			{
				Replace(index, length, buffer);
				return;
			}
			default:
			{
				var array = values.ToArray();
				Replace(index, length, array, 0, array.Length);
				return;
			}
		}
	}

	/// <inheritdoc />
	public override void Replace(int index, int length, T[] values, int valueStart, int valueLength)
	{
		if (length > 0)
		{
			VerifyRange(index, length);
		}

		if (valueLength > 0)
		{
			values.BoundsCheckArray(valueStart, valueLength);
		}

		if (length > 0)
		{
			InternalRemove(index, length);
		}

		if (valueLength > 0)
		{
			InternalInsert(index, values, valueStart, valueLength);
		}

		ResetState();
	}

	/// <summary>
	/// Reset the buffer to the values provided.
	/// </summary>
	/// <param name="values"> The values to reset to. </param>
	public void Reset(RopeBuffer<T> values)
	{
		InternalReset();
		Replace(0, 0, values);
	}

	/// <summary>
	/// Reset the buffer to the values provided.
	/// </summary>
	/// <param name="values"> The values to reset to. </param>
	public void Reset(IEnumerable<T> values)
	{
		InternalReset();
		Replace(0, 0, values);
	}

	/// <summary>
	/// Reset the buffer to the values provided.
	/// </summary>
	/// <param name="values"> The values to reset to. </param>
	/// <param name="offset"> The offset in the values collection to start at. </param>
	/// <param name="length"> The length of data to copy from the values collection. </param>
	public void Reset(T[] values, int offset, int length)
	{
		InternalReset();
		Replace(0, 0, values, offset, length);
	}

	/// <summary>
	/// Creates an array and copies the contents of the buffer into it.
	/// Runs in O(N).
	/// </summary>
	/// <remarks>
	/// This method counts as a read access and may be called concurrently to other read accesses.
	/// </remarks>
	public T[] ToArray()
	{
		var arr = new T[Length];
		Root.CopyTo(0, arr, 0, arr.Length);
		return arr;
	}

	/// <summary>
	/// Creates an array and copies the contents of the buffer into it.
	/// Runs in O(N).
	/// </summary>
	/// <remarks>
	/// This method counts as a read access and may be called concurrently to other read accesses.
	/// </remarks>
	public T[] ToArray(int startIndex, int count)
	{
		VerifyRange(startIndex, count);
		var arr = new T[count];
		CopyTo(startIndex, arr, 0, count);
		return arr;
	}

	/// <inheritdoc />
	/// <remarks>
	/// This method counts as a read access and may be called concurrently to other read accesses.
	/// </remarks>
	public override string ToString()
	{
		if (Length == 0)
		{
			return string.Empty;
		}

		if (this is RopeBuffer<char> charBuffer)
		{
			var buffer = new char[Length];
			charBuffer.CopyTo(0, buffer, 0, Length);
			return new string(buffer);
		}

		var b = new TextBuilder();
		foreach (var element in this)
		{
			if (b.Length == 0)
			{
				b.Append('{');
			}
			else
			{
				b.Append(", ");
			}

			b.Append(element.ToString());
		}

		b.Append('}');
		return b.ToString();
	}

	/// <inheritdoc />
	object ICloneable.Clone()
	{
		return Clone();
	}

	private static IEnumerator<T> Enumerate(RopeBufferNode<T> node)
	{
		var stack = new Stack<RopeBufferNode<T>>();
		while (node != null)
		{
			// go to leftmost node, pushing the right parts that we'll have to visit later
			while (node.contents == null)
			{
				if (node.height == 0)
				{
					// go down into function nodes
					node = node.GetContentNode();
					continue;
				}

				Debug.Assert(node.right != null);
				stack.Push(node.right);
				node = node.left;
			}

			// yield contents of leaf node
			for (var i = 0; i < node.length; i++)
			{
				yield return node.contents[i];
			}

			// go up to the next node not visited yet
			node = stack.Count > 0 ? stack.Pop() : null;
		}
	}

	private ImmutableStack<BufferCacheEntry> FindNodeUsingCache(int index)
	{
		Debug.Assert((index >= 0) && (index < Length));

		// thread safety: fetch stack into local variable
		var stack = _lastUsedNodeStack;
		var oldStack = stack;

		if (stack == null)
		{
			stack = ImmutableStack<BufferCacheEntry>.Empty.Push(new BufferCacheEntry(Root, 0));
		}

		while (!stack.PeekOrDefault().IsInside(index))
		{
			stack = stack.Pop();

			if (stack.IsEmpty)
			{
				stack = ImmutableStack<BufferCacheEntry>.Empty.Push(new BufferCacheEntry(Root, 0));
			}
		}

		while (true)
		{
			var entry = stack.PeekOrDefault();

			// check if we've reached a leaf or function node
			if (entry.Node.height == 0)
			{
				if (entry.Node.contents == null)
				{
					// this is a function node - go down into its subtree
					entry = new BufferCacheEntry(entry.Node.GetContentNode(), entry.NodeStartIndex);

					// entry is now guaranteed NOT to be another function node
				}

				if (entry.Node.contents != null)
				{
					// this is a node containing actual content, so we're done
					break;
				}
			}

			// go down towards leaves
			stack = stack.Push((index - entry.NodeStartIndex) >= entry.Node.left.length
				? new BufferCacheEntry(entry.Node.right, entry.NodeStartIndex + entry.Node.left.length)
				: new BufferCacheEntry(entry.Node.left, entry.NodeStartIndex));
		}

		// write back stack to volatile cache variable
		// (in multi-threaded access, it doesn't matter which of the threads wins - it's just a cache)
		if (oldStack != stack)
		{
			// no need to write when we the cache variable didn't change
			_lastUsedNodeStack = stack;
		}

		// this method guarantees that it finds a leaf node
		Debug.Assert(stack.Peek().Node.contents != null);
		return stack;
	}

	private void InternalInsert(int index, IEnumerable<T> values)
	{
		switch (values)
		{
			case T[] array:
			{
				InternalInsert(index, array, 0, array.Length);
				break;
			}

			default:
			{
				var array = values.ToArray();
				InternalInsert(index, array, 0, array.Length);
				break;
			}
		}
	}

	private void InternalInsert(int index, T[] values, int valueStart, int valueLength)
	{
		Root = Root.Insert(index, values, valueStart, valueLength);
	}

	private void InternalRemove(int index, int length)
	{
		Root = Root.RemoveRange(index, length);
	}

	private void InternalReset()
	{
		Root = RopeBufferNode<T>.EmptyBufferNode;
	}

	private void Replace(int index, int length, RopeBuffer<T> buffer)
	{
		VerifyRange(index, length);

		if (length > 0)
		{
			InternalRemove(index, length);
		}

		if (buffer.Length > 0)
		{
			InternalInsert(index, buffer);
		}

		ResetState();
	}

	private void ResetState()
	{
		_lastUsedNodeStack = null;
		Root.CheckInvariants();
	}

	#endregion

	#region Structures

	internal struct BufferCacheEntry
	{
		#region Fields

		internal readonly int NodeStartIndex;

		#endregion

		#region Constructors

		internal BufferCacheEntry(RopeBufferNode<T> node, int nodeStartOffset)
		{
			Node = node ?? throw new ArgumentNullException(nameof(node));
			NodeStartIndex = nodeStartOffset;
		}

		#endregion

		#region Properties

		internal RopeBufferNode<T> Node { get; }

		#endregion

		#region Methods

		internal bool IsInside(int offset)
		{
			if (Node == null)
			{
				return false;
			}

			return (offset >= NodeStartIndex) && (offset < (NodeStartIndex + Node.length));
		}

		#endregion
	}

	#endregion
}