#region References

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media.TextFormatting;
using Cornerstone.Avalonia.Text.Input;
using Cornerstone.Avalonia.Text.Models;
using Cornerstone.Avalonia.Text.Rendering;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Avalonia.Text;

/// <summary>
/// What does the editor need to know?
/// - Line Foldings
/// - Additional Cursors
/// - Inline Snippet
/// - Multiple Snippets (rectangle selection)
/// - Smart options
/// </summary>
[SourceReflection]
public partial class TextEditorViewModel : Notifiable<TextEditorViewModel>
{
	#region Constructors

	public TextEditorViewModel()
	{
		Buffer = new StringGapBuffer(16384);
		Caret = new(this);
		Clipboard = new ClipboardManager(this);
		CompletionManager = new CompletionManager(this);
		Lines = new LineManager(this);
		IndentionManager = new IndentionManager(this);
		InputManager = new InputManager(this);
		TokenManager = new TokenManager(this);
		UndoManager = new UndoManager(this);
		ViewMetrics = new ViewMetrics();

		HighlightCurrentLine = true;
		ShowLineNumbers = true;

		Load(string.Empty);
	}

	#endregion

	#region Properties

	/// <summary>
	/// The carets for the editor.
	/// </summary>
	public Caret Caret { get; }

	public ClipboardManager Clipboard { get; }

	public CompletionManager CompletionManager { get; }

	/// <summary>
	/// The length of the document.
	/// </summary>
	public int DocumentLength => Buffer.Count;

	/// <summary>
	/// The option to highlight the current line.
	/// </summary>
	[Notify]
	public partial bool HighlightCurrentLine { get; set; }

	public IndentionManager IndentionManager { get; }

	public InputManager InputManager { get; }

	/// <summary>
	/// The lines of the document.
	/// </summary>
	public LineManager Lines { get; }

	/// <summary>
	/// An optional profiler.
	/// </summary>
	public Profiler Profiler { get; set; }

	/// <summary>
	/// The option to show line numbers.
	/// </summary>
	[Notify]
	public partial bool ShowLineNumbers { get; set; }

	public TokenManager TokenManager { get; set; }

	public UndoManager UndoManager { get; }

	/// <summary>
	/// The option to wrap text.
	/// </summary>
	[Notify]
	public partial bool WordWrap { get; set; }

	/// <summary>
	/// The character buffer for the document.
	/// </summary>
	internal StringGapBuffer Buffer { get; }

	/// <summary>
	/// Represents the visual details.
	/// </summary>
	internal ViewMetrics ViewMetrics { get; }

	#endregion

	#region Methods

	public void Append(string message)
	{
		var offset = Buffer.Count;
		Buffer.Append(message);
		OnDocumentChanged(offset, message, TextDocumentChangeType.Add);
	}

	public void Clear()
	{
		Load(string.Empty);
	}

	public void ConfigureForFileType(string fileExtension)
	{
		CompletionManager.Initialize(fileExtension);
		IndentionManager.Initialize(fileExtension);
		TokenManager.Initialize(fileExtension);
	}

	public int Delete(int offset, bool forward)
	{
		if (TryRemoveSelection(out var removed))
		{
			return removed;
		}

		return forward
			? DeleteForward(offset)
			: DeleteBackwards(offset);
	}

	/// <summary>
	/// Increase indentation by one level on the current line or all lines in the selection.
	/// </summary>
	public void Indent()
	{
		if (Caret.Selection.Length > 0)
		{
			IndentSelection();
		}
		else
		{
			IndentAtCaret();
		}
	}

	public void Insert(GapBuffer<char> builder)
	{
		var text = builder.ToString();
		Insert(text);
	}

	public void Insert(char value)
	{
		Insert(new string([value]));
	}

	public void Insert(string value)
	{
		var offset = Caret.Offset;
		Insert(offset, value);
	}

	public void Insert(int offset, string value)
	{
		Buffer.Insert(offset, value);
		OnDocumentChanged(offset, value, TextDocumentChangeType.Add);
	}

	public void Load(string data)
	{
		Buffer.Reset(data);
		OnDocumentChanged(0, null, TextDocumentChangeType.Reset);
	}

	public void Measure(TextLayout line, Size availableSize)
	{
		ViewMetrics.CharacterHeight = line.Height;
		ViewMetrics.CharacterWidth = Math.Max(1, line.WidthIncludingTrailingWhitespace);
		ViewMetrics.DocumentSize = Lines.Measure(availableSize, WordWrap);
		ViewMetrics.Viewport = availableSize;
		Caret.UpdateVisualLayout();
	}

	public void ProcessKeyDownEvent(KeyEventArgs args)
	{
		Caret.Selection.ProcessKeyDown(args);
		InputManager.ProcessKeyArgs(args);
	}

	public void ProcessKeyUpEvent(KeyEventArgs args)
	{
		Caret.Selection.ProcessKeyUp(args);
	}

	public void ProcessTextInput(TextInputEventArgs args)
	{
		if (string.IsNullOrEmpty(args.Text))
		{
			return;
		}

		TryRemoveSelection(out _);
		var offset = Caret.Offset;
		Insert(offset, args.Text);
		Caret.Move(offset + args.Text.Length);
	}

	public void RemoveAt(int offset, int length)
	{
		var removed = Buffer.Substring(offset, length);
		Buffer.RemoveAt(offset, length);
		OnDocumentChanged(offset, removed, TextDocumentChangeType.Remove);
	}

	public void SelectAllText()
	{
		Caret.Selection.Update(0, Buffer.Count);
	}

	public void SelectWord(int offset)
	{
		// Find word boundaries (very fast string scan)
		var start = offset;
		var end = offset;

		// Go left until non-word char or start
		while ((start > 0) && IsWordChar(Buffer[start - 1]))
		{
			start--;
		}

		// Go right until non-word char or end
		while ((end < Buffer.Count) && IsWordChar(Buffer[end]))
		{
			end++;
		}

		Caret.Move(end);
		Caret.Selection.Reset(start);
		Caret.Selection.Update(start, end);
	}

	public override string ToString()
	{
		return Buffer.ToString();
	}

	/// <summary>
	/// Reduces indentation by one level on the current line or all lines in the selection.
	/// </summary>
	public void Unindent()
	{
		if (Caret.Selection.Length > 0)
		{
			UnindentSelection();
		}
		else
		{
			UnindentCurrentLine();
		}
	}

	protected virtual void OnDocumentChanged(int offset, ReadOnlySpan<char> text, TextDocumentChangeType type)
	{
		var textString = text.IsEmpty ? null : text.ToString();
		OnDocumentChanged(offset, textString, type);
	}

	protected virtual void OnDocumentChanged(int offset, string text, TextDocumentChangeType type)
	{
		using var _ = ProfilerExtensions.Start(Profiler, nameof(DocumentChanged));
		var args = new TextDocumentChangedArgs(offset, text, type);
		if (args.Type is TextDocumentChangeType.Reset)
		{
			Caret.Reset();
			UndoManager.Clear();
		}
		else if (UndoManager.Enabled)
		{
			UndoManager.Add(args);
		}
		Lines.Rebuild(args);
		TokenManager.Rebuild(args);
		OnPropertyChanged(nameof(DocumentLength));
		OnPropertyChanged(nameof(UndoManager));
		DocumentChanged?.Invoke(this, args);
	}

	internal void HandleEnterKey()
	{
		// Insert the newline first (this creates the new line)
		var offset = Caret.Offset;
		Insert(offset, "\r\n");
		Caret.Move(offset + 2);

		// Try to apply smart indentation on the new line
		var newLineOffset = Caret.Offset;

		if (IndentionManager.TryGetIndention(newLineOffset, out var indent)
			&& !indent.IsEmpty)
		{
			// Insert the indentation at the beginning of the new line
			// Note: Insert() already moves the caret to the end of what was inserted
			Insert(newLineOffset, indent.ToString());
			Caret.Move(newLineOffset + indent.Length);
		}
	}

	private int CalculateUnindentAmount(string leadingWhitespace, string indentStr)
	{
		if (string.IsNullOrEmpty(leadingWhitespace))
		{
			return 0;
		}

		if (leadingWhitespace.StartsWith(indentStr))
		{
			return indentStr.Length;
		}

		// Fallback: remove up to one indent's worth of whitespace
		var count = 0;
		var maxRemove = indentStr.Length;
		while ((count < leadingWhitespace.Length)
				&& char.IsWhiteSpace(leadingWhitespace[count])
				&& (count < maxRemove))
		{
			count++;
		}

		return count;
	}

	private int DeleteBackwards(int caretOffset)
	{
		if (caretOffset <= 0)
		{
			return 0;
		}

		var offset = caretOffset - 1;

		if ((Buffer[offset] == '\n')
			&& (offset > 0)
			&& (Buffer[offset - 1] == '\r'))
		{
			offset--;
			Buffer.RemoveAt(offset, 2);
			OnDocumentChanged(offset, "\r\n", TextDocumentChangeType.Remove);
			Caret.Move(offset);
			return 2;
		}

		var removed = Buffer.Substring(offset, 1);
		Buffer.RemoveAt(offset, 1);
		OnDocumentChanged(offset, removed, TextDocumentChangeType.Remove);
		Caret.Move(offset);
		return 1;
	}

	private int DeleteForward(int caretOffset)
	{
		if (caretOffset >= Buffer.Count)
		{
			return 0;
		}

		if ((Buffer[caretOffset] == '\r')
			&& ((caretOffset + 1) < Buffer.Count)
			&& (Buffer[caretOffset + 1] == '\n'))
		{
			Buffer.RemoveAt(caretOffset, 2);
			OnDocumentChanged(caretOffset, "\r\n", TextDocumentChangeType.Remove);
			Caret.Move(caretOffset);
			return 2;
		}
		var deleted = Buffer.Substring(caretOffset, 1);
		Buffer.RemoveAt(caretOffset, 1);
		OnDocumentChanged(caretOffset, deleted, TextDocumentChangeType.Remove);
		Caret.Move(caretOffset);
		return 1;
	}

	private void IndentAtCaret()
	{
		var indent = IndentionManager.IndentString;
		if (string.IsNullOrEmpty(indent))
		{
			return;
		}

		var offset = Caret.Offset;
		var line = Lines.GetLineFromOffset(offset);

		// Determine if we are still in the leading whitespace area
		var posInLine = offset - line.StartOffset;
		var whiteSpaceEnd = 0;
		while ((whiteSpaceEnd < line.Length) && char.IsWhiteSpace(Buffer[line.StartOffset + whiteSpaceEnd]))
		{
			whiteSpaceEnd++;
		}

		var isInLeadingWhitespace = posInLine <= whiteSpaceEnd;
		if (isInLeadingWhitespace)
		{
			// Smart whole-line indent: add at the true beginning of the line
			Buffer.Insert(offset, indent);
			OnDocumentChanged(offset, indent, TextDocumentChangeType.Add);

			// Keep the caret at the same *visual* column (relative to content)
			// This feels natural and makes undo predictable
			var newCaretOffset = offset + indent.Length;
			Caret.Move(newCaretOffset);
		}
		else
		{
			// Caret is inside actual code, just insert indent string at caret position
			// (useful for aligning comments, etc.)
			Insert(indent);
		}
	}

	private void IndentSelection()
	{
		var indentStr = IndentionManager.IndentString;
		if (string.IsNullOrEmpty(indentStr))
		{
			return;
		}

		var startOffset = Math.Min(Caret.Selection.StartOffset, Caret.Selection.EndOffset);
		var endOffset = Math.Max(Caret.Selection.StartOffset, Caret.Selection.EndOffset);

		var firstLine = Lines.GetLineFromOffset(startOffset);
		var lastLine = Lines.GetLineFromOffset(Math.Max(endOffset - 1, 0));

		var changes = new List<(int offset, string text)>();
		var totalAdded = 0;
		var addedToStart = 0; // amount inserted at/before original start

		// Bottom-up so line.StartOffset stays valid during inserts
		for (var i = lastLine.LineNumber; i >= firstLine.LineNumber; i--)
		{
			if (!Lines.TryGetLine(i, out var line) || (line.Length == 0))
			{
				continue;
			}

			var lineStart = line.StartOffset;
			changes.Add((lineStart, indentStr));

			totalAdded += indentStr.Length;

			if (lineStart <= startOffset)
			{
				// only first line's indent affects start
				addedToStart += indentStr.Length;
			}
		}

		if (changes.Count == 0)
		{
			return;
		}

		try
		{
			UndoManager.IsProcessing = true;
			var compoundChanges = new TextDocumentChangedArgs[changes.Count];

			for (var i = 0; i < changes.Count; i++)
			{
				var (offset, text) = changes[i];
				Buffer.Insert(offset, text);

				var args = new TextDocumentChangedArgs(offset, text, TextDocumentChangeType.Add);
				compoundChanges[i] = args;
				OnDocumentChanged(offset, text, TextDocumentChangeType.Add);
			}

			UndoManager.AddCompound(compoundChanges);
		}
		finally
		{
			UndoManager.IsProcessing = false;
		}

		// Selection grows by indent on every line (standard behavior)
		var newStart = startOffset + addedToStart; // start moves right by first indent
		var newEnd = endOffset + totalAdded; // end expands by all indents

		Caret.Selection.Update(newStart, newEnd);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	private static bool IsWordChar(char c)
	{
		// todo: customize this based on the type of document
		return char.IsLetterOrDigit(c) || (c == '_') || (c == '-');
	}

	internal bool TryRemoveSelection(out int removed)
	{
		if ((Caret.Selection.Length <= 0)
			|| UndoManager.IsProcessing)
		{
			removed = 0;
			return false;
		}

		// Delete the selection
		var offset = Math.Min(Caret.Selection.StartOffset, Caret.Selection.EndOffset);
		removed = Caret.Selection.Length;

		var selection = Buffer.Substring(offset, removed);
		Buffer.RemoveAt(offset, removed);
		Caret.Move(offset);
		Caret.Selection.Reset(offset);
		OnDocumentChanged(offset, selection, TextDocumentChangeType.Remove);
		return true;
	}

	private void UnindentCurrentLine()
	{
		var indentStr = IndentionManager.IndentString;
		if (string.IsNullOrEmpty(indentStr))
		{
			return;
		}

		var offset = Caret.Offset;
		var line = Lines.GetLineFromOffset(offset);
		if (line.Length == 0)
		{
			return;
		}

		// Find how much leading whitespace exists
		var whiteSpaceEnd = 0;
		while ((whiteSpaceEnd < line.Length) && char.IsWhiteSpace(Buffer[line.StartOffset + whiteSpaceEnd]))
		{
			whiteSpaceEnd++;
		}
		if (whiteSpaceEnd == 0)
		{
			return;
		}

		var leading = Buffer.Substring(line.StartOffset, whiteSpaceEnd);
		var removeCount = CalculateUnindentAmount(leading, indentStr);
		if (removeCount == 0)
		{
			return;
		}

		// This makes unindent symmetric with your new "insert at caret" indent behavior
		var removeOffset = line.StartOffset;

		// If caret is inside the leading whitespace, try to remove from the caret backwards
		var posInLine = offset - line.StartOffset;
		if ((posInLine > 0) && (posInLine <= whiteSpaceEnd))
		{
			// Prefer removing a chunk that ends at the caret position when it makes sense
			// (this feels more natural when repeatedly Shift+Tab)
			removeOffset = Math.Max(line.StartOffset, offset - removeCount);
		}

		var removedText = Buffer.Substring(removeOffset, removeCount);
		Buffer.RemoveAt(removeOffset, removeCount);
		OnDocumentChanged(removeOffset, removedText, TextDocumentChangeType.Remove);

		// Keep caret on same relative column when possible
		var newOffset = offset - removeCount;
		Caret.Move(Math.Max(line.StartOffset, newOffset));
	}

	private void UnindentSelection()
	{
		var indentStr = IndentionManager.IndentString;
		if (string.IsNullOrEmpty(indentStr))
		{
			return;
		}

		var startOffset = Math.Min(Caret.Selection.StartOffset, Caret.Selection.EndOffset);
		var endOffset = Math.Max(Caret.Selection.StartOffset, Caret.Selection.EndOffset);

		var firstLine = Lines.GetLineFromOffset(startOffset);
		var lastLine = Lines.GetLineFromOffset(Math.Max(endOffset - 1, 0));

		var changes = new List<(int offset, int length)>();
		var totalRemoved = 0;
		var removedFromStart = 0;

		// Bottom-up for removes
		for (var i = lastLine.LineNumber; i >= firstLine.LineNumber; i--)
		{
			if (!Lines.TryGetLine(i, out var line) || (line.Length == 0))
			{
				continue;
			}

			var wsEnd = 0;
			while ((wsEnd < line.Length) && char.IsWhiteSpace(Buffer[line.StartOffset + wsEnd]))
			{
				wsEnd++;
			}

			if (wsEnd == 0)
			{
				continue;
			}

			var leading = Buffer.Substring(line.StartOffset, wsEnd);
			var removeCount = CalculateUnindentAmount(leading, indentStr);

			if (removeCount > 0)
			{
				changes.Add((line.StartOffset, removeCount));
				totalRemoved += removeCount;

				if ((line.StartOffset <= startOffset) && (removedFromStart == 0))
				{
					// only first line affects start
					removedFromStart = removeCount;
				}
			}
		}

		if (changes.Count == 0)
		{
			return;
		}

		try
		{
			UndoManager.IsProcessing = true;
			var compoundChanges = new TextDocumentChangedArgs[changes.Count];

			for (var i = 0; i < changes.Count; i++)
			{
				var (offset, length) = changes[i];
				var removed = Buffer.Substring(offset, length);
				Buffer.RemoveAt(offset, length);

				var args = new TextDocumentChangedArgs(offset, removed, TextDocumentChangeType.Remove);
				compoundChanges[i] = args;
				OnDocumentChanged(offset, removed, TextDocumentChangeType.Remove);
			}

			UndoManager.AddCompound(compoundChanges);
		}
		finally
		{
			UndoManager.IsProcessing = false;
		}

		// Selection shrinks symmetrically, start only by first-line removal
		var newStart = Math.Max(0, startOffset - removedFromStart);
		var newEnd = Math.Max(0, endOffset - totalRemoved);

		Caret.Selection.Update(newStart, newEnd);
	}

	#endregion

	#region Events

	public event EventHandler<TextDocumentChangedArgs> DocumentChanged;

	#endregion
}