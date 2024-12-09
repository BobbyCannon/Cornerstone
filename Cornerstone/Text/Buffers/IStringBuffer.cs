#region References

using System;
using System.IO;
using Cornerstone.Collections;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Text.Buffers;

/// <summary>
/// Represents a string buffer.
/// </summary>
public interface IStringBuffer : IBuffer<char>, ICloneable<IStringBuffer>
{
	#region Methods

	/// <summary>
	/// Append the value.
	/// </summary>
	/// <param name="value"> The value to append to the document. </param>
	/// <returns> The text document. </returns>
	void Add(string value);

	/// <summary>
	/// Gets the index of the first occurrence of any character in the specified array from the index provided in reverse.
	/// </summary>
	/// <param name="anyOf"> Characters to search for </param>
	/// <returns> The first index where any character was found otherwise -1 if not found. </returns>
	int IndexOfAnyReverse(char[] anyOf);

	/// <summary>
	/// Gets the index of the first occurrence of any character in the specified array from the index provided in reverse.
	/// </summary>
	/// <param name="anyOf"> Characters to search for </param>
	/// <param name="index"> The index to start at. </param>
	/// <returns> The first index where any character was found otherwise -1 if not found. </returns>
	int IndexOfAnyReverse(char[] anyOf, int index);

	/// <summary>
	/// Insert a string into the buffer.
	/// </summary>
	/// <param name="index"> The index to insert into. </param>
	/// <param name="value"> The value to insert. </param>
	void Insert(int index, string value);

	/// <summary>
	/// Insert a buffer into the buffer.
	/// </summary>
	/// <param name="index"> The index to insert into. </param>
	/// <param name="value"> The value to insert. </param>
	void Insert(int index, IStringBuffer value);

	/// <summary>
	/// Gets the index of the last occurrence of the specified item in this rope.
	/// </summary>
	/// <param name="index"> The index to start at. </param>
	/// <param name="item"> The item to search for. </param>
	/// <returns> The last index where the item was found otherwise -1 if not found. </returns>
	int LastIndexOf(int index, string item);

	/// <summary>
	/// Check to see if buffer matches value at provided index.
	/// </summary>
	/// <param name="index"> The index to start at. </param>
	/// <param name="value"> The value to search for. </param>
	/// <param name="comparisonType"> The comparison to use when matching. </param>
	/// <returns> True if the buffer matches the value otherwise false. </returns>
	bool Match(int index, string value, StringComparison comparisonType);

	/// <summary>
	/// Check to see if buffer matches value at provided index.
	/// </summary>
	/// <param name="index"> The index to start at. </param>
	/// <param name="values"> The values to search for. </param>
	/// <param name="comparison"> The comparison to use when matching. </param>
	/// <param name="length"> The length of text to read. </param>
	/// <returns> True if the buffer matches the value otherwise false. </returns>
	bool MatchAny(int index, char[] values, StringComparison comparison, out int length);

	/// <summary>
	/// Gets a range of text from the buffer.
	/// </summary>
	/// <param name="index"> The index to start at. </param>
	/// <returns> The text. </returns>
	string SubString(int index);

	/// <summary>
	/// Gets a range of text from the buffer.
	/// </summary>
	/// <param name="index"> The index to start at. </param>
	/// <param name="length"> The length of text to read. </param>
	/// <returns> The text. </returns>
	string SubString(int index, int length);

	/// <summary>
	/// Write the buffer to a provided writer.
	/// </summary>
	/// <param name="writer"> The writer to write to. </param>
	/// <param name="index"> The index to start at. </param>
	/// <param name="length"> The length to write. </param>
	void WriteTo(TextWriter writer, int index, int length);

	#endregion
}