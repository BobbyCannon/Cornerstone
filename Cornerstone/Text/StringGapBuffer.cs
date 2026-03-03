#region References

using System;
using System.Linq;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Text;

/// <summary>
/// A gap buffer that represents a string.
/// </summary>
public class StringGapBuffer : GapBuffer<char>
{
	#region Constructors

	public StringGapBuffer()
		: this(Span<char>.Empty)
	{
	}

	public StringGapBuffer(uint capacity)
		: this(null, capacity)
	{
	}

	public StringGapBuffer(ReadOnlySpan<char> value, uint capacity = 0)
		: base(capacity, value)
	{
	}

	#endregion

	#region Methods

	public virtual void Add(string value)
	{
		base.Add(value.AsSpan());
	}

	public void Insert(int index, string value)
	{
		var array = value.AsSpan();
		InternalInsert(index, array, 0, array.Length);
	}

	public string SubString(int index, int length)
	{
		return length == 0
			? string.Empty
			: new string(Read(index, length).ToArray());
	}

	public override string ToString()
	{
		return SubString(0, Count);
	}

	#endregion
}