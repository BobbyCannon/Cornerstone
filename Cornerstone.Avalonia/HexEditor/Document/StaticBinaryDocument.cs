#region References

using System;
using System.Linq;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Document;

/// <summary>
/// Represents a binary document that is a static length.
/// </summary>
public class StaticBinaryDocument : IBinaryDocument
{
	#region Fields

	private readonly GapBuffer<byte> _buffer;
	private readonly BitRangeUnion _validRanges;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new empty static binary document.
	/// </summary>
	public StaticBinaryDocument(int size) : this(new byte[size])
	{
	}

	/// <summary>
	/// Creates a new static binary document with the provided initial data.
	/// </summary>
	/// <param name="initialData"> The data to initialize the document with. </param>
	public StaticBinaryDocument(byte[] initialData)
	{
		_buffer = new GapBuffer<byte>(initialData);
		_validRanges = new BitRangeUnion([new BitRange(0ul, (ulong) initialData.Length)]);

		CanInsert = false;
		CanRemove = false;
		ValidRanges = _validRanges.AsReadOnly();
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public bool CanInsert { get; set; }

	/// <inheritdoc />
	public bool CanRemove { get; set; }

	/// <inheritdoc />
	public bool IsReadOnly { get; set; }

	/// <inheritdoc />
	public ulong Length => (ulong) _buffer.Count;

	/// <inheritdoc />
	public IReadOnlyBitRangeUnion ValidRanges { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public void Dispose()
	{
	}

	/// <inheritdoc />
	public void Flush()
	{
	}

	/// <inheritdoc />
	public void Insert(ulong offset, ReadOnlySpan<byte> buffer)
	{
		AssertIsWriteable();

		if (!CanInsert)
		{
			throw new InvalidOperationException("Data cannot be inserted into the document.");
		}

		_buffer.InsertRange((int) offset, buffer.ToArray());
		_validRanges.Add(new BitRange(_validRanges.EnclosingRange.End, _validRanges.EnclosingRange.End.AddBytes((ulong) buffer.Length)));

		OnChanged(new BinaryDocumentChange(BinaryDocumentChangeType.Insert, new BitRange(offset, offset + (ulong) buffer.Length)));
	}

	/// <inheritdoc />
	public void Read(ulong offset, Span<byte> buffer)
	{
		var array = _buffer.Read((int) offset, buffer.Length);
		array.AsSpan().CopyTo(buffer);
	}

	/// <inheritdoc />
	public void Remove(ulong offset, ulong length)
	{
		AssertIsWriteable();

		if (!CanRemove)
		{
			throw new InvalidOperationException("Data cannot be removed from the document.");
		}

		_buffer.Remove((int) offset, (int) length);
		_validRanges.Remove(new BitRange(_validRanges.EnclosingRange.End.SubtractBytes(length), _validRanges.EnclosingRange.End));

		OnChanged(new BinaryDocumentChange(BinaryDocumentChangeType.Remove, new BitRange(offset, offset + length)));
	}

	/// <summary>
	/// Serializes the contents of the document into a byte array.
	/// </summary>
	/// <returns> The serialized contents. </returns>
	public byte[] ToArray()
	{
		return _buffer.ToArray();
	}

	/// <inheritdoc />
	public void Write(ulong offset, ReadOnlySpan<byte> buffer)
	{
		AssertIsWriteable();

		var array = buffer.ToArray();
		_buffer.Write((int) offset, array, 0, array.Length);

		OnChanged(new BinaryDocumentChange(BinaryDocumentChangeType.Modify, new BitRange(offset, offset + (ulong) buffer.Length)));
	}

	/// <summary>
	/// Fires the <see cref="Changed" /> event.
	/// </summary>
	/// <param name="e"> The event arguments describing the change. </param>
	protected virtual void OnChanged(BinaryDocumentChange e)
	{
		Changed?.Invoke(this, e);
	}

	private void AssertIsWriteable()
	{
		if (IsReadOnly)
		{
			throw new InvalidOperationException("Document is read-only.");
		}
	}

	#endregion

	#region Events

	/// <inheritdoc />
	public event EventHandler<BinaryDocumentChange> Changed;

	#endregion
}