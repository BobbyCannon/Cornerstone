#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

/// <summary>
/// Allows transforming visual line elements.
/// </summary>
public interface IVisualLineTransformer
{
	#region Methods

	/// <summary>
	/// Applies the transformation to the specified list of visual line elements.
	/// </summary>
	void Transform(ITextRunConstructionContext context, IList<VisualLineElement> elements);

	#endregion
}