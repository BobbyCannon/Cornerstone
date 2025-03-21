#region References

using System.IO;
using System.Xml;
using Cornerstone.Generators;
using Cornerstone.UnitTests.Generators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Themes;

[TestClass]
public class IconsTests : CodeGeneratorTests
{
	#region Methods

	[TestMethod]
	public void GenerateScenarios()
	{
		if (!EnableFileUpdates && !IsDebugging)
		{
			return;
		}

		var xmlPath = $"{SolutionDirectory}\\Cornerstone.Avalonia\\Themes\\Icons.axaml";
		IsTrue(File.Exists(xmlPath));

		var output = XamlStyleEditor.GenerateSvgSample(xmlPath);
		FileModifier.UpdateFileIfNecessary("<!-- Generated Code - Preview -->\r\n", "<!-- /Generated Code - Preview -->", xmlPath, output.ToString());
	}

	#endregion
}