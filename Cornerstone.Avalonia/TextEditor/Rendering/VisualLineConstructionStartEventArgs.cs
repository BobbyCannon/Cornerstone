#region References

using System;
using Cornerstone.Avalonia.TextEditor.Document;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

/// <summary>
/// EventArgs for the <see cref="TextView.VisualLineConstructionStarting" /> event.
/// </summary>
public class VisualLineConstructionStartEventArgs : EventArgs
{
	#region Constructors

	/// <summary>
	/// Creates a new VisualLineConstructionStartEventArgs instance.
	/// </summary>
	public VisualLineConstructionStartEventArgs(DocumentLine firstLineInView)
	{
		FirstLineInView = firstLineInView ?? throw new ArgumentNullException(nameof(firstLineInView));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets/Sets the first line that is visible in the TextView.
	/// </summary>
	public DocumentLine FirstLineInView { get; }

	#endregion
}