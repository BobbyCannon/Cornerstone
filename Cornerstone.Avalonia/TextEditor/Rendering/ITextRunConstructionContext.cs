﻿#nullable enable

#region References

using Avalonia.Media.TextFormatting;
using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Avalonia.TextEditor.Utils;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

/// <summary>
/// Contains information relevant for text run creation.
/// </summary>
public interface ITextRunConstructionContext
{
	#region Properties

	/// <summary>
	/// Gets the text document.
	/// </summary>
	TextEditorDocument Document { get; }

	/// <summary>
	/// Gets the global text run properties.
	/// </summary>
	TextRunProperties GlobalTextRunProperties { get; }

	/// <summary>
	/// Gets the text view for which the construction runs.
	/// </summary>
	TextView TextView { get; }

	/// <summary>
	/// Gets the visual line that is currently being constructed.
	/// </summary>
	VisualLine VisualLine { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets a piece of text from the document.
	/// </summary>
	/// <remarks>
	/// This method is allowed to return a larger string than requested.
	/// It does this by returning a <see cref="StringSegment" /> that describes the requested segment within the returned string.
	/// This method should be the preferred text access method in the text transformation pipeline, as it can avoid repeatedly allocating string instances
	/// for text within the same line.
	/// </remarks>
	StringSegment GetText(int offset, int length);

	#endregion
}