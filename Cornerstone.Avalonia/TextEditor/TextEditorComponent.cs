#region References

using System;
using System.ComponentModel;
using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Avalonia.TextEditor.Editing;
using Cornerstone.Avalonia.TextEditor.Rendering;

#endregion

namespace Cornerstone.Avalonia.TextEditor;

/// <summary>
/// Represents a text editor control
/// (<see cref="TextEditorControl" />, <see cref="TextArea" /> or <see cref="TextView" />).
/// </summary>
public interface ITextEditorComponent : IServiceProvider
{
	#region Properties

	/// <summary>
	/// Gets the document being edited.
	/// </summary>
	TextEditorDocument Document { get; }

	/// <summary>
	/// Gets the options of the text editor.
	/// </summary>
	TextEditorSettings Settings { get; }

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