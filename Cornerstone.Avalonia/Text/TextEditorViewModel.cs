#region References

using Avalonia.Input;
using Cornerstone.Avalonia.Text.Models;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.Text;

/// <summary>
/// What does the editor need to know?
/// - Line Tokens
/// - Line Numbers
/// - Line Foldings
/// - Additional Cursors
/// - Syntax Highlight (tokens)
/// - Inline Snippet
/// - Multiple Snippets (rectangle selection)
/// - Context Menu
/// - Smart options
/// </summary>
[SourceReflection]
public partial class TextEditorViewModel : Notifiable<TextEditorViewModel>
{
	#region Constructors

	public TextEditorViewModel()
	{
		Caret = new(this);
		Document = new TextDocument();
		Selection = new Selection();
		ShowLineNumbers = true;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The carets for the editor.
	/// </summary>
	[UpdateableAction(UpdateableAction.All)]
	public Caret Caret { get; }

	/// <summary>
	/// The option to wrap text.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial TextDocument Document { get; set; }

	/// <summary>
	/// The lines of the document.
	/// </summary>
	[UpdateableAction(UpdateableAction.All)]
	public Selection Selection { get; }

	/// <summary>
	/// The option to show line numbers.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool ShowLineNumbers { get; set; }

	/// <summary>
	/// The option to wrap text.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool WordWrap { get; set; }

	#endregion

	#region Methods

	public bool ProcessKeyDownEvent(KeyEventArgs args)
	{
		switch (args.Key)
		{
			case Key.Home:
			{
				if (args.KeyModifiers == KeyModifiers.Control)
				{
					Caret.Move(0);
				}
				else
				{
					Caret.MoveToLineStart();
				}
				return true;
			}
			case Key.End:
			{
				if (args.KeyModifiers == KeyModifiers.Control)
				{
					Caret.Move(Document.Length);
				}
				else
				{
					Caret.MoveToLineEnd();
				}
				return true;
			}
			case Key.Left:
			{
				Caret.MoveLeft();
				return true;
			}
			case Key.Right:
			{
				Caret.MoveRight();
				return true;
			}
			case Key.Up:
			{
				Caret.MoveUp();
				return true;
			}
			case Key.Down:
			{
				Caret.MoveDown();
				return true;
			}
			case Key.Enter:
			{
				Document.Insert(Caret.Offset, "\r\n");
				Caret.Move(Caret.Offset + 2);
				return true;
			}
			case Key.Tab:
			{
				Document.Insert(Caret.Offset, "\t");
				Caret.Move(Caret.Offset + 1);
				return true;
			}
			case Key.Back:
			{
				var moved = Document.Delete(Caret.Offset, false);
				if (moved > 0)
				{
					Caret.Move(Caret.Offset - moved);
				}
				return true;
			}
			case Key.Delete:
			{
				Document.Delete(Caret.Offset, true);
				return true;
			}
			case Key.LeftShift:
			{
				if (!Selection.IsSelectingUsingKeyboard)
				{
					Selection.StartKeyboardSelection(Caret.Offset);
				}
				return false;
			}
			default:
			{
				return false;
			}
		}
	}

	public bool ProcessKeyUpEvent(KeyEventArgs args)
	{
		switch (args.Key)
		{
			case Key.LeftShift:
			case Key.RightShift:
			{
				Selection.EndKeyboardSelection();
				return false;
			}
			default:
			{
				return false;
			}
		}
	}

	public void ProcessTextInput(TextInputEventArgs args)
	{
		if (string.IsNullOrEmpty(args.Text))
		{
			return;
		}

		Document.Insert(Caret.Offset, args.Text);
		Caret.Move(Caret.Offset + args.Text.Length);
	}

	internal void OnCaretMoved(int offset)
	{
		if (Selection.IsSelecting)
		{
			Selection.EndOffset = offset;
		}
	}

	#endregion
}