#region References

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
	void Append(char value);

	/// <summary>
	/// Append the value.
	/// </summary>
	/// <param name="value"> The value to append to the document. </param>
	/// <returns> The text document. </returns>
	void Append(string value);

	/// <summary>
	/// Gets the index of the first occurrence of any character in the specified array.
	/// </summary>
	/// <param name="anyOf"> Characters to search for </param>
	/// <returns> The first index where any character was found otherwise -1 if not found. </returns>
	int IndexOfAny(char[] anyOf);

	/// <summary>
	/// Gets the index of the first occurrence of any character in the specified array.
	/// </summary>
	/// <param name="anyOf"> Characters to search for </param>
	/// <param name="index"> The index to start at. </param>
	/// <param name="count"> Length of the area to search. </param>
	/// <returns> The first index where any character was found otherwise -1 if not found. </returns>
	int IndexOfAny(char[] anyOf, int index, int count);

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
	/// Insert a value into the builder.
	/// </summary>
	/// <param name="index"> The index to insert into. </param>
	/// <param name="value"> The value to insert. </param>
	void Insert(int index, string value);

	/// <summary>
	/// Insert a value into the builder.
	/// </summary>
	/// <param name="index"> The index to insert into. </param>
	/// <param name="value"> The value to insert. </param>
	void Insert(int index, IStringBuffer value);

	/// <summary>
	/// Inserts multiple items from a subset of items into the collection.
	/// </summary>
	void Insert(int index, char[] value, int valueStart, int valueLength);

	/// <summary>
	/// Inserts multiple items from a subset of items into the collection.
	/// </summary>
	void Insert(int index, string value, int valueStart, int valueLength);

	/// <summary>
	/// Gets the index of the last occurrence of the specified item in this rope.
	/// </summary>
	/// <param name="item"> The item to search for. </param>
	/// <param name="index"> The index to start at. </param>
	/// <returns> The last index where the item was found otherwise -1 if not found. </returns>
	int LastIndexOf(string item, int index);

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