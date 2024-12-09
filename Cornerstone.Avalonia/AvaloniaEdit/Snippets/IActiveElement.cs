#region References

#endregion

using Cornerstone.Collections;
using Cornerstone.Text;
using Cornerstone.Text.Document;

namespace Cornerstone.Avalonia.AvaloniaEdit.Snippets;

/// <summary>
/// Represents an active element that allows the snippet to stay interactive after insertion.
/// </summary>
public interface IActiveElement
{
	#region Properties

	/// <summary>
	/// Gets whether this element is editable (the user will be able to select it with Tab).
	/// </summary>
	bool IsEditable { get; }

	/// <summary>
	/// Gets the segment associated with this element. May be null.
	/// </summary>
	IRange Range { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Called when the interactive mode is deactivated.
	/// </summary>
	void Deactivate(SnippetEventArgs e);

	/// <summary>
	/// Called when the all snippet elements have been inserted.
	/// </summary>
	void OnInsertionCompleted();

	#endregion
}