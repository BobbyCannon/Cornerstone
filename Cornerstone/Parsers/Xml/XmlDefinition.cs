#region References

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Cornerstone.Parsers.Xml;

/// <summary>
/// </summary>
/// <remarks>
/// version="1.0" encoding="utf-8"
/// </remarks>
public class XmlDeclaration
{
	#region Constructors

	public XmlDeclaration()
	{
	}

	public XmlDeclaration(string version) : this(version, null)
	{
	}

	public XmlDeclaration(string version, string encoding)
	{
		Encoding = encoding;
		Version = version;
	}

	#endregion

	#region Properties

	public string Encoding { get; set; }

	public string Version { get; set; }

	#endregion

	#region Methods

	public static XmlDeclaration FromAttributes(IList<XmlAttribute> attributes)
	{
		var response = new XmlDeclaration
		{
			Version = attributes.FirstOrDefault(x => string.Equals(x.Name, "Version", StringComparison.OrdinalIgnoreCase))?.Value,
			Encoding = attributes.FirstOrDefault(x => string.Equals(x.Name, "Encoding", StringComparison.OrdinalIgnoreCase))?.Value
		};
		return response;
	}

	#endregion
}