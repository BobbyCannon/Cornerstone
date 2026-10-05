#region References

using Cornerstone.VisualStudio.EditorHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests;

[TestClass]
public class GridDefinitionsSuggestionTests
{
	#region Methods

	[TestMethod]
	public void ConvertsColumnDefinitionsToAttribute()
	{
		var text = "<Grid>\r\n            <Grid.ColumnDefinitions>\r\n            <ColumnDefinition Width=\"*\"></ColumnDefinition>\r\n            <ColumnDefinition Width=\"Auto\"></ColumnDefinition>\r\n        </Grid.ColumnDefinitions>\r\n</Grid>";
		var caret = text.IndexOf("Width=\"Auto\"");
		Assert.IsTrue(GridDefinitionsSuggestion.TryConvert(text, caret, out var conversion));
		Assert.AreEqual("ColumnDefinitions", conversion.AttributeName);
		Assert.AreEqual("*,Auto", conversion.AttributeValue);

		var removed = text.Remove(conversion.RemoveStart, conversion.RemoveLength);
		var edited = removed.Insert(conversion.InsertAt, " " + conversion.AttributeName + "=\"" + conversion.AttributeValue + "\"");
		Assert.AreEqual("<Grid ColumnDefinitions=\"*,Auto\">\r\n</Grid>", edited);
	}

	[TestMethod]
	public void ConvertsRowDefinitionsToAttribute()
	{
		var text = "<Grid><Grid.RowDefinitions><RowDefinition Height=\"Auto\"/><RowDefinition/></Grid.RowDefinitions></Grid>";
		var caret = text.IndexOf("RowDefinition Height");
		Assert.IsTrue(GridDefinitionsSuggestion.TryConvert(text, caret, out var conversion));
		Assert.AreEqual("RowDefinitions", conversion.AttributeName);
		Assert.AreEqual("Auto,*", conversion.AttributeValue);
		var removed = text.Remove(conversion.RemoveStart, conversion.RemoveLength);
		var edited = removed.Insert(conversion.InsertAt, " " + conversion.AttributeName + "=\"" + conversion.AttributeValue + "\"");
		Assert.AreEqual("<Grid RowDefinitions=\"Auto,*\"></Grid>", edited);
	}

	[TestMethod]
	public void SkipsDefinitionsThatSetMinWidth()
	{
		var text = "<Grid><Grid.ColumnDefinitions><ColumnDefinition Width=\"*\" MinWidth=\"10\"/></Grid.ColumnDefinitions></Grid>";
		var caret = text.IndexOf("MinWidth");
		Assert.IsFalse(GridDefinitionsSuggestion.TryConvert(text, caret, out _));
	}

	[TestMethod]
	public void ConvertsColumnDefinitionsAttributeToElements()
	{
		var text = "<Grid ColumnDefinitions=\"*,Auto\">\r\n</Grid>";
		var caret = text.IndexOf("*,Auto");
		Assert.IsTrue(GridDefinitionsSuggestion.TryConvert(text, caret, out var conversion));
		Assert.AreEqual("Convert to element", conversion.DisplayText);
		Assert.AreEqual("*,Auto", conversion.AttributeValue);
		Assert.AreEqual(
			"<Grid>\r\n" +
			"    <Grid.ColumnDefinitions>\r\n" +
			"        <ColumnDefinition Width=\"*\" />\r\n" +
			"        <ColumnDefinition Width=\"Auto\" />\r\n" +
			"    </Grid.ColumnDefinitions>\r\n" +
			"</Grid>",
			Apply(text, conversion));
	}

	[TestMethod]
	public void ConvertsRowDefinitionsAttributeToElements()
	{
		var text = "\t<Grid RowDefinitions=\"Auto,2*\"></Grid>";
		var caret = text.IndexOf("RowDefinitions");
		Assert.IsTrue(GridDefinitionsSuggestion.TryConvert(text, caret, out var conversion));
		Assert.AreEqual(
			"\t<Grid>\n" +
			"\t\t<Grid.RowDefinitions>\n" +
			"\t\t\t<RowDefinition Height=\"Auto\" />\n" +
			"\t\t\t<RowDefinition Height=\"2*\" />\n" +
			"\t\t</Grid.RowDefinitions></Grid>",
			Apply(text, conversion));
	}

	[TestMethod]
	public void ConvertsSelfClosingGridAndKeepsOtherAttributes()
	{
		var text = "    <Grid Margin=\"4\" ColumnDefinitions=\"1*,Auto\"/>";
		var caret = text.IndexOf("1*");
		Assert.IsTrue(GridDefinitionsSuggestion.TryConvert(text, caret, out var conversion));
		Assert.AreEqual(
			"    <Grid Margin=\"4\">\n" +
			"        <Grid.ColumnDefinitions>\n" +
			"            <ColumnDefinition Width=\"*\" />\n" +
			"            <ColumnDefinition Width=\"Auto\" />\n" +
			"        </Grid.ColumnDefinitions>\n" +
			"    </Grid>",
			Apply(text, conversion));
	}

	[TestMethod]
	public void SkipsAttributeWhenDefinitionsElementAlreadyExists()
	{
		var text = "<Grid ColumnDefinitions=\"*\"><Grid.ColumnDefinitions><ColumnDefinition Width=\"*\"/></Grid.ColumnDefinitions></Grid>";
		var caret = text.IndexOf("ColumnDefinitions=\"");
		Assert.IsFalse(GridDefinitionsSuggestion.TryConvert(text, caret, out _));
	}

	private static string Apply(string text, GridDefinitionsSuggestion.Conversion conversion)
	{
		var removed = text.Remove(conversion.RemoveStart, conversion.RemoveLength);
		var insertAt = conversion.InsertAt;
		if (insertAt > conversion.RemoveStart)
		{
			insertAt -= conversion.RemoveLength;
		}

		return removed.Insert(insertAt, conversion.Insertion);
	}

	#endregion
}
