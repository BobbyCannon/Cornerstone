namespace Cornerstone.Avalonia.AvaloniaEdit.Highlighting.Xshd;

/// <summary>
/// A visitor over the XSHD element tree.
/// </summary>
public interface IXshdVisitor
{
	#region Methods

	/// <summary> Visit method for XshdColor </summary>
	object VisitColor(XshdColor color);

	/// <summary> Visit method for XshdImport </summary>
	object VisitImport(XshdImport import);

	/// <summary> Visit method for XshdKeywords </summary>
	object VisitKeywords(XshdKeywords keywords);

	/// <summary> Visit method for XshdRule </summary>
	object VisitRule(XshdRule rule);

	/// <summary> Visit method for XshdRuleSet </summary>
	object VisitRuleSet(XshdRuleSet ruleSet);

	/// <summary> Visit method for XshdSpan </summary>
	object VisitSpan(XshdSpan span);

	#endregion
}