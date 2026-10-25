#region References

using System.IO;
using System.Xml;

#endregion

namespace Cornerstone.VisualStudio.Core.Parsing;

/// <summary>
/// Whether a designer buffer is a complete XML document.
/// The previewer uses this to separate broken markup from a document the last build cannot load.
/// </summary>
public static class XamlDocument
{
	/// <summary>
	/// True when <paramref name="xaml"/> is one well-formed XML document.
	/// A missing type or property still parses. An unclosed tag, a bad quote, or mismatched elements do not.
	/// </summary>
	public static bool IsWellFormed(string xaml)
	{
		if (string.IsNullOrWhiteSpace(xaml))
		{
			return false;
		}

		var settings = new XmlReaderSettings();
		settings.ConformanceLevel = ConformanceLevel.Document;
		settings.DtdProcessing = DtdProcessing.Prohibit;
		settings.XmlResolver = null;
		settings.CloseInput = true;

		try
		{
			using (var reader = XmlReader.Create(new StringReader(xaml), settings))
			{
				while (reader.Read())
				{
				}
			}

			return true;
		}
		catch (XmlException)
		{
			return false;
		}
	}
}
