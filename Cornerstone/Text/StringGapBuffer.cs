#region References

using System;
using System.Linq;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Text;

/// <summary>
/// A gap buffer that represents a string.
/// </summary>
public class StringGapBuffer : GapBuffer<char>, IStringBuffer
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

	public virtual void Append(string value)
	{
		base.Add(value.AsSpan());
	}

	public virtual void AppendLine(string value)
	{
		base.Add(value.AsSpan());
		base.Add(Environment.NewLine);
	}

	public bool Equals(int index, ReadOnlySpan<char> value)
	{
		for (var i = 0; i < value.Length; i++)
		{
			if ((i + index) >= Count)
			{
				return false;
			}

			if (this[i + index] != value[i])
			{
				return false;
			}
		}

		return true;
	}

	public void Insert(int index, ReadOnlySpan<char> value)
	{
		InternalInsert(index, value, 0, value.Length);
	}
	
	public void Insert(int index, string value)
	{
		var array = value.AsSpan();
		InternalInsert(index, array, 0, array.Length);
	}

	public void Insert(int index, GapBuffer<char> builder)
	{
		var spans = builder.GetReadOnlySpans(0, builder.Count);
		InternalInsert(index, spans.BeforeGap, 0, spans.BeforeGap.Length);
		InternalInsert(index, spans.AfterGap, 0, spans.AfterGap.Length);
	}

	public string Substring(int index, int length)
	{
		return length == 0
			? string.Empty
			: new string(Read(index, length).ToArray());
	}

	public override string ToString()
	{
		return Substring(0, Count);
	}

	#endregion
}