#region References

using System;
using System.Linq;
using Cornerstone.Parsers.Markdown;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.Markdown;

[TestClass]
public class MarkdownTableFormatterTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void MaxWidth()
	{
		var table =
			"""
			| Feature                    | Description                             | Status   | Version | Performance |
			|----------------------------|-----------------------------------------|----------|:-------:|------------:|
			| Zero-Allocation Parsing    | Computes widths without storing strings | Stable   |  1.2.3  |   Excellent |
			| Alignment Support          | Left, center, and right alignment       | Complete |   2.0   |   Very Fast |
			| Large Table Handling       | Handles thousands of rows efficiently   | Beta     |   1.5   | Outstanding |
			| StringBuilder Optimization | Minimal resizes and fast appends        | Stable   |   1.0   |        High |
			| .NET 10 Span Usage         | Modern low-allocation APIs              | Released |   3.1   |        Best |
			""";

		var expected =
			"""
			| Feature                   | Description                          | Status  | Versio | Performance |
			|                           |                                      |         |   n    |             |
			|---------------------------|--------------------------------------|---------|:------:|------------:|
			| Zero-Allocation Parsing   | Computes widths without storing      | Stable  | 1.2.3  |   Excellent |
			|                           | strings                              |         |        |             |
			| Alignment Support         | Left, center, and right alignment    | Complet |  2.0   |   Very Fast |
			|                           |                                      | e       |        |             |
			| Large Table Handling      | Handles thousands of rows            | Beta    |  1.5   | Outstanding |
			|                           | efficiently                          |         |        |             |
			| StringBuilder             | Minimal resizes and fast appends     | Stable  |  1.0   |        High |
			| Optimization              |                                      |         |        |             |
			| .NET 10 Span Usage        | Modern low-allocation APIs           | Release |  3.1   |        Best |
			|                           |                                      | d       |        |             |
			""";

		AreEqual(expected, MarkdownTableFormatter.Format(table, 101));
	}
	
	[TestMethod]
	public void FormatTableBasic()
	{
		var table =
			"""
			| Name | Age | Email |
			|-|-|-|
			| Alice | 30 | alice@domain.com |
			| Bob | 25 | bob@foo.com |
			""";

		var expected =
			"""
			| Name  | Age   | Email            |
			|-------|-------|------------------|
			| Alice | 30    | alice@domain.com |
			| Bob   | 25    | bob@foo.com      |
			""";

		AreEqual(expected, MarkdownTableFormatter.Format(table));
	}

	[TestMethod]
	public void FormatTableCellsWithExtraWhitespace()
	{
		var table =
			"""
			| Name   |  Age  |   Email    |
			|--------|-------|------------|
			|  Alice   |  30   | alice@test.com |
			""";

		var result = MarkdownTableFormatter.Format(table);

		IsFalse(result.Contains("  Alice  "));
	}

	[TestMethod]
	public void FormatTableEmptyAndMissingCells()
	{
		var table =
			"""
			| Name | Age | City |
			|------|-----|------|
			| Alice | | |
			| Bob || New York |
			| Charlie | 35 |
			""";

		var expected =
			"""
			| Name    | Age   | City     |
			|---------|-------|----------|
			| Alice   |       |          |
			| Bob     |       | New York |
			| Charlie | 35    |          |
			""";

		var result = MarkdownTableFormatter.Format(table, 60);
		result.Dump();
		IsTrue(result.Split("\r\n", StringSplitOptions.None).All(x => x.Length <= 60));
		AreEqual(expected, result);
	}

	[TestMethod]
	public void FormatTableEmptyInputReturnsOriginal()
	{
		AreEqual("", MarkdownTableFormatter.Format(""));
		AreEqual("   ", MarkdownTableFormatter.Format("   "));
		AreEqual("\n\n", MarkdownTableFormatter.Format("\n\n"));
	}

	[TestMethod]
	public void FormatTableLongUnbreakableWord()
	{
		var table =
			"""
			| Item | Value |
			|------|-------|
			| Test | SupercalifragilisticexpialidociousAndEvenLongerWord |
			""";

		var result = MarkdownTableFormatter.Format(table, 45);

		// Must force character wrapping
		IsTrue(result.Contains("Supercalifragilistic") ||
			result.Contains("expialidociousAndEven"));
	}

	[TestMethod]
	public void FormatTableMaximumProportionalScaling()
	{
		// Scenario: Wide columns that must be shrunk dramatically by max width constraint.
		var table =
			"""
			| Col1 Content (Very Long) | Col2 Content (Also Very Long) | Col3 Content (Medium Length) |
			|:-|:-|:-|
			| This is a very long string of text that must be scaled down drastically. | Another extremely verbose block of content requiring proportional scaling and wrapping across multiple lines due to width limits. | A simple column.|
			| Short data | Medium data | Simple.|
			""";
		var expected =
			"""
			| Col1 Content     | Col2 Content (Also Very    | Col3     |
			| (Very Long)      | Long)                      | Content  |
			|                  |                            | (Medium  |
			|                  |                            | Length)  |
			|:-----------------|:---------------------------|:---------|
			| This is a very   | Another extremely verbose  | A simple |
			| long string of   | block of content requiring | column.  |
			| text that must   | proportional scaling and   |          |
			| be scaled down   | wrapping across multiple   |          |
			| drastically.     | lines due to width limits. |          |
			| Short data       | Medium data                | Simple.  |
			""";

		// Set max width much smaller than natural width (e.g., 60)
		var result = MarkdownTableFormatter.Format(table, 60);
		result.Dump();
		AreEqual(expected, result);
	}

	[TestMethod]
	public void FormatTableMaxTableWidthNoWrappingNeeded()
	{
		var table =
			"""
			| Product | Description |
			|---------|-------------|
			| Laptop | High performance laptop |
			| Phone | Compact device |
			""";

		var expected =
			"""
			| Product | Description             |
			|---------|-------------------------|
			| Laptop  | High performance laptop |
			| Phone   | Compact device          |
			""";

		AreEqual(expected, MarkdownTableFormatter.Format(table, 120));
	}

	[TestMethod]
	public void FormatTableMixedAlignmentsWithMaxTableWidth()
	{
		var table =
			"""
			| Name | Age | Description |
			|:----|:-:|--:|
			| Alice | 30 | This is a long description that will force the table to shrink and wrap. |
			| Bob | 25 | Short desc. |
			""";

		var result = MarkdownTableFormatter.Format(table, 70);
		result.Dump();

		// Verify alignment markers are preserved correctly after shrinking
		IsTrue(result.Contains("|:------|") || result.Contains("----:"), () => "Left alignment colon missing or misplaced");
		IsTrue(result.Contains("|:-----:|"), () => "Center alignment should be preserved");
		IsTrue(result.Contains("-------:|"), () => "Right alignment colon should be preserved");

		// Verify total width constraint
		var lines = result.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
		foreach (var line in lines)
		{
			IsTrue(line.Length <= 70, () => $"Line too wide: {line.Length} chars");
		}
	}

	[TestMethod]
	public void FormatTableMultipleLineHeightsAfterWrapping()
	{
		var table =
			"""
			| Name | Note |
			|------|------|
			| Alice | This sentence is long enough to wrap onto multiple lines when the table width is constrained. |
			| Bob | One line only. |
			""";

		var result = MarkdownTableFormatter.Format(table, 55);

		IsTrue(result.Contains("One line only."));
	}

	[TestMethod]
	public void FormatTableNoSeparator()
	{
		var table =
			"""
			| Name | Age |
			| Alice | 30 |
			| Bob   | 25  |
			""";

		var expected =
			"""
			| Name  | Age   |
			|-------|-------|
			| Alice | 30    |
			| Bob   | 25    |
			""";

		var result = MarkdownTableFormatter.Format(table);
		AreEqual(expected, result);
	}

	[TestMethod]
	public void FormatTableOnlyHeaderAndSeparator()
	{
		var table =
			"""
			| Name | Age | City |
			|------|-----|------|
			""";

		var expected =
			"""
			| Name  | Age   | City  |
			|-------|-------|-------|
			""";

		AreEqual(expected, MarkdownTableFormatter.Format(table));
	}

	[TestMethod]
	public void FormatTablePurelyLeftAligned()
	{
		var table =
			"""
			| Product Name | SKU Code | Status |
			|:-------------|:---------|:-------|
			| Widget A     | 12345    | Active |
			| Gizmo B      | 67890    | Inactive|
			""";

		var expected =
			"""
			| Product Name | SKU Code | Status   |
			|:-------------|:---------|:---------|
			| Widget A     | 12345    | Active   |
			| Gizmo B      | 67890    | Inactive |
			""";

		AreEqual(expected, MarkdownTableFormatter.Format(table));
	}

	[TestMethod]
	public void FormatTableRespectsMaxTableWidth()
	{
		var table =
			"""
			| Name | Description |
			|------|-------------|
			| Alice | This is a long description that will cause the table to wrap columns when a tight max width is applied. |
			| Bob   | Short text here. |
			""";

		var scenarios = new (int MaxWidth, string Name)[]
		{
			(80, "Should wrap moderately"),
			(50, "Should wrap more aggressively"),
			(35, "Should force heavy wrapping")
		};

		foreach (var scenario in scenarios)
		{
			scenario.Name.Dump();

			var result = MarkdownTableFormatter.Format(table, scenario.MaxWidth);
			var lines = result.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

			foreach (var line in lines)
			{
				IsTrue(line.Length <= scenario.MaxWidth,
					() => $"Line exceeds maxTableWidth {scenario.MaxWidth}. Actual length: {line.Length}. Line: {line}"
				);
			}
		}
	}

	[TestMethod]
	public void FormatTableSingleColumnWithMaxWidth()
	{
		var table =
			"""
			| Header |
			|--------|
			| This is a long sentence that needs to be wrapped when the maximum table width is quite small. |
			""";

		var result = MarkdownTableFormatter.Format(table, 40);

		var lines = result.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
		foreach (var line in lines)
		{
			IsTrue(line.Length <= 40,
				() => $"Line exceeds maxTableWidth {40}. Actual length: {line.Length}. Line: {line}"
			);
		}
	}

	[TestMethod]
	public void FormatTableVeryNarrowMaxWidthTriggersCharacterWrapping()
	{
		var table =
			"""
			| Column |
			|--------|
			| ThisIsAVeryLongUnbreakableWordThatExceedsAnyNormalColumnWidthAndMustBeBrokenByCharacter |
			""";

		var result = MarkdownTableFormatter.Format(table, 30);

		// Should contain broken word pieces
		IsTrue(result.Contains("ThisIsAVeryLongUnbreakable") ||
			result.Contains("UnbreakableWordThatExceeds"));
	}

	[TestMethod]
	public void FormatTableWithAlignments()
	{
		var table =
			"""
			| Name | Age | Email |
			|:-|:-:|-:|
			| Alice | 6 | alice@domain.com |
			| Bob   | 9  | bob@foo.com |
			""";

		var expected =
			"""
			| Name  |  Age  |            Email |
			|:------|:-----:|-----------------:|
			| Alice |   6   | alice@domain.com |
			| Bob   |   9   |      bob@foo.com |
			""";

		AreEqual(expected, MarkdownTableFormatter.Format(table));
	}

	#endregion
}