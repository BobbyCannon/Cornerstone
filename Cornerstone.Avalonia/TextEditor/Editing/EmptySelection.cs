#region References

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cornerstone.Collections;
using Cornerstone.Internal;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Editing;

internal sealed class EmptySelection : Selection
{
	#region Constructors

	public EmptySelection(TextArea textArea) : base(textArea)
	{
	}

	#endregion

	#region Properties

	public override TextViewPosition EndPosition => new(TextLocation.Empty);

	public override int Length => 0;

	public override IEnumerable<SelectionRange> Segments => [];

	public override TextViewPosition StartPosition => new(TextLocation.Empty);

	public override IRange SurroundingRange => null;

	#endregion

	#region Methods

	public override bool Equals(object obj)
	{
		return this == obj;
	}

	// Use reference equality because there's only one EmptySelection per text area.
	public override int GetHashCode()
	{
		return RuntimeHelpers.GetHashCode(this);
	}

	public override string GetText()
	{
		return string.Empty;
	}

	public override void ReplaceSelectionWithText(string newText)
	{
		if (newText == null)
		{
			throw new ArgumentNullException(nameof(newText));
		}
		newText = AddSpacesIfRequired(newText, TextArea.Caret.Position, TextArea.Caret.Position);
		if (newText.Length > 0)
		{
			if (TextArea.ReadOnlySectionProvider.CanInsert(TextArea.Caret.Offset))
			{
				TextArea.Document.Insert(TextArea.Caret.Offset, newText);
			}
		}
		TextArea.Caret.VisualColumn = -1;
	}

	public override Selection SetEndpoint(TextViewPosition endPosition)
	{
		throw new NotSupportedException();
	}

	public override Selection StartSelectionOrSetEndpoint(TextViewPosition startPosition, TextViewPosition endPosition)
	{
		var document = TextArea.Document;
		if (document == null)
		{
			throw ThrowUtil.NoDocumentAssigned();
		}
		return Create(TextArea, startPosition, endPosition);
	}

	public override Selection UpdateOnDocumentChange(DocumentChangeEventArgs e)
	{
		return this;
	}

	#endregion
}