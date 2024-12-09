﻿#region References

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
	public StringGapBuffer(IEnumerable<char> value) : this(value == null ? string.Empty : new string(value.ToArray()))
	{
	}

	/// <inheritdoc />
	public StringGapBuffer(string value) : base(value)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public virtual void Add(string value)
	{
		base.Add(value);
	}

	/// <inheritdoc />
	public StringGapBuffer DeepClone(int? maxDepth = null, IncludeExcludeSettings settings = null)
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
	public int LastIndexOf(int index, string item)
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

	public bool Match(int index, char value)
	{
		VerifyRange(index);

		if (this[index] == value)
		{
			return true;
		}

		return false;
	}

	/// <inheritdoc />
	public bool Match(int index, string value, StringComparison comparisonType)
	{
		var length = value.Length;

		VerifyRange(index);

		var comparer = comparisonType.GetComparer();

		for (var i = 0; i < length; i++)
		{
			var offset = index + i;
			if (offset >= Count)
			{
				return false;
			}

			if (comparer.Compare(this[offset], value[i]) != 0)
			{
				return false;
			}
		}

		return true;
	}

	/// <inheritdoc />
	public bool MatchAny(int index, char[] values, StringComparison comparison, out int length)
	{
		VerifyRange(index);

		var comparer = comparison.GetComparer();
		var offset = index;
		var matches = 0;

		while (offset <= Count)
		{
			if (values.Any(x => comparer.Compare(x, this[offset]) == 0))
			{
				matches++;
				continue;
			}

			break;
		}

		length = matches;
		return matches > 0;
	}

	/// <inheritdoc />
	public StringGapBuffer ShallowClone(IncludeExcludeSettings settings = null)
	{
		return DeepClone(0, settings);
	}

	/// <inheritdoc />
	public string SubString(int index)
	{
		var length = Count - index;
		return SubString(index, length);
	}

	/// <inheritdoc />
	public string SubString(int index, int length)
	{
		if (length == 0)
		{
			return string.Empty;
		}

		VerifyRange(index, length);
		return new string(Read(index, length).ToArray());
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
	IStringBuffer ICloneable<IStringBuffer>.DeepClone(int? maxDepth, IncludeExcludeSettings settings)
	{
		return DeepClone(maxDepth, settings);
	}

	/// <inheritdoc />
	object ICloneable.DeepCloneObject(int? maxDepth, IncludeExcludeSettings settings)
	{
		return DeepClone(maxDepth, settings);
	}

	/// <inheritdoc />
	IStringBuffer ICloneable<IStringBuffer>.ShallowClone(IncludeExcludeSettings settings)
	{
		return ShallowClone(settings);
	}

	/// <inheritdoc />
	object ICloneable.ShallowCloneObject(IncludeExcludeSettings settings)
	{
		return ShallowClone(settings);
	}

	#endregion
}