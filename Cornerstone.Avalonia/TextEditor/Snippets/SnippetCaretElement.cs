#region References

#endregion

using Cornerstone.Avalonia.TextEditor.Document;

namespace Cornerstone.Avalonia.TextEditor.Snippets;

/// <summary>
/// Sets the caret position after interactive mode has finished.
/// </summary>
public class SnippetCaretElement : SnippetElement
{
	#region Fields

	private readonly bool _setCaretOnlyIfTextIsSelected;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new SnippetCaretElement.
	/// </summary>
	public SnippetCaretElement()
	{
	}

	/// <summary>
	/// Creates a new SnippetCaretElement.
	/// </summary>
	/// <param name="setCaretOnlyIfTextIsSelected">
	/// If set to true, the caret is set only when some text was selected.
	/// This is useful when both SnippetCaretElement and SnippetSelectionElement are used in the same snippet.
	/// </param>
	public SnippetCaretElement(bool setCaretOnlyIfTextIsSelected)
	{
		_setCaretOnlyIfTextIsSelected = setCaretOnlyIfTextIsSelected;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Insert(InsertionContext context)
	{
		if (!_setCaretOnlyIfTextIsSelected || !string.IsNullOrEmpty(context.SelectedText))
		{
			SetCaret(context);
		}
	}

	internal static void SetCaret(InsertionContext context)
	{
		var pos = context.Document.CreateAnchor(context.InsertionPosition);
		pos.MovementType = AnchorMovementType.BeforeInsertion;
		pos.SurviveDeletion = true;
		context.Deactivated += (sender, e) =>
		{
			if ((e.Reason == DeactivateReason.ReturnPressed) || (e.Reason == DeactivateReason.NoActiveElements))
			{
				context.TextArea.Caret.Offset = pos.Offset;
			}
		};
	}

	#endregion
}