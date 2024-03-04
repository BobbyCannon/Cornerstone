#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Cornerstone.Parsers.Xml;

public class XmlComment : XmlElement
{
	#region Constructors

	/// <inheritdoc />
	public XmlComment(string comment) : base("Comment")
	{
		ElementValue = comment;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void Write(XmlTextBuilder writer)
	{
		writer.PrepareForChildren(x => x.HasChildren = true);
		writer.NewLine();
		writer.Append("<!--");
		writer.Append(ElementValue);
		writer.Append("-->");
	}

	#endregion
}