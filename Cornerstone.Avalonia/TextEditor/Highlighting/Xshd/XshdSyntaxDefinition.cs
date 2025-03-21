#region References

using System.Collections.Generic;
using System.Xml.Serialization;
using Cornerstone.Avalonia.TextEditor.Utils;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Highlighting.Xshd;

/// <summary>
/// A &lt;SyntaxDefinition&gt; element.
/// </summary>
[XmlRoot("SyntaxDefinition")]
public class XshdSyntaxDefinition
{
	#region Constructors

	/// <summary>
	/// Creates a new XshdSyntaxDefinition object.
	/// </summary>
	public XshdSyntaxDefinition()
	{
		Elements = new NullSafeCollection<XshdElement>();
		Extensions = new NullSafeCollection<string>();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the collection of elements.
	/// </summary>
	public IList<XshdElement> Elements { get; }

	/// <summary>
	/// Gets the associated extensions.
	/// </summary>
	[XmlAttribute]
	public IList<string> Extensions { get; }

	/// <summary>
	/// Gets/sets the definition name
	/// </summary>
	[XmlAttribute]
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

	#endregion
}