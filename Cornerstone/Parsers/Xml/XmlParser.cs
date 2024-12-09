#region References

using System.IO;
using System.Xml;
using System.Xml.Linq;

#endregion

namespace Cornerstone.Parsers.Xml;

/// <summary>
/// Represents a parser for XML.
/// </summary>
public class XmlParser : Parser<XmlParserSettings>
{
	#region Fields

	private static XmlParser _instance;
	private static readonly object _syncRoot;

	#endregion

	#region Constructors

	/// <inheritdoc />
	public XmlParser() : this(new XmlParserSettings())
	{
	}

	/// <inheritdoc />
	public XmlParser(XmlParserSettings settings) : base(settings)
	{
	}

	static XmlParser()
	{
		_syncRoot = new object();
	}

	#endregion

	#region Properties

	/// <summary>
	/// The singleton instance.
	/// </summary>
	public static XmlParser Instance
	{
		get
		{
			if (_instance != null)
			{
				return _instance;
			}

			lock (_syncRoot)
			{
				_instance ??= new XmlParser();
			}

			return _instance;
		}
	}

	#endregion

	#region Methods

	/// <summary>
	/// </summary>
	/// <param name="text"> </param>
	/// <param name="settings"> </param>
	/// <returns> </returns>
	public static string Format(string text, XmlParserSettings settings = null)
	{
		settings ??= new XmlParserSettings();

		var xws = new XmlWriterSettings
		{
			Indent = true,
			IndentChars = new string(settings.IndentCharacter, settings.IndentionCount),
			NewLineChars = "\r\n",
			OmitXmlDeclaration = settings.OmitXmlDeclaration
		};

		if (settings.Minify)
		{
			xws.Indent = false;
		}

		xws.ConformanceLevel = ConformanceLevel.Fragment;

		var document = XElement.Parse(text);
		using var memory = new MemoryStream();
		using (var xw = XmlWriter.Create(memory, xws))
		{
			document.WriteTo(xw);
		}

		memory.Position = 0;

		using var reader = new StreamReader(memory);
		return reader.ReadToEnd();
	}

	#endregion
}