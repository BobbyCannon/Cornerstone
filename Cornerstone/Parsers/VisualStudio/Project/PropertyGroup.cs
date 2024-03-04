#region References

using Cornerstone.Parsers.Xml;

#endregion

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

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