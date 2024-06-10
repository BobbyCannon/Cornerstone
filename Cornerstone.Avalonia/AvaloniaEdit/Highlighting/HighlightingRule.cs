#region References

using System.Text.RegularExpressions;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Highlighting;

/// <summary>
/// A highlighting rule.
/// </summary>
public class HighlightingRule
{
	#region Properties

	/// <summary>
	/// Gets/Sets the highlighting color.
	/// </summary>
	public HighlightingColor Color { get; set; }

	/// <summary>
	/// Gets/Sets the regular expression for the rule.
	/// </summary>
	public Regex Regex { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override string ToString()
	{
		return "[" + GetType().Name + " " + Regex + "]";
	}

	#endregion
}