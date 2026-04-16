#region References

using System;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Text;

/// <summary>
/// Only good for buffer that moves forward. If you want
/// to insert or remove then please use StringGapBuffer
/// instead.
/// </summary>
public class StringBuffer : SpeedyList<char>, IStringBuffer
{
	#region Constructors

	public StringBuffer()
	{
	}

	public StringBuffer(string value)
	{
		Append(value);
	}

	#endregion

	#region Methods

	public void Append(string value)
	{
		Add(value.AsSpan());
	}

	public void AppendLine(string value)
	{
		Add(value.AsSpan());
		Add(Environment.NewLine);
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

	public string Substring(int index, int length)
	{
		return length == 0
			? string.Empty
			: new string(Read(index, length));
	}

	public override string ToString()
	{
		return Count == 0
			? string.Empty
			: new string(Read(0, Count));
	}

	#endregion
}

public interface IStringBuffer
{
	#region Properties

	int Count { get; }

	char this[int index] { get; set; }

	#endregion

	#region Methods

	void Append(string value);

	void AppendLine(string value);

	void Clear();

	bool Equals(int index, ReadOnlySpan<char> value);

	string Substring(int index, int length);

	string ToString();

	#endregion
}