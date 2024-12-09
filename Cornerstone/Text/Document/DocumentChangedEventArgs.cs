#region References

using System;

#endregion

namespace Cornerstone.Text.Document;

public class DocumentChangedEventArgs : EventArgs
{
	#region Constructors

	/// <summary>
	/// Provides data for the <see cref="DocumentChanged" /> event.
	/// </summary>
	public DocumentChangedEventArgs(TextEditorDocument oldDocument, TextEditorDocument newDocument)
	{
		OldDocument = oldDocument;
		NewDocument = newDocument;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the new TextDocument.
	/// </summary>
	public TextEditorDocument NewDocument { get; private set; }

	/// <summary>
	/// Gets the old TextDocument.
	/// </summary>
	public TextEditorDocument OldDocument { get; private set; }

	#endregion
}