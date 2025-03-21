#region References

using System.Xml;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Generators;

public static class XamlStyleEditor
{
	#region Methods

	public static TextBuilder GenerateSvgSample(string xmlPath)
	{
		using var reader = new XmlTextReader(xmlPath);
		var output = new TextBuilder();

		while (reader.Read())
		{
			if ((reader.Name == "StreamGeometry")
				&& (reader.NodeType == XmlNodeType.Element))
			{
				var name = reader.GetAttribute("x:Key");
				output.AppendLine("<StackPanel>");
				output.AppendLine($"\t<Path Classes=\"Icon\" Data=\"{{StaticResource {name}}}\" ToolTip.Tip=\"{name}\" />");
				output.AppendLine($"\t<TextBlock Classes=\"IconHeader\" Text=\"{name}\"></TextBlock>");
				output.AppendLine("</StackPanel>");
			}
		}

		reader.Close();
		return output;
	}

	#endregion
}