#region References

using Cornerstone.Avalonia;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Resources;

[TestClass]
public class ThemeBuilderTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ToggleButtonTheme()
	{
		var template = """
						<!-- $Name theme. -->
						<Style Selector="^[(Theme.Color)=$Name]">
							<Setter Property="Background" Value="{DynamicResource ThemeColor06}" />
							<Style Selector="^:pointerover">
								<Setter Property="Background" Value="{DynamicResource ThemeColor07}" />
							</Style>
							<Style Selector="^:pressed">
								<Setter Property="Background" Value="{DynamicResource ThemeColor04}" />
							</Style>
							<Style Selector="^:disabled">
								<Setter Property="Background" Value="{DynamicResource ThemeColor03}" />
								<Setter Property="Foreground" Value="{DynamicResource ThemeColor01}" />
							</Style>
							<Style Selector="^:checked">
								<Setter Property="Background" Value="{DynamicResource ThemeColor08}" />
								<Style Selector="^:pointerover">
									<Setter Property="Background" Value="{DynamicResource ThemeColor07}" />
								</Style>
								<Style Selector="^:pressed">
									<Setter Property="Background" Value="{DynamicResource ThemeColor07}" />
								</Style>
								<Style Selector="^:disabled">
									<Setter Property="Background" Value="{DynamicResource ThemeColor04}" />
									<Setter Property="Foreground" Value="{DynamicResource ThemeColor01}" />
								</Style>
							</Style>
						</Style>
						""";

		var builder = new TextBuilder();
		builder.AppendLine(template.Replace("$Name", "Current"));
		builder.AppendLine();

		foreach (var color in Theme.Colors)
		{
			builder.AppendLine(
				template.Replace("$Name", color.ToString())
					.Replace("ThemeColor", color.ToString())
			);
			builder.AppendLine();
		}

		builder.Trim();

		CopyToClipboard(builder.ToString()).Dump();
	}

	#endregion
}