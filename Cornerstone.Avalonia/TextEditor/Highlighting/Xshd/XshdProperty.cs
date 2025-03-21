namespace Cornerstone.Avalonia.TextEditor.Highlighting.Xshd;

/// <summary>
/// A property in an Xshd file.
/// </summary>
public class XshdProperty : XshdElement
{
	#region Properties

	/// <summary>
	/// Gets/sets the name.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Gets/sets the value.
	/// </summary>
	public string Value { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override object AcceptVisitor(IXshdVisitor visitor)
	{
		return null;
		//			return visitor.VisitProperty(this);
	}

	#endregion
}