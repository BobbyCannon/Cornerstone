#region References

using System.IO;
using System.Xml;
using Cornerstone.Generators;
using Cornerstone.Text;
using Cornerstone.UnitTests.Generators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Themes;

[TestClass]
public class FontAwesomeIconsTests : CodeGeneratorTests
{
	#region Methods

	[TestMethod]
	public void GenerateScenarios()
	{
		if (!EnableFileUpdates && !IsDebugging)
		{
			return;
		}

		var xmlPath = $"{SolutionDirectory}\\Cornerstone.Avalonia\\Themes\\FontAwesomeIcons.axaml";
		IsTrue(File.Exists(xmlPath));

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

		FileModifier.UpdateFileIfNecessary("<!-- Generated Code - Preview -->\r\n", "<!-- /Generated Code - Preview -->", xmlPath, output.ToString());
	}

	#endregion
}