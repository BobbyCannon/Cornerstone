﻿#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Avalonia.Input;
using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Avalonia.TextEditor.Utils;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Editing;

/// <summary>
/// We re-use the CommandBinding and InputBinding instances between multiple text areas,
/// so this class is static.
/// </summary>
internal class EditingCommandHandler
{
	#region Fields

	private static readonly List<RoutedCommandBinding> _commandBindings;
	private static readonly List<KeyBinding> _keyBindings;

	#endregion

	#region Constructors

	static EditingCommandHandler()
	{
		_keyBindings = [];
		_commandBindings = [];

		AddBinding(EditingCommands.Delete, KeyModifiers.None, Key.Delete, OnDelete(CaretMovementType.CharRight));
		AddBinding(EditingCommands.DeleteNextWord, KeyModifiers.Control, Key.Delete, OnDelete(CaretMovementType.WordRight));
		AddBinding(EditingCommands.Backspace, KeyModifiers.None, Key.Back, OnDelete(CaretMovementType.Backspace));

		// make Shift-Backspace do the same as plain backspace
		_keyBindings.Add(EditingCommands.Backspace.CreateKeyBinding(KeyModifiers.Shift, Key.Back));

		AddBinding(EditingCommands.DeletePreviousWord, KeyModifiers.Control, Key.Back, OnDelete(CaretMovementType.WordLeft));
		AddBinding(EditingCommands.EnterParagraphBreak, KeyModifiers.None, Key.Enter, OnEnter);
		AddBinding(EditingCommands.EnterLineBreak, KeyModifiers.Shift, Key.Enter, OnEnter);
		AddBinding(EditingCommands.TabForward, KeyModifiers.None, Key.Tab, OnTab);
		AddBinding(EditingCommands.TabBackward, KeyModifiers.Shift, Key.Tab, OnShiftTab);

		AddBinding(ApplicationCommands.Delete, OnDelete(CaretMovementType.None), CanDelete);
		AddBinding(ApplicationCommands.Copy, OnCopy, CanCopy);
		AddBinding(ApplicationCommands.Cut, OnCut, CanCut);
		AddBinding(ApplicationCommands.Paste, OnPaste, CanPaste);

		AddBinding(TextEditorEditCommands.ToggleOverstrike, OnToggleOverstrike);
		AddBinding(TextEditorEditCommands.DeleteLine, OnDeleteLine);
		AddBinding(TextEditorEditCommands.DuplicateLine, OnDuplicateLine);

		AddBinding(TextEditorEditCommands.RemoveLeadingWhitespace, OnRemoveLeadingWhitespace);
		AddBinding(TextEditorEditCommands.RemoveTrailingWhitespace, OnRemoveTrailingWhitespace);
		AddBinding(TextEditorEditCommands.ConvertToUppercase, OnConvertToUpperCase);
		AddBinding(TextEditorEditCommands.ConvertToLowercase, OnConvertToLowerCase);
		AddBinding(TextEditorEditCommands.ConvertToTitleCase, OnConvertToTitleCase);
		AddBinding(TextEditorEditCommands.InvertCase, OnInvertCase);
		AddBinding(TextEditorEditCommands.ConvertTabsToSpaces, OnConvertTabsToSpaces);
		AddBinding(TextEditorEditCommands.ConvertSpacesToTabs, OnConvertSpacesToTabs);
		AddBinding(TextEditorEditCommands.ConvertLeadingTabsToSpaces, OnConvertLeadingTabsToSpaces);
		AddBinding(TextEditorEditCommands.ConvertLeadingSpacesToTabs, OnConvertLeadingSpacesToTabs);
		AddBinding(TextEditorEditCommands.IndentSelection, OnIndentSelection);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Creates a new <see cref="TextAreaInputHandler" /> for the text area.
	/// </summary>
	public static TextAreaInputHandler Create(TextArea textArea)
	{
		var handler = new TextAreaInputHandler(textArea);
		handler.RoutedCommandBindings.AddRange(_commandBindings);
		handler.KeyBindings.AddRange(_keyBindings);
		return handler;
	}

	internal static string GetTextToPaste(string text, TextArea textArea)
	{
		try
		{
			// Try retrieving the text as one of:
			//  - the FormatToApply
			//  - UnicodeText
			//  - Text
			// (but don't try the same format twice)
			//if (pastingEventArgs.FormatToApply != null && dataObject.GetDataPresent(pastingEventArgs.FormatToApply))
			//    text = (string)dataObject.GetData(pastingEventArgs.FormatToApply);
			//else if (pastingEventArgs.FormatToApply != DataFormats.UnicodeText &&
			//         dataObject.GetDataPresent(DataFormats.UnicodeText))
			//    text = (string)dataObject.GetData(DataFormats.UnicodeText);
			//else if (pastingEventArgs.FormatToApply != DataFormats.Text &&
			//         dataObject.GetDataPresent(DataFormats.Text))
			//    text = (string)dataObject.GetData(DataFormats.Text);
			//else
			//    return null; // no text data format
			// convert text back to correct newlines for this document
			var newLine = TextUtilities.GetNewLineFromDocument(textArea.Document, textArea.Caret.Line);
			text = TextUtilities.NormalizeNewLines(text, newLine);
			text = textArea.Settings.ConvertTabsToSpaces
				? text.Replace("\t", new string(' ', textArea.Settings.IndentationSize))
				: text;
			return text;
		}
		catch (OutOfMemoryException)
		{
			// may happen when trying to paste a huge string
			return null;
		}
	}

	private static void AddBinding(RoutedCommand command, KeyModifiers modifiers, Key key, EventHandler<ExecutedRoutedEventArgs> handler)
	{
		_commandBindings.Add(new RoutedCommandBinding(command, handler));
		_keyBindings.Add(command.CreateKeyBinding(modifiers, key));
	}

	private static void AddBinding(RoutedCommand command, EventHandler<ExecutedRoutedEventArgs> handler, EventHandler<CanExecuteRoutedEventArgs> canExecuteHandler = null)
	{
		_commandBindings.Add(new RoutedCommandBinding(command, handler, canExecuteHandler));
	}

	private static void CanCopy(object target, CanExecuteRoutedEventArgs args)
	{
		// HasSomethingSelected for copy and cut commands
		var textArea = GetTextArea(target);
		if (textArea?.Document != null)
		{
			args.CanExecute = textArea.Settings.CutCopyWholeLine || !textArea.Selection.IsEmpty;
			args.Handled = true;
		}
	}

	private static void CanCut(object target, CanExecuteRoutedEventArgs args)
	{
		// HasSomethingSelected for copy and cut commands
		var textArea = GetTextArea(target);
		if (textArea?.Document != null)
		{
			//var segmentsToDelete = textArea.GetDeletableSegments(new SimpleRange(currentLine.StartIndex, currentLine.TotalLength));
			args.CanExecute = !textArea.IsReadOnly
				&& (textArea.Document.TextLength > 0)
				&& (
					(textArea.Settings.CutCopyWholeLine
						&& textArea.Selection.IsEmpty
						&& (textArea.GetDeletableSegments(textArea.Document.GetLineByOffset(textArea.Caret.Line)).Length > 0))
					|| (textArea.GetDeletableSegments(textArea.Selection.SurroundingRange).Length > 0)
				);
			args.Handled = true;
		}
	}

	private static void CanDelete(object target, CanExecuteRoutedEventArgs args)
	{
		var textArea = GetTextArea(target);
		if (textArea?.Document != null)
		{
			args.CanExecute = true;
			args.Handled = true;
		}
	}

	private static void CanPaste(object target, CanExecuteRoutedEventArgs args)
	{
		var textArea = GetTextArea(target);
		if (textArea?.Document != null)
		{
			args.CanExecute = textArea.ReadOnlySectionProvider.CanInsert(textArea.Caret.Offset);
			args.Handled = true;
		}
	}

	private static void ConvertCase(Func<string, string> transformText, object target, ExecutedRoutedEventArgs args)
	{
		TransformSelectedSegments(
			delegate(TextArea textArea, IRange segment)
			{
				var oldText = textArea.Document.GetText(segment);
				var newText = transformText(oldText);
				textArea.Document.Replace(segment.StartIndex, segment.Length, newText,
					OffsetChangeMappingType.CharacterReplace);
			}, target, args, DefaultSegmentType.WholeDocument);
	}

	private static void ConvertSpacesToTabs(TextArea textArea, DocumentLine line)
	{
		ConvertSpacesToTabs(textArea, TextUtilities.GetLeadingWhitespace(textArea.Document, line));
	}

	private static void ConvertSpacesToTabs(TextArea textArea, IRange range)
	{
		var document = textArea.Document;
		var endOffset = range.EndIndex;
		var indentationSize = textArea.Settings.IndentationSize;
		var spacesCount = 0;
		for (var offset = range.StartIndex; offset < endOffset; offset++)
		{
			if (document.GetCharAt(offset) == ' ')
			{
				spacesCount++;
				if (spacesCount == indentationSize)
				{
					document.Replace(offset - (indentationSize - 1), indentationSize, "\t",
						OffsetChangeMappingType.CharacterReplace);
					spacesCount = 0;
					offset -= indentationSize - 1;
					endOffset -= indentationSize - 1;
				}
			}
			else
			{
				spacesCount = 0;
			}
		}
	}

	private static void ConvertTabsToSpaces(TextArea textArea, DocumentLine line)
	{
		ConvertTabsToSpaces(textArea, TextUtilities.GetLeadingWhitespace(textArea.Document, line));
	}

	private static void ConvertTabsToSpaces(TextArea textArea, IRange range)
	{
		var document = textArea.Document;
		var endOffset = range.EndIndex;
		var indentationString = new string(' ', textArea.Settings.IndentationSize);
		for (var offset = range.StartIndex; offset < endOffset; offset++)
		{
			if (document.GetCharAt(offset) == '\t')
			{
				document.Replace(offset, 1, indentationString, OffsetChangeMappingType.CharacterReplace);
				endOffset += indentationString.Length - 1;
			}
		}
	}

	private static bool CopySelectedText(TextArea textArea)
	{
		var text = textArea.Selection.GetText();
		text = TextUtilities.NormalizeNewLines(text, Environment.NewLine);

		SetClipboardText(text, textArea);

		textArea.OnTextCopied(new TextEventArgs(text));
		return true;
	}

	private static bool CopyWholeLine(TextArea textArea, DocumentLine line)
	{
		IRange wholeLine = new SimpleRange(line.StartIndex, line.TotalLength);
		var text = textArea.Document.GetText(wholeLine);
		// Ignore empty line copy
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		// Ensure we use the appropriate newline sequence for the OS
		text = TextUtilities.NormalizeNewLines(text, Environment.NewLine);

		// TODO: formats
		//DataObject data = new DataObject();
		//if (ConfirmDataFormat(textArea, data, DataFormats.UnicodeText))
		//    data.SetText(text);

		//// Also copy text in HTML format to clipboard - good for pasting text into Word
		//// or to the SharpDevelop forums.
		//if (ConfirmDataFormat(textArea, data, DataFormats.Html))
		//{
		//    IHighlighter highlighter = textArea.GetService(typeof(IHighlighter)) as IHighlighter;
		//    HtmlClipboard.SetHtml(data,
		//        HtmlClipboard.CreateHtmlFragment(textArea.Document, highlighter, wholeLine,
		//            new HtmlOptions(textArea.Options)));
		//}

		//if (ConfirmDataFormat(textArea, data, LineSelectedType))
		//{
		//    var lineSelected = new MemoryStream(1);
		//    lineSelected.WriteByte(1);
		//    data.SetData(LineSelectedType, lineSelected, false);
		//}

		//var copyingEventArgs = new DataObjectCopyingEventArgs(data, false);
		//textArea.RaiseEvent(copyingEventArgs);
		//if (copyingEventArgs.CommandCancelled)
		//    return false;

		SetClipboardText(text, textArea);

		textArea.OnTextCopied(new TextEventArgs(text));
		return true;
	}

	private static TextArea GetTextArea(object target)
	{
		return target as TextArea;
	}

	private static string InvertCase(string text)
	{
		// TODO: culture
		//var culture = CultureInfo.CurrentCulture;
		var buffer = text.ToCharArray();
		for (var i = 0; i < buffer.Length; ++i)
		{
			var c = buffer[i];
			buffer[i] = char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c);
		}
		return new string(buffer);
	}

	private static void OnConvertLeadingSpacesToTabs(object target, ExecutedRoutedEventArgs args)
	{
		TransformSelectedLines(ConvertSpacesToTabs, target, args, DefaultSegmentType.WholeDocument);
	}

	private static void OnConvertLeadingTabsToSpaces(object target, ExecutedRoutedEventArgs args)
	{
		TransformSelectedLines(ConvertTabsToSpaces, target, args, DefaultSegmentType.WholeDocument);
	}

	private static void OnConvertSpacesToTabs(object target, ExecutedRoutedEventArgs args)
	{
		TransformSelectedSegments(ConvertSpacesToTabs, target, args, DefaultSegmentType.WholeDocument);
	}

	private static void OnConvertTabsToSpaces(object target, ExecutedRoutedEventArgs args)
	{
		TransformSelectedSegments(ConvertTabsToSpaces, target, args, DefaultSegmentType.WholeDocument);
	}

	private static void OnConvertToLowerCase(object target, ExecutedRoutedEventArgs args)
	{
		ConvertCase(CultureInfo.CurrentCulture.TextInfo.ToLower, target, args);
	}

	private static void OnConvertToTitleCase(object target, ExecutedRoutedEventArgs args)
	{
		throw new NotSupportedException();
		//ConvertCase(CultureInfo.CurrentCulture.TextInfo.ToTitleCase, target, args);
	}

	private static void OnConvertToUpperCase(object target, ExecutedRoutedEventArgs args)
	{
		ConvertCase(CultureInfo.CurrentCulture.TextInfo.ToUpper, target, args);
	}

	private static void OnCopy(object target, ExecutedRoutedEventArgs args)
	{
		var textArea = GetTextArea(target);
		if (textArea?.Document != null)
		{
			if (textArea.Selection.IsEmpty && textArea.Settings.CutCopyWholeLine)
			{
				var currentLine = textArea.Document.GetLineByNumber(textArea.Caret.Line);
				CopyWholeLine(textArea, currentLine);
			}
			else
			{
				CopySelectedText(textArea);
			}
			args.Handled = true;
		}
	}

	private static void OnCut(object target, ExecutedRoutedEventArgs args)
	{
		var textArea = GetTextArea(target);
		if (textArea?.Document != null)
		{
			if (textArea.Selection.IsEmpty && textArea.Settings.CutCopyWholeLine)
			{
				var currentLine = textArea.Document.GetLineByNumber(textArea.Caret.Line);
				if (CopyWholeLine(textArea, currentLine))
				{
					var segmentsToDelete =
						textArea.GetDeletableSegments(
							new SimpleRange(currentLine.StartIndex, currentLine.TotalLength));
					for (var i = segmentsToDelete.Length - 1; i >= 0; i--)
					{
						textArea.Document.Remove(segmentsToDelete[i]);
					}
				}
			}
			else
			{
				if (CopySelectedText(textArea))
				{
					textArea.RemoveSelectedText();
				}
			}
			textArea.Caret.BringCaretToView();
			args.Handled = true;
		}
	}

	private static EventHandler<ExecutedRoutedEventArgs> OnDelete(CaretMovementType caretMovement)
	{
		return (target, args) =>
		{
			var textArea = GetTextArea(target);
			if (textArea?.Document != null)
			{
				if (textArea.Selection.IsEmpty)
				{
					var startPos = textArea.Caret.Position;
					var enableVirtualSpace = textArea.Settings.EnableVirtualSpace;
					// When pressing delete; don't move the caret further into virtual space - instead delete the newline
					if (caretMovement == CaretMovementType.CharRight)
					{
						enableVirtualSpace = false;
					}
					var desiredXPos = textArea.Caret.DesiredXPos;
					var endPos = CaretNavigationCommandHandler.GetNewCaretPosition(
						textArea.TextView, startPos, caretMovement, enableVirtualSpace, ref desiredXPos);
					// GetNewCaretPosition may return (0,0) as new position,
					// thus we need to validate endPos before using it in the selection.
					if ((endPos.Line < 1) || (endPos.Column < 1))
					{
						endPos = new TextViewPosition(Math.Max(endPos.Line, 1), Math.Max(endPos.Column, 1));
					}
					// Don't do anything if the number of lines of a rectangular selection would be changed by the deletion.
					if (textArea.Selection is RectangleSelection && (startPos.Line != endPos.Line))
					{
						return;
					}
					// Don't select the text to be deleted; just reuse the ReplaceSelectionWithText logic
					// Reuse the existing selection, so that we continue using the same logic
					textArea.Selection.StartSelectionOrSetEndpoint(startPos, endPos)
						.ReplaceSelectionWithText(string.Empty);
				}
				else
				{
					textArea.RemoveSelectedText();
				}
				textArea.Caret.BringCaretToView();
				args.Handled = true;
			}
		};
	}

	private static void OnDeleteLine(object target, ExecutedRoutedEventArgs args)
	{
		var textArea = GetTextArea(target);
		if (textArea?.Document == null)
		{
			return;
		}

		var line = textArea.Document.GetLineByNumber(textArea.Caret.Line);
		if (textArea.ReadOnlySectionProvider.CanInsert(line.StartIndex))
		{
			textArea.Document.Remove(line);
		}

		var segment = textArea.ReadOnlySectionProvider.GetDeletableSegments(line).FirstOrDefault();
		if (segment != null)
		{
			textArea.Document.Remove(segment);
		}

		args.Handled = true;
	}

	private static void OnDuplicateLine(object target, ExecutedRoutedEventArgs args)
	{
		var textArea = GetTextArea(target);
		if (textArea?.Document == null)
		{
			return;
		}

		int startOffset = 0, endOffset = 0, originalOffset = textArea.Caret.Offset;

		if (textArea.Selection.Length > 0)
		{
			startOffset = textArea.Document.GetOffset(textArea.Selection.StartPosition.Location);
			endOffset = textArea.Document.GetOffset(textArea.Selection.EndPosition.Location);
		}

		var text = textArea.Selection.Length == 0
			? textArea.Document.GetText(textArea.Document.GetLineByNumber(textArea.Caret.Line))
			: textArea.Selection.GetText();

		var offset = textArea.Selection.Length == 0
			? textArea.Document.GetLineByNumber(textArea.Caret.Line).EndIndex
			: Math.Max(startOffset, endOffset);

		textArea.Document.Insert(offset, textArea.Selection.Length == 0 ? Environment.NewLine + text : text);

		if (textArea.Selection.Length > 0)
		{
			textArea.Selection = Selection.Create(textArea, offset, offset + text.Length);
			textArea.Caret.Offset = offset + text.Length;
		}
		else
		{
			textArea.Caret.Offset = originalOffset + Environment.NewLine.Length + text.Length;
		}

		args.Handled = true;
	}

	private static void OnEnter(object target, ExecutedRoutedEventArgs args)
	{
		var textArea = GetTextArea(target);
		if ((textArea != null) && textArea.IsFocused)
		{
			textArea.PerformTextInput("\n");
			args.Handled = true;
		}
	}

	private static void OnIndentSelection(object target, ExecutedRoutedEventArgs args)
	{
		var textArea = GetTextArea(target);
		if ((textArea?.Document != null) && (textArea.IndentationStrategy != null))
		{
			using (textArea.Document.RunUpdate())
			{
				int start, end;
				if (textArea.Selection.IsEmpty)
				{
					start = 1;
					end = textArea.Document.LineCount;
				}
				else
				{
					start = textArea.Document.GetLineByOffset(textArea.Selection.SurroundingRange.StartIndex)
						.LineNumber;
					end = textArea.Document.GetLineByOffset(textArea.Selection.SurroundingRange.EndIndex)
						.LineNumber;
				}
				textArea.IndentationStrategy.IndentLines(textArea.Document, start, end);
			}
			textArea.Caret.BringCaretToView();
			args.Handled = true;
		}
	}

	private static void OnInvertCase(object target, ExecutedRoutedEventArgs args)
	{
		ConvertCase(InvertCase, target, args);
	}

	private static async void OnPaste(object target, ExecutedRoutedEventArgs args)
	{
		var textArea = GetTextArea(target);
		if (textArea?.Document != null)
		{
			textArea.Document.BeginUpdate();

			string text = null;
			try
			{
				text = await TopLevel.GetTopLevel(textArea)?.Clipboard?.GetTextAsync();
			}
			catch (Exception)
			{
				textArea.Document.EndUpdate();
				return;
			}

			if (text == null)
			{
				textArea.Document.EndUpdate();
				return;
			}

			text = GetTextToPaste(text, textArea);

			if (!string.IsNullOrEmpty(text))
			{
				textArea.ReplaceSelectionWithText(text);
			}

			textArea.Caret.BringCaretToView();
			args.Handled = true;

			textArea.Document.EndUpdate();
		}
	}

	private static void OnRemoveLeadingWhitespace(object target, ExecutedRoutedEventArgs args)
	{
		TransformSelectedLines(
			delegate(TextArea textArea, DocumentLine line) { textArea.Document.Remove(TextUtilities.GetLeadingWhitespace(textArea.Document, line)); }, target, args, DefaultSegmentType.WholeDocument);
	}

	private static void OnRemoveTrailingWhitespace(object target, ExecutedRoutedEventArgs args)
	{
		TransformSelectedLines(
			delegate(TextArea textArea, DocumentLine line) { textArea.Document.Remove(TextUtilities.GetTrailingWhitespace(textArea.Document, line)); }, target, args, DefaultSegmentType.WholeDocument);
	}

	private static void OnShiftTab(object target, ExecutedRoutedEventArgs args)
	{
		TransformSelectedLines(
			RemoveLineIndent,
			target,
			args,
			DefaultSegmentType.CurrentLine
		);
	}

	private static void OnTab(object target, ExecutedRoutedEventArgs args)
	{
		var textArea = GetTextArea(target);
		if (textArea?.Document != null)
		{
			using (textArea.Document.RunUpdate())
			{
				if (textArea.Selection.IsMultiline)
				{
					var segment = textArea.Selection.SurroundingRange;
					var start = textArea.Document.GetLineByOffset(segment.StartIndex);
					var end = textArea.Document.GetLineByOffset(segment.EndIndex);
					// don't include the last line if no characters on it are selected
					if ((start != end) && (end.StartIndex == segment.EndIndex))
					{
						end = end.PreviousLine;
					}
					var current = start;
					while (true)
					{
						var offset = current.StartIndex;
						if (textArea.ReadOnlySectionProvider.CanInsert(offset))
						{
							textArea.Document.Replace(offset, 0, textArea.Settings.IndentationString,
								OffsetChangeMappingType.KeepAnchorBeforeInsertion);
						}
						if (current == end)
						{
							break;
						}
						current = current.NextLine;
					}
				}
				else
				{
					var indentationString = textArea.Settings.GetIndentationString(textArea.Caret.Column);
					textArea.ReplaceSelectionWithText(indentationString);
				}
			}
			textArea.Caret.BringCaretToView();
			args.Handled = true;
		}
	}

	private static void OnToggleOverstrike(object target, ExecutedRoutedEventArgs args)
	{
		var textArea = GetTextArea(target);
		if ((textArea != null) && textArea.Settings.AllowToggleOverstrikeMode)
		{
			textArea.OverstrikeMode = !textArea.OverstrikeMode;
		}
	}

	private static void RemoveLineIndent(TextArea textArea, DocumentLine line)
	{
		var offset = line.StartIndex;
		var s = TextUtilities.GetSingleIndentationSegment(textArea.Document, offset, textArea.Settings.IndentationSize);
		if (s.Length <= 0)
		{
			return;
		}

		s = textArea.GetDeletableSegments(s).FirstOrDefault();
		if (s is { Length: > 0 })
		{
			textArea.Document.Remove(s.StartIndex, s.Length);
		}
	}

	private static void SetClipboardText(string text, Visual visual)
	{
		try
		{
			TopLevel.GetTopLevel(visual)?.Clipboard?.SetTextAsync(text);
		}
		catch (Exception)
		{
			// Apparently this exception sometimes happens randomly.
			// The MS controls just ignore it, so we'll do the same.
		}
	}

	/// <summary>
	/// Calls transformLine on all lines in the selected range.
	/// transformLine needs to handle read-only segments!
	/// </summary>
	private static void TransformSelectedLines(Action<TextArea, DocumentLine> transformLine, object target,
		ExecutedRoutedEventArgs args, DefaultSegmentType defaultSegmentType)
	{
		var textArea = GetTextArea(target);
		if (textArea?.Document != null)
		{
			using (textArea.Document.RunUpdate())
			{
				DocumentLine start, end;
				if (textArea.Selection.IsEmpty)
				{
					if (defaultSegmentType == DefaultSegmentType.CurrentLine)
					{
						start = end = textArea.Document.GetLineByNumber(textArea.Caret.Line);
					}
					else if (defaultSegmentType == DefaultSegmentType.WholeDocument)
					{
						start = textArea.Document.Lines.First();
						end = textArea.Document.Lines.Last();
					}
					else
					{
						start = end = null;
					}
				}
				else
				{
					var segment = textArea.Selection.SurroundingRange;
					start = textArea.Document.GetLineByOffset(segment.StartIndex);
					end = textArea.Document.GetLineByOffset(segment.EndIndex);
					// don't include the last line if no characters on it are selected
					if ((start != end) && (end.StartIndex == segment.EndIndex))
					{
						end = end.PreviousLine;
					}
				}
				if (start != null)
				{
					transformLine(textArea, start);
					while (start != end)
					{
						start = start.NextLine;
						transformLine(textArea, start);
					}
				}
			}
			textArea.Caret.BringCaretToView();
			args.Handled = true;
		}
	}

	/// <summary>
	/// Calls transformLine on all writable segment in the selected range.
	/// </summary>
	private static void TransformSelectedSegments(Action<TextArea, IRange> transformSegment, object target,
		ExecutedRoutedEventArgs args, DefaultSegmentType defaultSegmentType)
	{
		var textArea = GetTextArea(target);
		if (textArea?.Document != null)
		{
			using (textArea.Document.RunUpdate())
			{
				IEnumerable<IRange> segments;
				if (textArea.Selection.IsEmpty)
				{
					if (defaultSegmentType == DefaultSegmentType.CurrentLine)
					{
						segments = [textArea.Document.GetLineByNumber(textArea.Caret.Line)];
					}
					else if (defaultSegmentType == DefaultSegmentType.WholeDocument)
					{
						segments = textArea.Document.Lines;
					}
					else
					{
						segments = null;
					}
				}
				else
				{
					segments = textArea.Selection.Segments;
				}
				if (segments != null)
				{
					foreach (var segment in segments.Reverse())
					{
						foreach (var writableSegment in textArea.GetDeletableSegments(segment).Reverse())
						{
							transformSegment(textArea, writableSegment);
						}
					}
				}
			}
			textArea.Caret.BringCaretToView();
			args.Handled = true;
		}
	}

	#endregion

	#region Enumerations

	private enum DefaultSegmentType
	{
		WholeDocument,
		CurrentLine
	}

	#endregion
}