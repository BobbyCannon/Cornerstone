namespace Cornerstone.Parsers.Xml;

/// <summary>
/// Options for the XML parser.
/// </summary>
public class XmlParserSettings : ParserSettings
{
	#region Constructors

	/// <inheritdoc />
	public XmlParserSettings()
	{
		OmitXmlDeclaration = false;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Options to omit XML declaration during processing.
	/// </summary>
	public bool OmitXmlDeclaration { get; set; }

	#endregion
}