namespace Cornerstone.Avalonia.AvaloniaEdit.Highlighting.Xshd;

/// <summary>
/// &lt;Rule&gt; element.
/// </summary>
public class XshdRule : XshdElement
{
	#region Properties

	/// <summary>
	/// Gets/sets the color reference.
	/// </summary>
	public XshdReference<XshdColor> ColorReference { get; set; }

	/// <summary>
	/// Gets/sets the rule regex.
	/// </summary>
	public string Regex { get; set; }

	/// <summary>
	/// Gets/sets the rule regex type.
	/// </summary>
	public XshdRegexType RegexType { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override object AcceptVisitor(IXshdVisitor visitor)
	{
		return visitor.VisitRule(this);
	}

	#endregion
}