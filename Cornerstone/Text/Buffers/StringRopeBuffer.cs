#region References

using System;
using System.IO;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;
using ICloneable = Cornerstone.Data.ICloneable;

#endregion

namespace Cornerstone.Text.Buffers;

/// <summary>
/// A rope buffer that represents a string.
/// </summary>
public class StringRopeBuffer : RopeBuffer<char>, IStringBuffer
{
	#region Constructors

	/// <inheritdoc />
	public StringRopeBuffer() : this(string.Empty)
	{
	}

	/// <inheritdoc />
	public StringRopeBuffer(char[] value) : this(new string(value))
	{
	}

	/// <inheritdoc />
	public StringRopeBuffer(string value) : base(value)
	{
	}

	#endregion

	#region Methods

	public void Add(string value)
	{
		base.Add(value);
	}

	/// <inheritdoc />
	public IStringBuffer DeepClone(int? maxDepth = null, IncludeExcludeSettings settings = null)
	{
		return ShallowClone(settings);
	}

	/// <inheritdoc />
	public char FirstOrDefault()
	{
		return Count > 0 ? this[0] : '\0';
	}

	/// <inheritdoc />
	public int IndexOf(string value)
	{
		return IndexOf(value, 0, Count, StringComparison.Ordinal);
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
		Insert(index, value.ToCharArray());
	}

	/// <inheritdoc />
	public void Insert(int index, IStringBuffer value)
	{
		base.Insert(index, value);
	}

	/// <inheritdoc />
	public int LastIndexOf(string item, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
	{
		return LastIndexOf(item, 0, Count, comparisonType);
	}

	/// <inheritdoc />
	public int LastIndexOf(string item, int index, int length, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
	{
		VerifyRange(index, length);

		var startIndex = (index + length) - item.Length;
		var endIndex = index;

		for (var i = startIndex; i >= endIndex; i--)
		{
			if (Match(i, item, comparisonType))
			{
				return i;
			}
		}

		return -1;
	}

	/// <inheritdoc />
	public char LastOrDefault()
	{
		return Count > 0 ? this[Count - 1] : '\0';
	}

	public bool Match(int index, string desired, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
	{
		var length = desired.Length;

		VerifyRange(index, length);

		var comparer = comparisonType.GetComparer();

		for (var i = 0; i < length; i++)
		{
			if (comparer.Compare(this[index + i], desired[i]) != 0)
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
	public void Replace(int index, int length, string value)
	{
		Remove(index, length);
		Insert(index, value);
	}

	/// <inheritdoc />
	public IStringBuffer ShallowClone(IncludeExcludeSettings settings = null)
	{
		return new StringRopeBuffer(ToString());
	}

	/// <inheritdoc />
	public string SubString(int index)
	{
		var length = Count - index;
		return SubString(index, length);
	}

	public string SubString(int index, int length)
	{
		return new string(base.Read(index, length));
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return SubString(0, Count);
	}

	/// <inheritdoc />
	public void WriteTo(TextWriter writer, int index, int length)
	{
		writer.Write(Read(index, length));
	}

	/// <inheritdoc />
	object ICloneable.DeepCloneObject(int? maxDepth, IncludeExcludeSettings settings)
	{
		return DeepClone(maxDepth, settings);
	}

	/// <inheritdoc />
	object ICloneable.ShallowCloneObject(IncludeExcludeSettings settings)
	{
		return ShallowClone(settings);
	}

	#endregion
}