#region References

using System;
using System.ComponentModel;
using Cornerstone.Avalonia.AvaloniaEdit.Editing;
using Cornerstone.Avalonia.AvaloniaEdit.Rendering;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit;

/// <summary>
/// Represents a text editor control
/// (<see cref="TextEditor" />, <see cref="TextArea" /> or <see cref="TextView" />).
/// </summary>
public interface ITextEditorComponent : IServiceProvider
{
	#region Properties

	/// <summary>
	/// Gets the document being edited.
	/// </summary>
	TextDocument Document { get; }

	/// <summary>
	/// Gets the options of the text editor.
	/// </summary>
	TextEditorOptions Options { get; }

	#endregion

	#region Events

	/// <summary>
	/// Occurs when the Document property changes (when the text editor is connected to another
	/// document - not when the document content changes).
	/// </summary>
	event EventHandler<DocumentChangedEventArgs> DocumentChanged;

	/// <summary>
	/// Occurs when the Options property changes, or when an option inside the current option list
	/// changes.
	/// </summary>
	event PropertyChangedEventHandler OptionChanged;

	#endregion
}