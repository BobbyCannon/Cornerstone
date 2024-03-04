namespace Cornerstone.Parsers.Xml;

/// <summary>
/// Options for the XML parser.
/// </summary>
public class XmlParserOptions : ParserOptions
{
	#region Constructors

	/// <inheritdoc />
	public XmlParserOptions()
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