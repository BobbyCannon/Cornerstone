#region References

using System.IO;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Text;

/// <summary>
/// A gap buffer that represents a string.
/// </summary>
public class StringGapBuffer : GapBuffer<char>, IStringBuffer
{
	#region Constructors

	/// <inheritdoc />
	public StringGapBuffer() : this(string.Empty)
	{
	}

	/// <inheritdoc />
	public StringGapBuffer(char[] value) : this(new string(value))
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
	public IStringBuffer DeepClone(int? maxDepth = null)
	{
		return ShallowClone();
	}

	/// <inheritdoc />
	public string GetString(int index, int length)
	{
		return new string(GetRange(index, length).ToArray());
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

	/// <inheritdoc />
	public IStringBuffer ShallowClone()
	{
		return new StringGapBuffer(ToString());
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return GetString(0, Count);
	}

	/// <inheritdoc />
	public void WriteTo(TextWriter writer, int index, int length)
	{
		writer.Write(GetString(index, length));
	}

	/// <inheritdoc />
	object ICloneable.DeepClone(int? maxDepth)
	{
		return DeepClone(maxDepth);
	}

	/// <inheritdoc />
	object ICloneable.ShallowClone()
	{
		return ShallowClone();
	}

	#endregion
}