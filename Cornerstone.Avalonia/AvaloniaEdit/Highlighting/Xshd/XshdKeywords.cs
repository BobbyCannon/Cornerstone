#region References

using System.Collections.Generic;
using Cornerstone.Avalonia.AvaloniaEdit.Utils;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Highlighting.Xshd;

/// <summary>
/// A list of keywords.
/// </summary>
public class XshdKeywords : XshdElement
{
	#region Fields

	private readonly NullSafeCollection<string> _words = [];

	#endregion

	#region Properties

	/// <summary>
	/// The color.
	/// </summary>
	public XshdReference<XshdColor> ColorReference { get; set; }

	/// <summary>
	/// Gets the list of key words.
	/// </summary>
	public IList<string> Words => _words;

	#endregion

	#region Methods

	/// <inheritdoc />
	public override object AcceptVisitor(IXshdVisitor visitor)
	{
		return visitor.VisitKeywords(this);
	}

	#endregion
}