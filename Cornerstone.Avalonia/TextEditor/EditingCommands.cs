#region References

using Cornerstone.Avalonia.Input;

#endregion

namespace Cornerstone.Avalonia.TextEditor;

public static class EditingCommands
{
	#region Constructors

	static EditingCommands()
	{
		Backspace = new(nameof(Backspace));
		Delete = new(nameof(Delete));
		DeleteNextWord = new(nameof(DeleteNextWord));
		DeletePreviousWord = new(nameof(DeletePreviousWord));
		EnterLineBreak = new(nameof(EnterLineBreak));
		EnterParagraphBreak = new(nameof(EnterParagraphBreak));
		MoveDownByLine = new(nameof(MoveDownByLine));
		MoveDownByPage = new(nameof(MoveDownByPage));
		MoveLeftByCharacter = new(nameof(MoveLeftByCharacter));
		MoveLeftByWord = new(nameof(MoveLeftByWord));
		MoveRightByCharacter = new(nameof(MoveRightByCharacter));
		MoveRightByWord = new(nameof(MoveRightByWord));
		MoveToDocumentEnd = new(nameof(MoveToDocumentEnd));
		MoveToDocumentStart = new(nameof(MoveToDocumentStart));
		MoveToLineEnd = new(nameof(MoveToLineEnd));
		MoveToLineStart = new(nameof(MoveToLineStart));
		MoveUpByLine = new(nameof(MoveUpByLine));
		MoveUpByPage = new(nameof(MoveUpByPage));
		SelectDownByLine = new(nameof(SelectDownByLine));
		SelectDownByPage = new(nameof(SelectDownByPage));
		SelectLeftByCharacter = new(nameof(SelectLeftByCharacter));
		SelectLeftByWord = new(nameof(SelectLeftByWord));
		SelectRightByCharacter = new(nameof(SelectRightByCharacter));
		SelectRightByWord = new(nameof(SelectRightByWord));
		SelectToDocumentEnd = new(nameof(SelectToDocumentEnd));
		SelectToDocumentStart = new(nameof(SelectToDocumentStart));
		SelectToLineEnd = new(nameof(SelectToLineEnd));
		SelectToLineStart = new(nameof(SelectToLineStart));
		SelectUpByLine = new(nameof(SelectUpByLine));
		SelectUpByPage = new(nameof(SelectUpByPage));
		TabBackward = new(nameof(TabBackward));
		TabForward = new(nameof(TabForward));
	}

	#endregion

	#region Properties

	public static RoutedCommand Backspace { get; }

	public static RoutedCommand Delete { get; }

	public static RoutedCommand DeleteNextWord { get; }

	public static RoutedCommand DeletePreviousWord { get; }

	public static RoutedCommand EnterLineBreak { get; }

	public static RoutedCommand EnterParagraphBreak { get; }

	public static RoutedCommand MoveDownByLine { get; }

	public static RoutedCommand MoveDownByPage { get; }

	public static RoutedCommand MoveLeftByCharacter { get; }

	public static RoutedCommand MoveLeftByWord { get; }

	public static RoutedCommand MoveRightByCharacter { get; }

	public static RoutedCommand MoveRightByWord { get; }

	public static RoutedCommand MoveToDocumentEnd { get; }

	public static RoutedCommand MoveToDocumentStart { get; }

	public static RoutedCommand MoveToLineEnd { get; }

	public static RoutedCommand MoveToLineStart { get; }

	public static RoutedCommand MoveUpByLine { get; }

	public static RoutedCommand MoveUpByPage { get; }

	public static RoutedCommand SelectDownByLine { get; }

	public static RoutedCommand SelectDownByPage { get; }

	public static RoutedCommand SelectLeftByCharacter { get; }

	public static RoutedCommand SelectLeftByWord { get; }

	public static RoutedCommand SelectRightByCharacter { get; }

	public static RoutedCommand SelectRightByWord { get; }

	public static RoutedCommand SelectToDocumentEnd { get; }

	public static RoutedCommand SelectToDocumentStart { get; }

	public static RoutedCommand SelectToLineEnd { get; }

	public static RoutedCommand SelectToLineStart { get; }

	public static RoutedCommand SelectUpByLine { get; }

	public static RoutedCommand SelectUpByPage { get; }

	public static RoutedCommand TabBackward { get; }

	public static RoutedCommand TabForward { get; }

	#endregion
}