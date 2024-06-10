#region References

using System.Collections.Generic;
using Cornerstone.Avalonia.AvaloniaEdit.Utils;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Highlighting.Xshd;

/// <summary>
/// A rule set in a XSHD file.
/// </summary>
public class XshdRuleSet : XshdElement
{
	#region Fields

	private readonly NullSafeCollection<XshdElement> _elements = [];

	#endregion

	#region Properties

	/// <summary>
	/// Gets the collection of elements.
	/// </summary>
	public IList<XshdElement> Elements => _elements;

	/// <summary>
	/// Gets/sets whether the case is ignored in expressions inside this rule set.
	/// </summary>
	public bool? IgnoreCase { get; set; }

	/// <summary>
	/// Gets/Sets the name of the rule set.
	/// </summary>
	public string Name { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Applies the visitor to all elements.
	/// </summary>
	public void AcceptElements(IXshdVisitor visitor)
	{
		foreach (var element in Elements)
		{
			element.AcceptVisitor(visitor);
		}
	}

	/// <inheritdoc />
	public override object AcceptVisitor(IXshdVisitor visitor)
	{
		return visitor.VisitRuleSet(this);
	}

	#endregion
}