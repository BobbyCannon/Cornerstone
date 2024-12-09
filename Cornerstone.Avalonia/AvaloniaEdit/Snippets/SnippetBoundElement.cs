#region References

using System;
using Cornerstone.Collections;
using Cornerstone.Text;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Snippets;

/// <summary>
/// An element that binds to a <see cref="SnippetReplaceableTextElement" /> and displays the same text.
/// </summary>
public class SnippetBoundElement : SnippetElement
{
	#region Properties

	/// <summary>
	/// Gets/Sets the target element.
	/// </summary>
	public SnippetReplaceableTextElement TargetElement { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Converts the text before copying it.
	/// </summary>
	public virtual string ConvertText(string input)
	{
		return input;
	}

	/// <inheritdoc />
	public override void Insert(InsertionContext context)
	{
		if (TargetElement != null)
		{
			var start = context.Document.CreateAnchor(context.InsertionPosition);
			start.MovementType = AnchorMovementType.BeforeInsertion;
			start.SurviveDeletion = true;
			var inputText = TargetElement.Text;
			if (inputText != null)
			{
				context.InsertText(ConvertText(inputText));
			}
			var end = context.Document.CreateAnchor(context.InsertionPosition);
			end.MovementType = AnchorMovementType.BeforeInsertion;
			end.SurviveDeletion = true;
			var segment = new AnchorRange(start, end);
			context.RegisterActiveElement(this, new BoundActiveElement(context, TargetElement, this, segment));
		}
	}

	#endregion

	///// <inheritdoc/>
	//public override Inline ToTextRun()
	//{
	//	if (TargetElement != null) {
	//		string inputText = TargetElement.Text;
	//		if (inputText != null) {
	//			return new Italic(new Run(ConvertText(inputText)));
	//		}
	//	}
	//	return base.ToTextRun();
	//}
}

internal sealed class BoundActiveElement : IActiveElement
{
	#region Fields

	internal IReplaceableActiveElement TargetElement;
	private readonly SnippetBoundElement _boundElement;
	private readonly InsertionContext _context;
	private AnchorRange _range;
	private readonly SnippetReplaceableTextElement _targetSnippetElement;

	#endregion

	#region Constructors

	public BoundActiveElement(InsertionContext context, SnippetReplaceableTextElement targetSnippetElement, SnippetBoundElement boundElement, AnchorRange range)
	{
		_context = context;
		_targetSnippetElement = targetSnippetElement;
		_boundElement = boundElement;
		_range = range;
	}

	#endregion

	#region Properties

	public bool IsEditable => false;

	public IRange Range => _range;

	#endregion

	#region Methods

	public void Deactivate(SnippetEventArgs e)
	{
		TargetElement.TextChanged -= targetElement_TextChanged;
	}

	public void OnInsertionCompleted()
	{
		TargetElement = _context.GetActiveElement(_targetSnippetElement) as IReplaceableActiveElement;
		if (TargetElement != null)
		{
			TargetElement.TextChanged += targetElement_TextChanged;
		}
	}

	private void targetElement_TextChanged(object sender, EventArgs e)
	{
		// Don't copy text if the segments overlap (we would get an endless loop).
		// This can happen if the user deletes the text between the replaceable element and the bound element.
		if (_range.GetOverlap(TargetElement.Range) == SegmentExtensions.Invalid)
		{
			var offset = _range.StartIndex;
			var length = _range.Length;
			var text = _boundElement.ConvertText(TargetElement.Text);
			if ((length != text.Length) || (text != _context.Document.GetText(offset, length)))
			{
				// Call replace only if we're actually changing something.
				// Without this check, we would generate an empty undo group when the user pressed undo.
				_context.Document.Replace(offset, length, text);
				if (length == 0)
				{
					// replacing an empty anchor segment with text won't enlarge it, so we have to recreate it
					_range = new AnchorRange(_context.Document, offset, text.Length);
				}
			}
		}
	}

	#endregion
}