#region References

using System;
using System.IO;
using System.Text;

#endregion

namespace Cornerstone.Text.Document;

/// <summary>
/// A TextWriter implementation that directly inserts into a document.
/// </summary>
public class DocumentTextWriter : TextWriter
{
	#region Fields

	private readonly ITextEditorDocument _document;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new DocumentTextWriter that inserts into document, starting at insertionOffset.
	/// </summary>
	public DocumentTextWriter(ITextEditorDocument document, int insertionOffset)
	{
		InsertionOffset = insertionOffset;
		_document = document ?? throw new ArgumentNullException(nameof(document));
		var line = document.GetLineByOffset(insertionOffset);
		if (line.DelimiterLength == 0)
		{
			line = line.PreviousLine;
		}
		if (line != null)
		{
			// ReSharper disable once VirtualMemberCallInConstructor
			NewLine = document.GetText(line.EndIndex, line.DelimiterLength);
		}
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override Encoding Encoding => Encoding.UTF8;

	/// <summary>
	/// Gets/Sets the current insertion offset.
	/// </summary>
	public int InsertionOffset { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Write(char value)
	{
		_document.Insert(InsertionOffset, value.ToString());
		InsertionOffset++;
	}

	/// <inheritdoc />
	public override void Write(char[] buffer, int index, int count)
	{
		_document.Insert(InsertionOffset, new string(buffer, index, count));
		InsertionOffset += count;
	}

	/// <inheritdoc />
	public override void Write(string value)
	{
		_document.Insert(InsertionOffset, value);
		InsertionOffset += value.Length;
	}

	#endregion
}