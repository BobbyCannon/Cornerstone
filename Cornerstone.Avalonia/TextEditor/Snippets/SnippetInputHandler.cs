#region References

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Input;
using Cornerstone.Avalonia.TextEditor.Editing;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Snippets;

internal sealed class SnippetInputHandler : TextAreaStackedInputHandler
{
	#region Fields

	private readonly InsertionContext _context;

	#endregion

	#region Constructors

	public SnippetInputHandler(InsertionContext context)
		: base(context.TextArea)
	{
		_context = context;
	}

	#endregion

	#region Methods

	public override void Attach()
	{
		base.Attach();

		SelectElement(FindNextEditableElement(-1, false));
	}

	public override void Detach()
	{
		base.Detach();
		_context.Deactivate(new SnippetEventArgs(DeactivateReason.InputHandlerDetached));
	}

	public override void OnPreviewKeyDown(KeyEventArgs e)
	{
		base.OnPreviewKeyDown(e);
		if (e.Key == Key.Escape)
		{
			_context.Deactivate(new SnippetEventArgs(DeactivateReason.EscapePressed));
			e.Handled = true;
		}
		else if (e.Key == Key.Return)
		{
			_context.Deactivate(new SnippetEventArgs(DeactivateReason.ReturnPressed));
			e.Handled = true;
		}
		else if (e.Key == Key.Tab)
		{
			var backwards = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
			SelectElement(FindNextEditableElement(TextArea.Caret.Offset, backwards));
			e.Handled = true;
		}
	}

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private IActiveElement FindNextEditableElement(int offset, bool backwards)
	{
		var elements = _context.ActiveElements.Where(e => e.IsEditable && (e.Range != null));
		if (backwards)
		{
			elements = elements.Reverse();
			foreach (var element in elements)
			{
				if (offset > element.Range.EndIndex)
				{
					return element;
				}
			}
		}
		else
		{
			foreach (var element in elements)
			{
				if (offset < element.Range.StartIndex)
				{
					return element;
				}
			}
		}
		return elements.FirstOrDefault();
	}

	private void SelectElement(IActiveElement element)
	{
		if (element != null)
		{
			TextArea.Selection = Selection.Create(TextArea, element.Range);
			TextArea.Caret.Offset = element.Range.EndIndex;
		}
	}

	#endregion
}