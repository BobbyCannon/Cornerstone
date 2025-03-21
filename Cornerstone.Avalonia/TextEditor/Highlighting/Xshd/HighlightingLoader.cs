#region References

using System;
using System.Xml;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Highlighting.Xshd;

/// <summary>
/// Static class with helper methods to load XSHD highlighting files.
/// </summary>
public static class HighlightingLoader
{
	#region Methods

	/// <summary>
	/// Creates a highlighting definition from the XSHD file.
	/// </summary>
	public static IHighlightingDefinition Load(string resourceName, XshdSyntaxDefinition syntaxDefinition, IHighlightingDefinitionReferenceResolver resolver)
	{
		if (syntaxDefinition == null)
		{
			throw new ArgumentNullException(nameof(syntaxDefinition));
		}
		return new XmlHighlightingDefinition(resourceName, syntaxDefinition, resolver);
	}

	/// <summary>
	/// Creates a highlighting definition from the XSHD file.
	/// </summary>
	public static IHighlightingDefinition Load(string resourceName, XmlReader reader, IHighlightingDefinitionReferenceResolver resolver)
	{
		return Load(resourceName, LoadXshd(reader), resolver);
	}

	public static XshdSyntaxDefinition LoadXshd(XmlReader reader)
	{
		if (reader == null)
		{
			throw new ArgumentNullException(nameof(reader));
		}
		try
		{
			reader.MoveToContent();
			return XshdLoader.LoadDefinition(reader);
		}
		catch (XmlException ex)
		{
			throw WrapException(ex, ex.LineNumber, ex.LinePosition);
		}
	}

	internal static string FormatExceptionMessage(string message, int lineNumber, int linePosition)
	{
		if (lineNumber <= 0)
		{
			return message;
		}
		return "Error at position (line " + lineNumber + ", column " + linePosition + "):\n" + message;
	}

	internal static XmlReader GetValidatingReader(XmlReader input, bool ignoreWhitespace)
	{
		var settings = new XmlReaderSettings
		{
			CloseInput = true,
			IgnoreComments = true,
			IgnoreWhitespace = ignoreWhitespace
		};
		return XmlReader.Create(input, settings);
	}

	private static Exception WrapException(Exception ex, int lineNumber, int linePosition)
	{
		return new HighlightingDefinitionInvalidException(FormatExceptionMessage(ex.Message, lineNumber, linePosition), ex);
	}

	#endregion

	//internal static XmlSchemaSet LoadSchemaSet(XmlReader schemaInput)
	//{
	//    XmlSchemaSet schemaSet = new XmlSchemaSet();
	//    schemaSet.Add(null, schemaInput);
	//    schemaSet.ValidationEventHandler += delegate (object sender, ValidationEventArgs args)
	//    {
	//        throw new HighlightingDefinitionInvalidException(args.Message);
	//    };
	//    return schemaSet;
	//}
}