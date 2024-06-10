#region References

using System.Collections.Generic;
using Cornerstone.Avalonia.AvaloniaEdit.Utils;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Snippets;

/// <summary>
/// A snippet element that has sub-elements.
/// </summary>
public class SnippetContainerElement : SnippetElement
{
	#region Fields

	private readonly NullSafeCollection<SnippetElement> _elements = [];

	#endregion

	#region Properties

	/// <summary>
	/// Gets the list of child elements.
	/// </summary>
	public IList<SnippetElement> Elements => _elements;

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Insert(InsertionContext context)
	{
		foreach (var e in Elements)
		{
			e.Insert(context);
		}
	}

	#endregion

	///// <inheritdoc/>
	//public override Inline ToTextRun()
	//{
	//	Span span = new Span();
	//	foreach (SnippetElement e in this.Elements) {
	//		Inline r = e.ToTextRun();
	//		if (r != null)
	//			span.Inlines.Add(r);
	//	}
	//	return span;
	//}
}