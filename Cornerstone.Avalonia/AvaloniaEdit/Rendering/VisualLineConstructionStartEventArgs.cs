#region References

using System;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Rendering;

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