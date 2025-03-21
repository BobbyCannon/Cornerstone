#region References

using System.Text.RegularExpressions;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Highlighting;

/// <summary>
/// A highlighting span is a region with start+end expression that has a different RuleSet inside
/// and colors the region.
/// </summary>
public class HighlightingSpan
{
	#region Properties

	/// <summary>
	/// Gets the color used for the text matching the end expression.
	/// </summary>
	public HighlightingColor EndColor { get; set; }

	/// <summary>
	/// Gets/Sets the end expression.
	/// </summary>
	public Regex EndExpression { get; set; }

	/// <summary>
	/// Gets/Sets the rule set that applies inside this span.
	/// </summary>
	public HighlightingRuleSet RuleSet { get; set; }

	/// <summary>
	/// Gets the color used for the text between start and end.
	/// </summary>
	public HighlightingColor SpanColor { get; set; }

	/// <summary>
	/// Gets/Sets whether the span color includes the end.
	/// The default is <c> false </c>.
	/// </summary>
	public bool SpanColorIncludesEnd { get; set; }

	/// <summary>
	/// Gets/Sets whether the span color includes the start.
	/// The default is <c> false </c>.
	/// </summary>
	public bool SpanColorIncludesStart { get; set; }

	/// <summary>
	/// Gets the color used for the text matching the start expression.
	/// </summary>
	public HighlightingColor StartColor { get; set; }

	/// <summary>
	/// Gets/Sets the start expression.
	/// </summary>
	public Regex StartExpression { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override string ToString()
	{
		return "[" + GetType().Name + " Start=" + StartExpression + ", End=" + EndExpression + "]";
	}

	#endregion
}