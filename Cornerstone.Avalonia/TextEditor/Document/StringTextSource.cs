#region References

using System;
using System.IO;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Document;

/// <summary>
/// Implements the ITextSource interface using a string.
/// </summary>
public class StringTextSource : ITextSource
{
	#region Fields

	/// <summary>
	/// Gets a text source containing the empty string.
	/// </summary>
	public static readonly StringTextSource Empty = new(string.Empty);

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new StringTextSource with the given text.
	/// </summary>
	public StringTextSource(string text)
	{
		Text = text ?? throw new ArgumentNullException(nameof(text));
	}

	/// <summary>
	/// Creates a new StringTextSource with the given text.
	/// </summary>
	public StringTextSource(string text, ITextSourceVersion version)
	{
		Text = text ?? throw new ArgumentNullException(nameof(text));
		Version = version;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public string Text { get; }

	/// <inheritdoc />
	public int TextLength => Text.Length;

	/// <inheritdoc />
	public ITextSourceVersion Version { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public TextReader CreateReader()
	{
		return new StringReader(Text);
	}

	/// <inheritdoc />
	public TextReader CreateReader(int offset, int length)
	{
		return new StringReader(Text.Substring(offset, length));
	}

	/// <inheritdoc />
	public ITextSource CreateSnapshot()
	{
		return this; // StringTextSource is immutable
	}

	/// <inheritdoc />
	public ITextSource CreateSnapshot(int offset, int length)
	{
		return new StringTextSource(Text.Substring(offset, length));
	}

	/// <inheritdoc />
	public char GetCharAt(int offset)
	{
		return Text[offset];
	}

	/// <inheritdoc />
	public string GetText(int offset, int length)
	{
		return Text.Substring(offset, length);
	}

	/// <inheritdoc />
	public string GetText(IRange range)
	{
		if (range == null)
		{
			throw new ArgumentNullException(nameof(range));
		}
		return Text.Substring(range.StartIndex, range.Length);
	}

	/// <inheritdoc />
	public int IndexOf(char c, int startIndex, int count)
	{
		return Text.IndexOf(c, startIndex, count);
	}

	/// <inheritdoc />
	public int IndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
	{
		return Text.IndexOf(searchText, startIndex, count, comparisonType);
	}

	/// <inheritdoc />
	public int IndexOfAny(char[] anyOf, int startIndex, int count)
	{
		return Text.IndexOfAny(anyOf, startIndex, count);
	}

	/// <inheritdoc />
	public int LastIndexOf(char c, int startIndex, int count)
	{
		return Text.LastIndexOf(c, (startIndex + count) - 1, count);
	}

	public int LastIndexOf(string searchText, StringComparison comparisonType)
	{
		return LastIndexOf(searchText, 0, TextLength, comparisonType);
	}

	/// <inheritdoc />
	public int LastIndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
	{
		return Text.LastIndexOf(searchText, (startIndex + count) - 1, count, comparisonType);
	}

	/// <inheritdoc />
	public void WriteTextTo(TextWriter writer)
	{
		writer.Write(Text);
	}

	/// <inheritdoc />
	public void WriteTextTo(TextWriter writer, int offset, int length)
	{
		writer.Write(Text.Substring(offset, length));
	}

	#endregion
}