#region References

using System.Collections.Generic;
using Cornerstone.Avalonia.TextEditor.Utils;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Highlighting;

/// <summary>
/// A highlighting rule set describes a set of spans that are valid at a given code location.
/// </summary>
public class HighlightingRuleSet
{
	#region Constructors

	/// <summary>
	/// Creates a new RuleSet instance.
	/// </summary>
	public HighlightingRuleSet()
	{
		Spans = new NullSafeCollection<HighlightingSpan>();
		Rules = new NullSafeCollection<HighlightingRule>();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets/Sets the name of the rule set.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Gets the list of rules.
	/// </summary>
	public IList<HighlightingRule> Rules { get; }

	/// <summary>
	/// Gets the list of spans.
	/// </summary>
	public IList<HighlightingSpan> Spans { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override string ToString()
	{
		return "[" + GetType().Name + " " + Name + "]";
	}

	#endregion
}