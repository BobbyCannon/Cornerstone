#region References

using System.IO;
using Cornerstone.Avalonia;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Controls;

[TestClass]
public class GenerateThemeSelectorsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void UpdateColorThemeStyles()
	{
		if (!EnableFileUpdates && !IsDebugging)
		{
			"No files changed...".Dump();
			return;
		}

		var filesToUpdate = new[]
		{
			$"{SolutionDirectory}\\Cornerstone.Avalonia\\Controls\\ListBox.axaml"
		};

		var templateStart = "<!-- Theme Color - Template -->";
		var templateEnd = "<!-- Theme Color - /Template -->";
		var generatedStart = "<!-- Generated Code - ThemeColors -->";
		var generatedEnd = "<!-- Generated Code - /ThemeColors -->";

		foreach (var filePath in filesToUpdate)
		{
			if (!File.Exists(filePath))
			{
				Fail($"The file could not be found... {filePath}");
			}

			var content = File.ReadAllText(filePath);
			var templateStartIndex = content.IndexOf(templateStart);
			var templateEndIndex = content.IndexOf(templateEnd);

			if ((templateStartIndex < 0) || (templateEndIndex < 0))
			{
				Fail("Could not found the template...");
			}

			var generatedStartIndex = content.IndexOf(generatedStart);
			var generatedEndIndex = content.IndexOf(generatedEnd);

			if ((generatedStartIndex < 0) || (generatedEndIndex < 0))
			{
				Fail("Could not find the generated section...");
			}

			templateStartIndex += templateStart.Length;
			generatedStartIndex += generatedStart.Length;

			var template = content.Substring(templateStartIndex, templateEndIndex - templateStartIndex).Trim();
			content = content.Remove(generatedStartIndex, generatedEndIndex - generatedStartIndex);

			var builder = new TextBuilder();
			builder.AppendLine();
			builder.PushIndent();
			foreach (var color in Theme.Colors)
			{
				var colorTemplate = template
					.Replace("Current", color.ToString());

				for (var i = 0; i <= 9; i++)
				{
					colorTemplate = colorTemplate
						.Replace($"ThemeColor0{i}", $"{color}0{i}")
						.Replace($"ThemeForeground0{i}", $"{color}Text0{i}");
				}

				builder.AppendLine(colorTemplate);
			}
			builder.PopIndent();
			builder.AppendLine();

			content = content.Insert(generatedStartIndex, builder.ToString());
			File.WriteAllText(filePath, content);
		}
	}

	#endregion
}