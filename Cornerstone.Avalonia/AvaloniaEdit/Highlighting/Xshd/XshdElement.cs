namespace Cornerstone.Avalonia.AvaloniaEdit.Highlighting.Xshd;

/// <summary>
/// An element in a XSHD rule set.
/// </summary>
public abstract class XshdElement
{
	#region Properties

	/// <summary>
	/// Gets the column number in the .xshd file.
	/// </summary>
	public int ColumnNumber { get; set; }

	/// <summary>
	/// Gets the line number in the .xshd file.
	/// </summary>
	public int LineNumber { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Applies the visitor to this element.
	/// </summary>
	public abstract object AcceptVisitor(IXshdVisitor visitor);

	#endregion
}