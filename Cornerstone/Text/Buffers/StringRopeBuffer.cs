#region References

using System.IO;
using Cornerstone.Collections;
using Cornerstone.Data;

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

	/// <inheritdoc />
	public void Append(char value)
	{
		Add(value);
	}

	/// <inheritdoc />
	public void Append(string value)
	{
		AddRange(value);
	}

	/// <inheritdoc />
	public IStringBuffer DeepClone(int? maxDepth = null)
	{
		return ShallowClone();
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
	public void Insert(int index, char[] value, int valueStart, int valueLength)
	{
		InsertRange(index, value, valueStart, valueLength);
	}

	/// <inheritdoc />
	public void Insert(int index, string value, int valueStart, int valueLength)
	{
		InsertRange(index, value.ToCharArray(), valueStart, valueLength);
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
		return new StringRopeBuffer(ToString());
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
	object ICloneable.DeepCloneObject(int? maxDepth)
	{
		return DeepClone(maxDepth);
	}

	/// <inheritdoc />
	object ICloneable.ShallowCloneObject()
	{
		return ShallowClone();
	}

	#endregion
}