#region References

using System.IO;
using System.Text;
using Cornerstone.Avalonia;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia;

[TestClass]
public class ThemeColorPaletteTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GenerateCode()
	{
		var b = new StringBuilder();
		b.AppendLine("BlueColors = [");
		for (var i = 0; i < ThemeColorPalette.BlueColors.Count; i++)
		{
			var color = ThemeColorPalette.BlueColors[i];
			b.AppendLine($"\tnew(\"ThemeColor0{i}\", \"{color}\"),");
		}
		b.Append("];");
		b.Dump();

		CopyToClipboard(b.ToString());

		foreach (var details in ThemeColorPalette.ThemeColors)
		{
			//WriteThemeResource(details);
		}
	}

	private void WriteThemeResource(ThemeColorPaletteDetails details)
	{
		var b = new StringBuilder();
		b.AppendLine("<ResourceDictionary xmlns=\"https://github.com/avaloniaui\"");
		b.AppendLine("\t\txmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");
		b.AppendLine();

		foreach (var color in details.Colors)
		{
			b.AppendLine($"\t<Color x:Key=\"{color.Name}\">{color.Background}</Color>");
		}

		b.AppendLine();

		foreach (var color in details.Colors)
		{
			b.AppendLine($"\t<Color x:Key=\"{color.Name}\">{color.Foreground}</Color>");
		}

		b.Append("</ResourceDictionary>");
		b.Dump();

		var filePath = $"{SolutionDirectory}\\Cornerstone.Avalonia\\Resources\\Color\\ThemeColor{details.Name}.xaml";
		filePath.Dump();

		IsTrue(File.Exists(filePath));

		File.WriteAllText(filePath, b.ToString(), Encoding.Default);
	}

	#endregion
}