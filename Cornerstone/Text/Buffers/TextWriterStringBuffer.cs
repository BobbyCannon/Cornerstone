#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cornerstone.Data;
using ICloneable = Cornerstone.Data.ICloneable;

#endregion

namespace Cornerstone.Text.Buffers;

/// <inheritdoc />
public class TextWriterStringBuffer : IStringBuffer
{
	#region Fields

	private readonly TextWriter _writer;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize the stream string buffer.
	/// </summary>
	/// <param name="writer"> </param>
	public TextWriterStringBuffer(TextWriter writer)
	{
		_writer = writer;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public int Count { get; }

	/// <inheritdoc />
	public bool IsReadOnly { get; }

	/// <inheritdoc />
	public char this[int index]
	{
		get => throw new NotImplementedException();
		set => throw new NotImplementedException();
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public void Add(char item)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public void Append(char value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public void Append(string value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public void BoundsCheckArray(char[] array, int index, int length)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public void Clear()
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public bool Contains(char item)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public void CopyTo(char[] array, int arrayIndex)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public IStringBuffer DeepClone(int? maxDepth = null, IncludeExcludeOptions options = null)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public IEnumerator<char> GetEnumerator()
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public int IndexOf(char item)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public int IndexOfAny(char[] anyOf)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public int IndexOfAny(char[] anyOf, int index, int count)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public int IndexOfAnyReverse(char[] anyOf)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public int IndexOfAnyReverse(char[] anyOf, int index)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public void Insert(int index, char item)
	{
		_writer.Write(item);
	}

	/// <inheritdoc />
	public void Insert(int index, string value)
	{
		_writer.Write(value);
	}

	/// <inheritdoc />
	public void Insert(int index, IStringBuffer value)
	{
		_writer.Write(value);
	}

	/// <inheritdoc />
	public void Insert(int index, char[] value, int valueStart, int valueLength)
	{
		_writer.Write(value);
	}

	/// <inheritdoc />
	public void Insert(int index, string value, int valueStart, int valueLength)
	{
		_writer.Write(value);
	}

	/// <inheritdoc />
	public int LastIndexOf(string item, int index)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public bool Remove(char item)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public void RemoveAt(int index)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public void RemoveRange(int index, int length)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public IStringBuffer ShallowClone(IncludeExcludeOptions options = null)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public string SubString(int index, int length)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return _writer.ToString();
	}

	/// <inheritdoc />
	public void VerifyRange(int index)
	{
	}

	/// <inheritdoc />
	public void VerifyRange(int index, int length)
	{
	}

	/// <inheritdoc />
	public void WriteTo(TextWriter writer, int index, int length)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	object ICloneable.DeepCloneObject(int? maxDepth, IncludeExcludeOptions options)
	{
		return DeepClone(maxDepth);
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	/// <inheritdoc />
	object ICloneable.ShallowCloneObject(IncludeExcludeOptions options)
	{
		return ShallowClone(options);
	}

	#endregion
}