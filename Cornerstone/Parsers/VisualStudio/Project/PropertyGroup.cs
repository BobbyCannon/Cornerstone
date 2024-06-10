#region References

using Cornerstone.Parsers.Xml;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Project;

public class PropertyGroup : XmlElement
{
	#region Constructors

	/// <inheritdoc />
	public PropertyGroup() : base("PropertyGroup")
	{
	}

	#endregion
}