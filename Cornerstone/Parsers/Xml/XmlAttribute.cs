#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Cornerstone.Parsers.Xml;

public class XmlAttribute
{
	#region Constructors

	public XmlAttribute()
	{
	}

	public XmlAttribute(string name, string value)
	{
		Name = name;
		Value = value;
	}

	#endregion

	#region Properties

	public string Name { get; set; }

	public string Value { get; set; }

	#endregion
}