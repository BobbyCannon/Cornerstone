#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Media.TextFormatting;
using Cornerstone.Presentation.Controls.Text.Folding;
using Cornerstone.Presentation.Controls.Text.Input;
using Cornerstone.Presentation.Controls.Text.Models;
using Cornerstone.Presentation.Controls.Text.Rendering;
using Cornerstone.Collections;
using InputManager = Cornerstone.Presentation.Controls.Text.Input.InputManager;
using Cornerstone.Data;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Text;
using Cornerstone.Text.Formatting;
using Cornerstone.Text.Parsing;
using Range = Cornerstone.Collections.Range;

#endregion

namespace Cornerstone.Presentation.Controls.Text;

/// <summary>
/// What does the editor need to know?
/// - Line Foldings
/// - Inline Snippet
/// - Multiple Snippets (rectangle selection)
/// - Smart options
/// </summary>
[SourceReflection]
[Updateable(UpdateableAction.All, ["*"])]
public partial class TextEditorViewModel : CornerstoneObject<TextEditorViewModel>
{
	#region Constructors

	public TextEditorViewModel()
	{
		Buffer = new StringGapBuffer(16384);
		Lines = new LineManager(this);
		Carets = new CaretManager(this);
		Clipboard = new ClipboardManager(this);
		CompletionManager = new CompletionManager(this);
		Diagnostics = new DiagnosticManager(this);
		FoldingManager = new FoldingManager(this);
		IndentionManager = new IndentionManager(this);
		InputManager = new InputManager(this);
		TokenManager = new TokenManager(this);
		UndoManager = new UndoManager(this);
		ViewMetrics = new ViewMetrics();

		HighlightCurrentLine = true;
		ShowCaret = true;
		ShowLineNumbers = true;

		Load(string.Empty);
	}

	#endregion

	#region Properties

	/// <summary>
	/// When true, document growth keeps the viewport pinned to the bottom.
	/// Cleared when the user scrolls away from the bottom.
	/// </summary>
	[Notify]
	public partial bool AutoScroll { get; set; }

	/// <summary>
	/// The primary caret. Same instance as Carets.Primary.
	/// </summary>
	public Caret Caret => Carets.Primary;

	/// <summary>
	/// All carets. v1 UI still uses a single caret; extras are for later PRs.
	/// </summary>
	public CaretManager Carets { get; }

	public ClipboardManager Clipboard { get; }

	public CompletionManager CompletionManager { get; }

	public DiagnosticManager Diagnostics { get; }

	/// <summary>
	/// Bumped when <see cref="Diagnostics" /> is replaced so the renderer can repaint squiggles.
	/// </summary>
	[Notify]
	public partial int DiagnosticVersion { get; set; }

	/// <summary>
	/// The length of the document.
	/// </summary>
	public int DocumentLength => Buffer.Count;

	public FoldingManager FoldingManager { get; }

	/// <summary>
	/// The option to highlight the current line.
	/// </summary>
	[Notify]
	public partial bool HighlightCurrentLine { get; set; }

	public IndentionManager IndentionManager { get; }

	public InputManager InputManager { get; }

	/// <summary>
	/// When true, the document cannot be mutated (cut, paste, typing).
	/// </summary>
	[Notify]
	public partial bool IsReadOnly { get; set; }

	/// <summary>
	/// True while caret/scroll is being restored. Caret moves must not scroll into
	/// view until ViewMetrics.Offset is applied.
	/// </summary>
	internal bool IsRestoringViewport { get; set; }

	/// <summary>
	/// True when the last document change asked the view to pin the viewport
	/// (insert-before-prompt) instead of scrolling to the end.
	/// </summary>
	public bool LastChangePinnedViewport { get; private set; }

	/// <summary>
	/// The lines of the document.
	/// </summary>
	public LineManager Lines { get; }

	/// <summary>
	/// An optional profiler.
	/// </summary>
	public Profiler Profiler { get; set; }

	/// <summary>
	/// Gets/Sets an object that provides read-only sections for the text area.
	/// </summary>
	public IReadOnlySectionProvider ReadOnlySectionProvider { get; set; }

	/// <summary>
	/// When false, the caret is never drawn (selection and keyboard navigation still work).
	/// Use for read-only surfaces such as <c> MarkdownView </c>.
	/// </summary>
	[Notify]
	public partial bool ShowCaret { get; set; }

	/// <summary>
	/// The option to show line numbers.
	/// </summary>
	[Notify]
	public partial bool ShowLineNumbers { get; set; }

	public TokenManager TokenManager { get; set; }

	public UndoManager UndoManager { get; }

	/// <summary>
	/// Represents the visual details.
	/// </summary>
	public ViewMetrics ViewMetrics { get; }

	/// <summary>
	/// The option to wrap text.
	/// </summary>
	[Notify]
	public partial bool WordWrap { get; set; }

	/// <summary>
	/// The character buffer for the document.
	/// </summary>
	public StringGapBuffer Buffer { get; }

	#endregion

	#region Methods

	public void Append(string message)
	{
		if (string.IsNullOrEmpty(message))
		{
			return;
		}

		Append(message.AsSpan());
	}

	public void Append(ReadOnlySpan<char> text)
	{
		if (text.IsEmpty)
		{
			return;
		}

		var offset = Buffer.Count;
		Buffer.Append(text);
		OnDocumentChanged(offset, text, TextDocumentChangeType.Add);
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

	public bool FormatDocument(DocumentFormatOptions options)
	{
		if (IsReadOnly || (options == null) || !TokenManager.HasTokenizer)
		{
			return false;
		}

		var tokens = TokenManager.Count > 0 ? TokenManager : null;
		if (!DocumentFormatter.TryFormat(TokenManager.Tokenizer, Buffer, tokens, options, out var formatted)
			|| string.Equals(formatted, Buffer.ToString(), StringComparison.Ordinal))
		{
			return false;
		}

		var original = Buffer.ToString();
		var caret = Caret.Offset;
		UndoManager.BeginCompound();
		try
		{
			if (DocumentLength > 0)
			{
				RemoveAt(0, DocumentLength);
			}

			if (!string.IsNullOrEmpty(formatted))
			{
				Insert(0, formatted);
			}

			if (!string.Equals(Buffer.ToString(), formatted ?? string.Empty, StringComparison.Ordinal))
			{
				ReplaceBufferSilent(original);
				UndoManager.CancelCompound();
				return false;
			}

			var offset = caret;
			if (offset < 0)
			{
				offset = 0;
			}

			if (offset > DocumentLength)
			{
				offset = DocumentLength;
			}

			Caret.Selection.Reset();
			Caret.Move(offset);
			UndoManager.EndCompound();
			return true;
		}
		catch
		{
			ReplaceBufferSilent(original);
			UndoManager.CancelCompound();
			return false;
		}
	}

	public int Delete(int offset, bool forward)
	{
		if (Carets.Count > 1)
		{
			var total = 0;
			ForEachCaretEdit(caret =>
			{
				if (TryRemoveSelection(caret, out var removed))
				{
					total += removed;
					return;
				}

				total += forward
					? DeleteForward(caret, caret.Offset)
					: DeleteBackwards(caret, caret.Offset);
			});
			return total;
		}

		if (TryRemoveSelection(Caret, out var singleRemoved))
		{
			return singleRemoved;
		}

		return forward
			? DeleteForward(Caret, offset)
			: DeleteBackwards(Caret, offset);
	}

	/// <summary>
	/// Duplicate the current selection, or the current line when nothing is selected.
	/// </summary>
	public void Duplicate()
	{
		if (IsReadOnly)
		{
			return;
		}

		ForEachCaretEdit(caret =>
		{
			if (caret.Selection.Length > 0)
			{
				DuplicateSelection(caret);
				return;
			}

			DuplicateLine(caret);
		});
	}

	public void HandlePointerMoved(int offset)
	{
		foreach (var caret in Carets.All)
		{
			if (!caret.Selection.IsSelectingUsingMouse)
			{
				continue;
			}

			if (caret.Selection.EndOffset != offset)
			{
				caret.Selection.EndOffset = offset;
				caret.Move(offset);
			}

			return;
		}
	}

	public void HandlePointerPressed(int offset, KeyModifiers modifiers, int clickCount)
	{
		if ((modifiers & KeyModifiers.Alt) != 0)
		{
			var caret = Carets.AddAt(offset);
			if (caret == null)
			{
				return;
			}

			caret.Selection.Reset(offset);
			caret.Selection.StartMouseSelection();
			return;
		}

		if (clickCount >= 2)
		{
			Carets.CollapseToPrimary();
			SelectWord(offset);
			return;
		}

		if ((modifiers & KeyModifiers.Shift) != 0)
		{
			Caret.Move(offset);
			Caret.Selection.Update(offset);
			Caret.Selection.StartMouseSelection();
			return;
		}

		Carets.CollapseToPrimary();
		Caret.Move(offset);
		Caret.Selection.Reset(offset);
		Caret.Selection.StartMouseSelection();
	}

	public void HandlePointerReleased()
	{
		foreach (var caret in Carets.All)
		{
			caret.Selection.StopMouseSelection();
		}
	}

	/// <summary>
	/// Increase indentation by one level on the current line or all lines in the selection.
	/// </summary>
	public void Indent()
	{
		ForEachCaretEdit(caret =>
		{
			if (caret.Selection.Length > 0)
			{
				IndentSelection(caret);
			}
			else
			{
				IndentAtCaret(caret);
			}
		});
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
		if (string.IsNullOrEmpty(value)
			|| (offset < 0)
			|| (offset > Buffer.Count))
		{
			return;
		}
		if (ReadOnlySectionProvider?.CanModify(offset) == false)
		{
			return;
		}

		Buffer.Insert(offset, value);
		OnDocumentChanged(offset, value, TextDocumentChangeType.Add);
	}

	public void Load(string data)
	{
		IsRestoringViewport = true;
		Buffer.Reset(data ?? string.Empty);
		OnDocumentChanged(0, null, TextDocumentChangeType.Reset);
	}

	/// <summary>
	/// Replace the buffer and derived line/token state without recording undo
	/// or clearing the undo stacks.
	/// </summary>
	internal void ReplaceBufferSilent(string text)
	{
		Buffer.Reset(text ?? string.Empty);
		var args = new TextDocumentChangedArgs(0, null, TextDocumentChangeType.Reset);
		try
		{
			Lines.Rebuild(args);
		}
		catch
		{
			try
			{
				Lines.Rebuild(args);
			}
			catch
			{
			}
		}

		try
		{
			FoldingManager.ApplyDocumentChange(args);
			FoldingManager.Refresh();
		}
		catch
		{
		}

		try
		{
			TokenManager.Rebuild(args);
		}
		catch
		{
		}

		NotifyComputedPropertyChanged(nameof(DocumentLength));
		Caret.EnsureInDocument();
		try
		{
			DocumentChanged?.Invoke(this, args);
		}
		catch
		{
		}
	}

	/// <summary>
	/// Moves the caret without bringing it into view. Used when restoring a saved
	/// caret together with ViewMetrics.Offset.
	/// </summary>
	public void RestoreCaret(int offset)
	{
		IsRestoringViewport = true;
		if (offset < 0)
		{
			offset = 0;
		}

		if (offset > DocumentLength)
		{
			offset = DocumentLength;
		}

		Caret.Move(offset);
	}

	public void Measure(TextLayout line, Size availableSize)
	{
		if (line != null)
		{
			ViewMetrics.CharacterHeight = Math.Max(1, line.Height);
			ViewMetrics.CharacterWidth = Math.Max(1, line.WidthIncludingTrailingWhitespace);
		}

		try
		{
			ViewMetrics.DocumentSize = Lines.Measure(availableSize, WordWrap);
		}
		catch
		{
			ViewMetrics.DocumentSize = new Size(
				Math.Max(1, ViewMetrics.CharacterWidth),
				Math.Max(1, ViewMetrics.CharacterHeight));
		}

		// Viewport is the visible area (set for real in Arrange). Unconstrained
		// measure must not replace a known viewport with DocumentSize — that
		// makes the scroller think content fits, then arrange shrinks it and
		// Offset.X jumps. Only fill Viewport when it is still empty (first paint).
		var existing = ViewMetrics.Viewport;
		var viewportWidth = double.IsFinite(availableSize.Width)
			? availableSize.Width
			: (existing.Width > 1 ? existing.Width : ViewMetrics.DocumentSize.Width);
		var viewportHeight = double.IsFinite(availableSize.Height)
			? availableSize.Height
			: (existing.Height > 1 ? existing.Height : ViewMetrics.DocumentSize.Height);
		ViewMetrics.Viewport = new Size(
			double.IsFinite(viewportWidth) && (viewportWidth >= 0) ? viewportWidth : 0,
			double.IsFinite(viewportHeight) && (viewportHeight >= 0) ? viewportHeight : 0
		);

		if (Lines.LastMeasureChangedLayout)
		{
			try
			{
				Carets.UpdateVisualLayouts();
			}
			catch
			{
			}
		}
	}

	public void MoveAllCarets(CaretMoveDirection direction, bool extendSelection)
	{
		foreach (var caret in Carets.All)
		{
			caret.Move(direction, extendSelection);
		}

		Carets.MergeOverlapping();
	}

	public void ProcessKeyDownEvent(KeyEventArgs args)
	{
		if (CompletionManager.TryHandleKey(args))
		{
			args.Handled = true;
			return;
		}

		foreach (var caret in Carets.All)
		{
			caret.Selection.ProcessKeyDown(args);
		}

		InputManager.ProcessKeyArgs(args);

		if (CompletionManager.IsOpen)
		{
			CompletionManager.UpdateFilterFromDocument();
		}
	}

	public void ProcessKeyUpEvent(KeyEventArgs args)
	{
		foreach (var caret in Carets.All)
		{
			caret.Selection.ProcessKeyUp(args);
		}
	}

	/// <summary>
	/// Paste clipboard text. When line count equals caret count, each caret gets one line
	/// (document order). Otherwise the full text is inserted at every caret.
	/// </summary>
	public void ProcessPaste(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		var carets = Carets.DocumentOrder();
		if (carets.Count > 1)
		{
			var lines = SplitClipboardLines(text);
			if (lines.Length == carets.Count)
			{
				ForEachCaretEdit(caret =>
				{
					var index = carets.IndexOf(caret);
					InsertTextAtCaret(caret, lines[index]);
				});
				return;
			}
		}

		ProcessTextInput(text);
	}

	public void ProcessTextInput(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		ForEachCaretEdit(caret => InsertTextAtCaret(caret, text));

		if (Carets.Count > 1)
		{
			return;
		}

		if (CompletionManager.TryTriggerFromInsertedText(text))
		{
			return;
		}

		if (CompletionManager.IsOpen)
		{
			CompletionManager.UpdateFilterFromDocument();
		}
	}

	public void RemoveAt(int offset, int length)
	{
		if ((offset < 0) || (length <= 0) || (offset >= Buffer.Count))
		{
			return;
		}

		if ((offset + length) > Buffer.Count)
		{
			length = Buffer.Count - offset;
			if (length <= 0)
			{
				return;
			}
		}

		var removed = Buffer.Substring(offset, length);
		Buffer.RemoveAt(offset, length);
		OnDocumentChanged(offset, removed, TextDocumentChangeType.Remove);
	}

	public void SelectAllText()
	{
		Caret.Selection.Update(0, Buffer.Count);
	}

	/// <summary>
	/// Selects the visual row (logical line, or one wrap section) at document Y.
	/// </summary>
	public bool SelectVisualLine(double documentY)
	{
		if (!TryGetVisualLineRange(documentY, out var start, out var end))
		{
			return false;
		}

		ApplyVisualLineSelection(start, end, end);
		return true;
	}

	/// <summary>
	/// Extends selection from an anchor visual-row range through the row at document Y.
	/// </summary>
	public bool SelectVisualLineRange(int anchorStart, int anchorEnd, double documentY)
	{
		if (!TryGetVisualLineRange(documentY, out var start, out var end))
		{
			return false;
		}

		var selectionStart = Math.Min(anchorStart, start);
		var selectionEnd = Math.Max(anchorEnd, end);

		// Down / same row: caret at the end of the last selected row.
		// Up: caret at the beginning of the first (top) selected row.
		var caretOffset = start < anchorStart ? selectionStart : selectionEnd;
		ApplyVisualLineSelection(selectionStart, selectionEnd, caretOffset);
		return true;
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

	public bool TryGetVisualLineRange(double documentY, out int start, out int end)
	{
		start = 0;
		end = 0;
		if (!Lines.TryGetLineForOffset(0, documentY, out var line))
		{
			return false;
		}

		line.GetVisualSubLineRangeAtY(documentY, out start, out end);
		return true;
	}

	/// <summary>
	/// Reduces indentation by one level on the current line or all lines in the selection.
	/// </summary>
	public void Unindent()
	{
		ForEachCaretEdit(caret =>
		{
			if (caret.Selection.Length > 0)
			{
				UnindentSelection(caret);
			}
			else
			{
				UnindentCurrentLine(caret);
			}
		});
	}

	protected virtual void OnDocumentChanged(int offset, ReadOnlySpan<char> text, TextDocumentChangeType type)
	{
		var textString = text.IsEmpty ? null : text.ToString();
		OnDocumentChanged(offset, textString, type);
	}

	protected virtual void OnDocumentChanged(int offset, string text, TextDocumentChangeType type, bool pinViewport = false)
	{
		using var _ = ProfilerExtensions.Start(Profiler, nameof(DocumentChanged));
		LastChangePinnedViewport = pinViewport;
		var args = new TextDocumentChangedArgs(offset, text, type, pinViewport);
		if (args.Type is TextDocumentChangeType.Reset)
		{
			UndoManager.Clear();
		}
		else if (UndoManager.Enabled)
		{
			UndoManager.Add(args);
		}
		try
		{
			if (!Lines.TryApplyLastLineEdit(args))
			{
				Lines.Rebuild(args);
			}
		}
		catch
		{
			try
			{
				Lines.Rebuild(new TextDocumentChangedArgs(0, null, TextDocumentChangeType.Reset));
			}
			catch
			{
				// Line table is required for caret/paint; keep the buffer if rebuild fails.
			}
		}

		try
		{
			FoldingManager.ApplyDocumentChange(args);
			if (!Lines.LastEditNeedsPaintOnly)
			{
				FoldingManager.Refresh();
			}
		}
		catch
		{
			// Folding is derived; a strategy fault must not drop the edit.
		}

		try
		{
			TokenManager.Rebuild(args);
		}
		catch
		{
			// Highlighting is derived; keep the document and caret consistent.
		}
		try
		{
			NotifyComputedPropertyChanged(nameof(DocumentLength));
			NotifyComputedPropertyChanged(nameof(UndoManager));
		}
		catch
		{
		}

		Carets.EnsureAllInDocument();
		try
		{
			DocumentChanged?.Invoke(this, args);
		}
		catch
		{
			// Host listeners must not take down typing.
		}
	}

	internal void NotifyDiagnosticVersion(int version)
	{
		DiagnosticVersion = version;
	}

	internal void DeleteCopyPayloads()
	{
		ForEachCaretEdit(caret =>
		{
			SelectCopyPayload(caret);
			TryRemoveSelection(caret, out _);
		});
	}

	internal void HandleEnterKey()
	{
		ForEachCaretEdit(caret =>
		{
			TryRemoveSelection(caret, out _);
			var offset = caret.Offset;
			Insert(offset, "\r\n");
			Carets.ShiftAfter(offset, 2, caret);
			caret.Move(offset + 2);

			var newLineOffset = caret.Offset;
			if (IndentionManager.TryGetIndention(newLineOffset, out var indent)
				&& !indent.IsEmpty)
			{
				Insert(newLineOffset, indent.ToString());
				Carets.ShiftAfter(newLineOffset, indent.Length, caret);
				caret.Move(newLineOffset + indent.Length);
			}
		});
	}

	/// <summary>
	/// Insert without <see cref="IReadOnlySectionProvider.CanModify" />. Used for
	/// host output that must land before a live prompt.
	/// </summary>
	internal void InsertUnrestricted(int offset, string value, bool pinViewport)
	{
		if ((offset < 0)
			|| (offset > Buffer.Count)
			|| string.IsNullOrEmpty(value))
		{
			return;
		}

		Buffer.Insert(offset, value);
		OnDocumentChanged(offset, value, TextDocumentChangeType.Add, pinViewport);
	}

	internal static string[] SplitClipboardLines(string text)
	{
		return text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
	}

	internal bool TryRemoveSelection(out int removed)
	{
		return TryRemoveSelection(Caret, out removed);
	}

	internal bool TryRemoveSelection(Caret caret, out int removed)
	{
		if ((caret.Selection.Length <= 0)
			|| UndoManager.IsProcessing)
		{
			removed = 0;
			return false;
		}

		var start = Math.Min(caret.Selection.StartOffset, caret.Selection.EndOffset);
		var end = Math.Max(caret.Selection.StartOffset, caret.Selection.EndOffset);
		var request = new Range
		{
			StartOffset = start,
			EndOffset = end
		};

		if (ReadOnlySectionProvider != null)
		{
			var segments = ReadOnlySectionProvider
				.GetDeletableSegments(request)
				.Where(static s => s.Length > 0)
				.OrderBy(static s => s.StartOffset)
				.ToList();

			if (segments.Count == 0)
			{
				removed = 0;
				return false;
			}

			removed = 0;

			for (var i = segments.Count - 1; i >= 0; i--)
			{
				var segment = segments[i];
				var text = Buffer.Substring(segment.StartOffset, segment.Length);
				Buffer.RemoveAt(segment.StartOffset, segment.Length);
				OnDocumentChanged(segment.StartOffset, text, TextDocumentChangeType.Remove);
				Carets.ShiftAfter(segment.StartOffset, -segment.Length, caret);
				removed += segment.Length;
			}

			var caretTarget = segments[0].StartOffset;
			caret.Move(caretTarget);
			caret.Selection.Reset(caretTarget);
			return true;
		}

		removed = end - start;
		var selection = Buffer.Substring(start, removed);
		Buffer.RemoveAt(start, removed);
		caret.Move(start);
		caret.Selection.Reset(start);
		OnDocumentChanged(start, selection, TextDocumentChangeType.Remove);
		Carets.ShiftAfter(start, -removed, caret);
		return true;
	}

	private void ApplyVisualLineSelection(int start, int end, int caretOffset)
	{
		Caret.Selection.StopSelection();
		Caret.Move(caretOffset);
		Caret.Selection.Update(start, end);
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

	private int DeleteBackwards(Caret caret, int caretOffset)
	{
		if (caretOffset > Buffer.Count)
		{
			caretOffset = Buffer.Count;
			caret.Move(caretOffset);
		}

		if (caretOffset <= 0)
		{
			return 0;
		}

		if (ReadOnlySectionProvider?.CanModify(caretOffset - 1) == false)
		{
			return caretOffset;
		}

		var offset = caretOffset - 1;

		if ((Buffer[offset] == '\n')
			&& (offset > 0)
			&& (Buffer[offset - 1] == '\r'))
		{
			offset--;
			Buffer.RemoveAt(offset, 2);
			OnDocumentChanged(offset, "\r\n", TextDocumentChangeType.Remove);
			Carets.ShiftAfter(offset, -2, caret);
			caret.Move(offset);
			return 2;
		}

		var removed = Buffer.Substring(offset, 1);
		Buffer.RemoveAt(offset, 1);
		OnDocumentChanged(offset, removed, TextDocumentChangeType.Remove);
		Carets.ShiftAfter(offset, -1, caret);
		caret.Move(offset);
		return 1;
	}

	private int DeleteForward(Caret caret, int caretOffset)
	{
		if (caretOffset > Buffer.Count)
		{
			caretOffset = Buffer.Count;
			caret.Move(caretOffset);
		}

		if (caretOffset >= Buffer.Count)
		{
			return Buffer.Count;
		}

		if (ReadOnlySectionProvider?.CanModify(caretOffset) == false)
		{
			return caretOffset;
		}

		if ((Buffer[caretOffset] == '\r')
			&& ((caretOffset + 1) < Buffer.Count)
			&& (Buffer[caretOffset + 1] == '\n'))
		{
			Buffer.RemoveAt(caretOffset, 2);
			OnDocumentChanged(caretOffset, "\r\n", TextDocumentChangeType.Remove);
			Carets.ShiftAfter(caretOffset, -2, caret);
			caret.Move(caretOffset);
			return 2;
		}
		var deleted = Buffer.Substring(caretOffset, 1);
		Buffer.RemoveAt(caretOffset, 1);
		OnDocumentChanged(caretOffset, deleted, TextDocumentChangeType.Remove);
		Carets.ShiftAfter(caretOffset, -1, caret);
		caret.Move(caretOffset);
		return 1;
	}

	private void DuplicateLine(Caret caret)
	{
		var line = Lines.GetLineFromOffset(caret.Offset);
		if (line == null)
		{
			return;
		}

		var lineLength = line.EndOffset - line.StartOffset;
		var lineText = lineLength > 0
			? Buffer.Substring(line.StartOffset, lineLength)
			: string.Empty;
		var insertOffset = line.EndOffset;
		string textToInsert;
		if (line.LineEndingLength > 0)
		{
			textToInsert = lineText;
		}
		else
		{
			textToInsert = "\r\n" + lineText;
		}

		if (string.IsNullOrEmpty(textToInsert))
		{
			return;
		}

		var caretOffset = caret.Offset;
		Insert(insertOffset, textToInsert);
		Carets.ShiftAfter(insertOffset, textToInsert.Length, caret);
		caret.Selection.Reset();
		caret.Move(caretOffset + textToInsert.Length);
	}

	private void DuplicateSelection(Caret caret)
	{
		var start = Math.Min(caret.Selection.StartOffset, caret.Selection.EndOffset);
		var end = Math.Max(caret.Selection.StartOffset, caret.Selection.EndOffset);
		var length = end - start;
		if (length <= 0)
		{
			return;
		}

		var text = Buffer.Substring(start, length);
		Insert(end, text);
		Carets.ShiftAfter(end, length, caret);
		var newEnd = end + length;
		caret.Move(newEnd);
		caret.Selection.Update(end, newEnd);
	}

	private void ForEachCaretEdit(Action<Caret> action)
	{
		UndoManager.BeginCompound();
		try
		{
			foreach (var caret in Carets.ReverseDocumentOrder())
			{
				try
				{
					action(caret);
				}
				catch
				{
					// One caret must not drop edits already applied to the others.
				}
			}

			Carets.MergeOverlapping();
			Carets.EnsureAllInDocument();
		}
		finally
		{
			UndoManager.EndCompound();
		}
	}

	private void IndentAtCaret(Caret caret)
	{
		var indent = IndentionManager.IndentString;
		if (string.IsNullOrEmpty(indent))
		{
			return;
		}

		var offset = caret.Offset;
		if (Lines.GetLineFromOffset(offset) == null)
		{
			return;
		}

		Insert(offset, indent);
		Carets.ShiftAfter(offset, indent.Length, caret);
		caret.Move(offset + indent.Length);
	}

	private void IndentSelection(Caret caret)
	{
		var indentStr = IndentionManager.IndentString;
		if (string.IsNullOrEmpty(indentStr))
		{
			return;
		}

		var startOffset = Math.Min(caret.Selection.StartOffset, caret.Selection.EndOffset);
		var endOffset = Math.Max(caret.Selection.StartOffset, caret.Selection.EndOffset);

		var firstLine = Lines.GetLineFromOffset(startOffset);
		var lastLine = Lines.GetLineFromOffset(Math.Max(endOffset - 1, 0));
		if ((firstLine == null) || (lastLine == null))
		{
			return;
		}

		var changes = new List<(int offset, string text)>();
		var totalAdded = 0;
		var addedToStart = 0;

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
				addedToStart += indentStr.Length;
			}
		}

		if (changes.Count == 0)
		{
			return;
		}

		for (var i = 0; i < changes.Count; i++)
		{
			var (offset, text) = changes[i];
			Insert(offset, text);
			Carets.ShiftAfter(offset, text.Length, caret);
		}

		var newStart = startOffset + addedToStart;
		var newEnd = endOffset + totalAdded;
		caret.Selection.Update(newStart, newEnd);
	}

	private void InsertTextAtCaret(Caret caret, string text)
	{
		TryRemoveSelection(caret, out _);
		var offset = caret.Offset;
		if (ReadOnlySectionProvider?.CanModify(offset) == false)
		{
			return;
		}

		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		Insert(offset, text);
		Carets.ShiftAfter(offset, text.Length, caret);
		caret.Move(offset + text.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	private static bool IsWordChar(char c)
	{
		// todo: customize this based on the type of document
		return char.IsLetterOrDigit(c) || (c == '_') || (c == '-');
	}

	private void SelectCopyPayload(Caret caret)
	{
		if (caret.Selection.Length > 0)
		{
			return;
		}

		var line = caret.Line ?? Lines.GetLineFromOffset(caret.Offset);
		if ((line == null) || (line.Length <= 0))
		{
			return;
		}

		caret.Selection.Update(line.StartOffset, line.StartOffset + line.Length);
	}

	private void UnindentCurrentLine(Caret caret)
	{
		var indentStr = IndentionManager.IndentString;
		if (string.IsNullOrEmpty(indentStr))
		{
			return;
		}

		var offset = caret.Offset;
		var line = Lines.GetLineFromOffset(offset);
		if ((line == null) || (line.Length == 0))
		{
			return;
		}

		var whiteSpaceEnd = 0;
		while ((whiteSpaceEnd < line.Length)
			&& ((line.StartOffset + whiteSpaceEnd) < Buffer.Count)
			&& char.IsWhiteSpace(Buffer[line.StartOffset + whiteSpaceEnd]))
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

		var removeOffset = line.StartOffset;
		var posInLine = offset - line.StartOffset;
		if ((posInLine > 0) && (posInLine <= whiteSpaceEnd))
		{
			removeOffset = Math.Max(line.StartOffset, offset - removeCount);
		}

		RemoveAt(removeOffset, removeCount);
		Carets.ShiftAfter(removeOffset, -removeCount, caret);

		var newOffset = offset - removeCount;
		caret.Move(Math.Max(line.StartOffset, newOffset));
	}

	private void UnindentSelection(Caret caret)
	{
		var indentStr = IndentionManager.IndentString;
		if (string.IsNullOrEmpty(indentStr))
		{
			return;
		}

		var startOffset = Math.Min(caret.Selection.StartOffset, caret.Selection.EndOffset);
		var endOffset = Math.Max(caret.Selection.StartOffset, caret.Selection.EndOffset);

		var firstLine = Lines.GetLineFromOffset(startOffset);
		var lastLine = Lines.GetLineFromOffset(Math.Max(endOffset - 1, 0));
		if ((firstLine == null) || (lastLine == null))
		{
			return;
		}

		var changes = new List<(int offset, int length)>();
		var totalRemoved = 0;
		var removedFromStart = 0;

		for (var i = lastLine.LineNumber; i >= firstLine.LineNumber; i--)
		{
			if (!Lines.TryGetLine(i, out var line) || (line.Length == 0))
			{
				continue;
			}

			var wsEnd = 0;
			while ((wsEnd < line.Length)
				&& ((line.StartOffset + wsEnd) < Buffer.Count)
				&& char.IsWhiteSpace(Buffer[line.StartOffset + wsEnd]))
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
					removedFromStart = removeCount;
				}
			}
		}

		if (changes.Count == 0)
		{
			return;
		}

		for (var i = 0; i < changes.Count; i++)
		{
			var (offset, length) = changes[i];
			RemoveAt(offset, length);
			Carets.ShiftAfter(offset, -length, caret);
		}

		var newStart = Math.Max(0, startOffset - removedFromStart);
		var newEnd = Math.Max(0, endOffset - totalRemoved);
		caret.Selection.Update(newStart, newEnd);
	}

	/// <summary>
	/// Ask the bound editor control to take keyboard focus.
	/// </summary>
	public void RequestFocus()
	{
		FocusRequested?.Invoke(this, EventArgs.Empty);
	}

	#endregion

	#region Events

	public event EventHandler<TextDocumentChangedArgs> DocumentChanged;

	public event EventHandler FocusRequested;

	#endregion
}