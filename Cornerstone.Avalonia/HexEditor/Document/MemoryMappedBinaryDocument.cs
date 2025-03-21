#region References

using System;
using System.IO;
using System.IO.MemoryMappedFiles;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Document;

/// <summary>
/// Represents a binary document that is backed by a file that is mapped into memory.
/// </summary>
public class MemoryMappedBinaryDocument : IBinaryDocument
{
	#region Fields

	private readonly MemoryMappedViewAccessor _accessor;
	private readonly bool _leaveOpen;

	#endregion

	#region Constructors

	/// <summary>
	/// Opens a file as a memory mapped document.
	/// </summary>
	/// <param name="filePath"> The file to memory map. </param>
	public MemoryMappedBinaryDocument(string filePath)
		: this(MemoryMappedFile.CreateFromFile(filePath, FileMode.OpenOrCreate), false, false)
	{
	}

	/// <summary>
	/// Wraps a memory mapped file in a document.
	/// </summary>
	/// <param name="file"> The file to use as a backing storage. </param>
	/// <param name="leaveOpen"> <c> true </c> if <paramref name="file" /> should be kept open on disposing, <c> false </c> otherwise. </param>
	public MemoryMappedBinaryDocument(MemoryMappedFile file, bool leaveOpen)
		: this(file, leaveOpen, false)
	{
	}

	/// <summary>
	/// Wraps a memory mapped file in a document.
	/// </summary>
	/// <param name="file"> The file to use as a backing storage. </param>
	/// <param name="leaveOpen"> <c> true </c> if <paramref name="file" /> should be kept open on disposing, <c> false </c> otherwise. </param>
	/// <param name="isReadOnly"> <c> true </c> if the document can be edited, <c> false </c> otherwise. </param>
	public MemoryMappedBinaryDocument(MemoryMappedFile file, bool leaveOpen, bool isReadOnly)
	{
		File = file;

		_leaveOpen = leaveOpen;
		_accessor = file.CreateViewAccessor();

		// Yuck! But this seems to be the only way to get the length from a MemoryMappedFile.
		using var stream = file.CreateViewStream();
		Length = (ulong) stream.Length;

		ValidRanges = new BitRangeUnion([new BitRange(0, Length)]).AsReadOnly();
		IsReadOnly = isReadOnly;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public bool CanInsert => false;

	/// <inheritdoc />
	public bool CanRemove => false;

	/// <summary>
	/// Gets the underlying memory mapped file that is used as a backing storage for this document.
	/// </summary>
	public MemoryMappedFile File { get; }

	/// <inheritdoc />
	public bool IsReadOnly { get; }

	/// <inheritdoc />
	public ulong Length { get; }

	/// <inheritdoc />
	public IReadOnlyBitRangeUnion ValidRanges { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public void Dispose()
	{
		_accessor.Dispose();

		if (!_leaveOpen)
		{
			File.Dispose();
		}
	}

	/// <inheritdoc />
	public void Flush()
	{
		_accessor.Flush();
	}

	/// <inheritdoc />
	public void Insert(ulong offset, ReadOnlySpan<byte> buffer)
	{
		if (IsReadOnly)
		{
			throw new InvalidOperationException("Document is read-only.");
		}

		throw new InvalidOperationException("Document cannot be resized.");
	}

	/// <inheritdoc />
	public void Read(ulong offset, Span<byte> buffer)
	{
		_accessor.SafeMemoryMappedViewHandle.ReadSpan(offset, buffer);
	}

	/// <inheritdoc />
	public void Remove(ulong offset, ulong length)
	{
		if (IsReadOnly)
		{
			throw new InvalidOperationException("Document is read-only.");
		}

		throw new InvalidOperationException("Document cannot be resized.");
	}

	/// <inheritdoc />
	public void Write(ulong offset, ReadOnlySpan<byte> buffer)
	{
		if (IsReadOnly)
		{
			throw new InvalidOperationException("Document is read-only.");
		}

		_accessor.SafeMemoryMappedViewHandle.WriteSpan(offset, buffer);

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

	#endregion

	#region Events

	/// <inheritdoc />
	public event EventHandler<BinaryDocumentChange> Changed;

	#endregion
}