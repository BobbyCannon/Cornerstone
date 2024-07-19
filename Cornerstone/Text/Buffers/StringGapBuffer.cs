#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;
using ICloneable = Cornerstone.Data.ICloneable;

#endregion

namespace Cornerstone.Text.Buffers;

/// <summary>
/// A gap buffer that represents a string.
/// </summary>
public class StringGapBuffer : GapBuffer<char>, ICloneable<StringGapBuffer>, IStringBuffer
{
	#region Constructors

	/// <inheritdoc />
	public StringGapBuffer() : this(string.Empty)
	{
	}

	/// <inheritdoc />
	public StringGapBuffer(IEnumerable<char> value) : this(new string(value.ToArray()))
	{
	}

	/// <inheritdoc />
	public StringGapBuffer(string value) : base(value)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public void Append(char value)
	{
		Add(value);
	}

	/// <inheritdoc />
	public virtual void Append(string value)
	{
		AddRange(value);
	}

	/// <inheritdoc />
	public StringGapBuffer DeepClone(int? maxDepth = null, IncludeExcludeOptions options = null)
	{
		return new StringGapBuffer(ToString());
	}

	public int IndexOf(string item, int index, int length, StringComparison comparisonType)
	{
		VerifyRange(index, length);

		var comparer = comparisonType.GetComparer();
		for (var i = index; i < (index + length); i++)
		{
			if (comparer.Compare(this[i], item) == 0)
			{
				return i;
			}
		}

		return -1;
	}

	/// <inheritdoc />
	public void Insert(int index, string value)
	{
		InsertRange(index, value);
	}

	/// <inheritdoc />
	public void Insert(int index, IStringBuffer value)
	{
		InsertRange(index, value);
	}

	/// <inheritdoc />
	public void Insert(int index, string value, int valueStart, int valueLength)
	{
		Insert(index, value.ToCharArray(), valueStart, valueLength);
	}

	/// <inheritdoc />
	public int LastIndexOf(string item, int index)
	{
		var itemIndex = item.Length - 1;

		for (var i = index; i >= 0; i--)
		{
			if (this[i] == item[itemIndex])
			{
				if (itemIndex == 0)
				{
					return i;
				}

				itemIndex--;
			}
			else
			{
				itemIndex = item.Length - 1;
			}
		}

		return -1;
	}

	public int LastIndexOf(string item, int index, int length, StringComparison comparisonType)
	{
		VerifyRange(index, length);

		var comparer = comparisonType.GetComparer();
		for (var i = (index + length) - 1; i >= index; i--)
		{
			if (comparer.Compare(this[i], item) == 0)
			{
				return i;
			}
		}

		return -1;
	}

	/// <inheritdoc />
	public StringGapBuffer ShallowClone(IncludeExcludeOptions options = null)
	{
		return DeepClone(0, options);
	}

	/// <inheritdoc />
	public string SubString(int index, int length)
	{
		return new string(GetRange(index, length).ToArray());
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return SubString(0, Count);
	}

	/// <inheritdoc />
	public void WriteTo(TextWriter writer, int index, int length)
	{
		writer.Write(SubString(index, length));
	}

	/// <inheritdoc />
	IStringBuffer ICloneable<IStringBuffer>.DeepClone(int? maxDepth, IncludeExcludeOptions options)
	{
		return DeepClone(maxDepth, options);
	}

	/// <inheritdoc />
	object ICloneable.DeepCloneObject(int? maxDepth, IncludeExcludeOptions options)
	{
		return DeepClone(maxDepth, options);
	}

	/// <inheritdoc />
	IStringBuffer ICloneable<IStringBuffer>.ShallowClone(IncludeExcludeOptions options)
	{
		return ShallowClone(options);
	}

	/// <inheritdoc />
	object ICloneable.ShallowCloneObject(IncludeExcludeOptions options)
	{
		return ShallowClone(options);
	}

	#endregion
}