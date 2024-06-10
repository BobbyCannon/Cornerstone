namespace Cornerstone.Avalonia.AvaloniaEdit.Highlighting.Xshd;

/// <summary>
/// &lt;Import&gt; element.
/// </summary>
public class XshdImport : XshdElement
{
	#region Properties

	/// <summary>
	/// Gets/sets the referenced rule set.
	/// </summary>
	public XshdReference<XshdRuleSet> RuleSetReference { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override object AcceptVisitor(IXshdVisitor visitor)
	{
		return visitor.VisitImport(this);
	}

	#endregion
}