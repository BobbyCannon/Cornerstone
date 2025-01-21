#region References

using System;
using Cornerstone.Text.Buffers;
#if !NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif

#endregion

namespace Cornerstone.Text;

/// <summary>
/// Represents a plain text builder.
/// </summary>
public class TextBuilder : TextBuilder<ITextBuilderSettings>
{
	#region Constructors

	/// <summary>
	/// Initialize the document.
	/// </summary>
	public TextBuilder() : this(string.Empty)
	{
	}

	/// <summary>
	/// Initialize the document.
	/// </summary>
	public TextBuilder(string text) : this(text, new TextBuilderSettings())
	{
	}

	/// <summary>
	/// Initialize the document.
	/// </summary>
	public TextBuilder(ITextBuilderSettings settings) : this(string.Empty, settings)
	{
	}

	/// <summary>
	/// Initialize the document.
	/// </summary>
	public TextBuilder(string text, ITextBuilderSettings settings) : base(text, settings)
	{
	}

	#endregion
}

/// <summary>
/// Represents a plain text builder.
/// </summary>
public class TextBuilder<T> : ITextBuilder
	where T : ITextBuilderSettings
{
	#region Fields

	private readonly IStringBuffer _buffer;
	private readonly IStringBuffer _currentIndent;
	private bool _endsWithNewline;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize the document.
	/// </summary>
	public TextBuilder(string text, T settings)
		: this(new StringGapBuffer(text), settings)
	{
	}

	/// <summary>
	/// Initialize the document.
	/// </summary>
	public TextBuilder(IStringBuffer buffer, T settings)
		: this(new StringGapBuffer(buffer), settings)
	{
	}

	/// <summary>
	/// Initialize the document.
	/// </summary>
	public TextBuilder(StringGapBuffer buffer, T settings)
	{
		_buffer = buffer ?? new StringGapBuffer();
		_currentIndent = new StringGapBuffer();
		_endsWithNewline = false;

		Settings = settings ?? Activator.CreateInstance<T>();
		IndentToken = '\t';
		IndentCount = 1;
		NewLineToken = Environment.NewLine;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Represents the quantity of characters per indentation.
	/// </summary>
	public int IndentCount { get; set; }

	/// <summary>
	/// Represents the indent character.
	/// </summary>
	public char IndentToken { get; set; }

	/// <summary>
	/// The length of the document.
	/// </summary>
	public int Length => _buffer.Count;

	/// <summary>
	/// Presents the character(s) for new line.
	/// </summary>
	public string NewLineToken { get; set; }

	/// <summary>
	/// Settings for the builder.
	/// </summary>
	public T Settings { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public ITextBuilder Append(char value)
	{
		InternalUpdate(_buffer.Count, value, false);
		return this;
	}

	/// <inheritdoc />
	public virtual ITextBuilder Append(string value)
	{
		InternalUpdate(_buffer.Count, value, false);
		return this;
	}

	/// <summary>
	/// Append the value with a <see cref="NewLineToken" />.
	/// </summary>
	/// <param name="value"> The value to append to the document. </param>
	/// <returns> The text document. </returns>
	public TextBuilder<T> AppendLine(char value)
	{
		InternalUpdate(_buffer.Count, value, true);
		return this;
	}

	/// <summary>
	/// Append the value with a <see cref="NewLineToken" />.
	/// </summary>
	/// <returns> The text document. </returns>
	public ITextBuilder AppendLine()
	{
		return NewLine();
	}

	/// <summary>
	/// Append the value with a <see cref="NewLineToken" />.
	/// </summary>
	/// <param name="value"> The value to append to the document. </param>
	/// <returns> The text document. </returns>
	public ITextBuilder AppendLine(string value)
	{
		InternalUpdate(_buffer.Count, value, true);
		return this;
	}

	/// <summary>
	/// Append the value with a <see cref="NewLineToken" /> then decrease indent.
	/// </summary>
	/// <param name="value"> The value to append to the document. </param>
	/// <returns> The text document. </returns>
	public TextBuilder<T> AppendLineThenPopIndent(char value)
	{
		InternalUpdate(_buffer.Count, value, true);
		PopIndent();
		return this;
	}

	/// <summary>
	/// Append the value with a <see cref="NewLineToken" /> then decrease indent.
	/// </summary>
	/// <param name="value"> The value to append to the document. </param>
	/// <returns> The text document. </returns>
	public ITextBuilder AppendLineThenPopIndent(string value = null)
	{
		InternalUpdate(_buffer.Count, value, true);
		PopIndent();
		return this;
	}

	/// <summary>
	/// Append the value with a <see cref="NewLineToken" /> then increment indent.
	/// </summary>
	/// <param name="value"> The value to append to the document. </param>
	/// <returns> The text document. </returns>
	public ITextBuilder AppendLineThenPushIndent(char value)
	{
		InternalUpdate(_buffer.Count, value, true);
		PushIndent();
		return this;
	}

	/// <summary>
	/// Append the value with a <see cref="NewLineToken" /> then increment indent.
	/// </summary>
	/// <param name="value"> The value to append to the document. </param>
	/// <returns> The text document. </returns>
	public ITextBuilder AppendLineThenPushIndent(string value = null)
	{
		InternalUpdate(_buffer.Count, value, true);
		PushIndent();
		return this;
	}

	/// <summary>
	/// Clear the builder. Does not affect <see cref="IndentToken" /> state.
	/// </summary>
	public void Clear()
	{
		_currentIndent.Clear();
		_buffer.Clear();
		_endsWithNewline = false;
	}

	/// <summary>
	/// The buffer ends with a newline.
	/// </summary>
	/// <returns> True if so otherwise false. </returns>
	public bool EndsWithNewLine()
	{
		return _endsWithNewline;
	}

	/// <summary>
	/// Fill the builder with the provided value.
	/// </summary>
	/// <param name="value"> The value to fill with. </param>
	/// <param name="offset"> The offset to start with. </param>
	/// <param name="length"> The amount of characters to fill. </param>
	public void Fill(char value, int offset, int length)
	{
		for (var i = 0; i < length; i++)
		{
			var bufferOffset = offset + i;
			if (bufferOffset < _buffer.Count)
			{
				_buffer[bufferOffset] = value;
				continue;
			}

			Append(value);
		}
	}

	public int IndexOf(string startComment)
	{
		return _buffer.IndexOf(startComment);
	}

	/// <summary>
	/// Insert a value into the builder.
	/// </summary>
	/// <param name="index"> The index to insert into. </param>
	/// <param name="value"> The value to insert. </param>
	public void Insert(int index, string value)
	{
		if (value != null)
		{
			_buffer.Insert(index, value);
		}
	}

	/// <inheritdoc />
	public virtual ITextBuilder NewLine()
	{
		InternalUpdate(_buffer.Count, string.Empty, true);
		return this;
	}

	/// <summary>
	/// Decrease indent.
	/// </summary>
	public ITextBuilder PopIndent()
	{
		if (Settings.TextFormat == TextFormat.None)
		{
			return this;
		}
		if (_currentIndent.Count > 0)
		{
			_currentIndent.RemoveAt(0);
		}
		return this;
	}

	/// <summary>
	/// Decrease indent then append the value with a <see cref="NewLineToken" />.
	/// </summary>
	/// <param name="value"> The value to append to the document. </param>
	/// <returns> The text document. </returns>
	public ITextBuilder PopIndentThenAppendLine(string value = null)
	{
		PopIndent();
		InternalUpdate(_buffer.Count, value, true);
		return this;
	}

	/// <summary>
	/// Increases indent.
	/// </summary>
	public ITextBuilder PushIndent()
	{
		if (Settings.TextFormat
			is TextFormat.None
			or TextFormat.Spaced)
		{
			return this;
		}

		_currentIndent.Add(IndentToken);
		return this;
	}

	/// <summary>
	/// Remove a range of text from the builder.
	/// </summary>
	/// <param name="index"> The index to start removing from. </param>
	/// <param name="length"> The length of text to remove. </param>
	public ITextBuilder Remove(int index, int length)
	{
		_buffer.Remove(index, length);
		return this;
	}

	/// <inheritdoc />
	public void Replace(int index, int length, string value)
	{
		_buffer.Replace(index, length, value);
	}

	/// <summary>
	/// Set the builder offset with the provided value.
	/// </summary>
	/// <param name="value"> The value to be set. </param>
	/// <param name="offset"> The offset to be updated. </param>
	public void Set(char value, int offset)
	{
		if ((offset < 0) || (offset >= _buffer.Count))
		{
			throw new ArgumentOutOfRangeException(nameof(offset));
		}

		_buffer[offset] = value;
	}

	/// <inheritdoc />
	#if (!NETSTANDARD)
	[return: NotNull]
	#endif
	public override string ToString()
	{
		return _buffer.ToString();
	}

	/// <summary>
	/// Trim the white space from the builder.
	/// </summary>
	public ITextBuilder Trim()
	{
		while ((_buffer.Count > 0) && char.IsWhiteSpace(_buffer[0]))
		{
			_buffer.RemoveAt(0);
		}

		while ((_buffer.Count > 0) && char.IsWhiteSpace(_buffer[_buffer.Count - 1]))
		{
			_buffer.RemoveAt(_buffer.Count - 1);
		}

		_endsWithNewline = false;
		return this;
	}

	public void WriteIndent()
	{
		WriteIndent(Length - 1);
	}

	private void InternalNewLine(int index)
	{
		switch (Settings.TextFormat)
		{
			case TextFormat.Indented:
			{
				_buffer.Insert(index, NewLineToken);
				_endsWithNewline = true;
				break;
			}
			case TextFormat.Spaced:
			{
				_buffer.Insert(index, ' ');
				_endsWithNewline = false;
				break;
			}
		}
	}

	private void InternalUpdate(int index, char value, bool newLine)
	{
		index = WriteIndent(index);

		_buffer.Insert(index, value);
		_endsWithNewline = false;

		if (newLine)
		{
			InternalNewLine(index + 1);
		}
	}

	private void InternalUpdate(int index, string value, bool newLine)
	{
		if (value is { Length: > 0 })
		{
			index = WriteIndent(index);

			_buffer.Insert(index, value);
		}

		_endsWithNewline = _buffer.LastOrDefault() is '\r' or '\n';

		if (newLine)
		{
			InternalNewLine(index + (value?.Length ?? 0));
		}
	}

	private int WriteIndent(int index)
	{
		if ((_currentIndent.Count > 0) && _endsWithNewline)
		{
			_buffer.Insert(index, _currentIndent);
			index += _currentIndent.Count;
			_endsWithNewline = false;
		}
		return index;
	}

	#endregion
}

/// <summary>
/// Represents a plain text builder.
/// </summary>
public interface ITextBuilder
{
	#region Methods

	/// <summary>
	/// Append the value.
	/// </summary>
	/// <param name="value"> The value to append to the document. </param>
	/// <returns> The text document. </returns>
	ITextBuilder Append(char value);

	/// <summary>
	/// Append the value.
	/// </summary>
	/// <param name="value"> The value to append to the document. </param>
	/// <returns> The text document. </returns>
	ITextBuilder Append(string value);

	/// <summary>
	/// Append the value then a newline.
	/// </summary>
	/// <returns> The text document. </returns>
	ITextBuilder AppendLine();

	/// <summary>
	/// Append the value then a newline.
	/// </summary>
	/// <param name="value"> The value to append to the document. </param>
	/// <returns> The text document. </returns>
	ITextBuilder AppendLine(string value);

	/// <summary>
	/// Append the value with a new line then decrease indent.
	/// </summary>
	/// <param name="value"> The value to append to the document. </param>
	/// <returns> The text document. </returns>
	ITextBuilder AppendLineThenPopIndent(string value = null);

	/// <summary>
	/// Append the value with a newline then increment indent.
	/// </summary>
	/// <param name="value"> The value to append to the document. </param>
	/// <returns> The text document. </returns>
	ITextBuilder AppendLineThenPushIndent(char value);

	/// <summary>
	/// Append the value with a newline then increment indent.
	/// </summary>
	/// <param name="value"> The value to append to the document. </param>
	/// <returns> The text document. </returns>
	ITextBuilder AppendLineThenPushIndent(string value = null);

	/// <summary>
	/// Write a new line.
	/// </summary>
	ITextBuilder NewLine();

	/// <summary>
	/// Decrease indent.
	/// </summary>
	ITextBuilder PopIndent();

	/// <summary>
	/// Decrease indent then append the value with a new line value.
	/// </summary>
	/// <param name="value"> The value to append to the document. </param>
	/// <returns> The text document. </returns>
	ITextBuilder PopIndentThenAppendLine(string value = null);

	/// <summary>
	/// Increases indent.
	/// </summary>
	ITextBuilder PushIndent();

	/// <summary>
	/// Replace a section of text with a new value.
	/// </summary>
	/// <param name="index"> The start index to read from. </param>
	/// <param name="length"> The length of text to read. </param>
	/// <param name="value"> The value to replace with. </param>
	void Replace(int index, int length, string value);

	#endregion
}