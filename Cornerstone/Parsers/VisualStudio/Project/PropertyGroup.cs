#region References

using System.Xml.Linq;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Project;

public class PropertyGroup
{
	#region Fields

	private readonly XElement _element;

	#endregion

	#region Constructors

	public PropertyGroup(XElement element)
	{
		_element = element;
	}

	#endregion
}