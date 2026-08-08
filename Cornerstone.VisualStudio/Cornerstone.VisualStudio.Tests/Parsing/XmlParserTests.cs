#region References

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Cornerstone.VisualStudio.Core.Parsing;
using Xunit;

#endregion

namespace Cornerstone.VisualStudio.Tests.Parsing;

/// <summary>
/// Tests for XmlParser behavior on which TextManipulator is dependent
/// </summary>
public class XmlParserTests
{
	#region Methods

	[Fact]
	public void ShouldBeInClosingTagWhenInsideEndTag()
	{
		var p = XmlParser.Parse("<Grid></Grid");
		Assert.True(p.IsInClosingTag);
	}

	[Fact]
	public void ShouldBeInClosingTagWhenParsedSlash()
	{
		var p = XmlParser.Parse("<Grid></");
		Assert.True(p.IsInClosingTag);
	}

	[Fact]
	public void ShouldBeInNoneStateWhenOnClosingBrace()
	{
		var parser = XmlParser.Parse("<Grid>");
		Assert.Equal(XmlParser.ParserState.None, parser.State);
	}

	[Fact]
	public void ShouldFailOnInavlidNesting()
	{
		var data = "<Grid><Foo></Grid>";
		var ppos = "<Grid".Length;
		var seek = data.Length;

		var p = XmlParser.Parse(data.AsMemory(), 0, ppos);
		var result = p.SeekClosingTag();

		Assert.False(result);
		Assert.Equal(seek, p.ParserPos);
	}

	[Theory]
	[InlineData("<UserControl x:DataType=\"Button\"><TextBlock Tag=\"\"")]
	[InlineData("<UserControl x:DataType= \"Button\"><TextBlock Tag=\"\"")]
	[InlineData("<UserControl x:DataType = \"Button\"><TextBlock Tag=\"\"")]
	[InlineData("<UserControl x:DataType =\"Button\"><TextBlock Tag=\"\"")]
	[InlineData("<UserControl x:DataType\t=\r\"Button\"><TextBlock Tag=\"\"")]
	[InlineData("<UserControl x:DataType\t=\n\"Button\"><TextBlock Tag=\"\"")]
	[InlineData("<UserControl x:DataType \t=\r\"Button\"><TextBlock Tag=\"\"")]
	[InlineData("<UserControl x:DataType\t =\r\"Button\"><TextBlock Tag=\"\"")]
	public void ShouldFindParentAttributeValue(string source)
	{
		var state = XmlParser.Parse(source.AsMemory(), source.Length, 0);
		Assert.NotNull(state.FindParentAttributeValue("(x\\:)?DataType"));
	}

	[Theory]
	[InlineData("OneLevel", 492, 1, 1, "Window")]
	[InlineData("OneLevelWithCDATA", 520, 1, 1, "Window")]
	[InlineData("OneLevelWithComment", 512, 1, 1, "Window")]
	[InlineData("TwoLevel", 512, 1, 2, "Window.Styles")]
	[InlineData("TwoLevelWithCDATA", 554, 1, 2, "Window.Styles")]
	[InlineData("TwoLevelWithComment", 88, 1, 2, "Window.Styles")]
	public void ShouldGetParentTagNameAtLevel(string source, int position, int level, int nestingLevelExpected, string expectedParentTag)
	{
		var data = GetData(source);
		var state = XmlParser.Parse(data.AsMemory(), position, 0);
		Assert.NotNull(state);
		Assert.Equal(nestingLevelExpected, state.NestingLevel);
		var parentTag = state.GetParentTagName(level);
		Assert.Equal(expectedParentTag, parentTag);
	}

	[Fact]
	public void ShouldMoveBackTo0NestingWhenParsedClosedTag()
	{
		var p = XmlParser.Parse("<Grid><Foo></Foo></");
		Assert.Equal(0, p.NestingLevel);
	}

	[Fact]
	public void ShouldMoveBackTo0NestingWhenParsedDeclarationTag()
	{
		var p = XmlParser.Parse("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
		Assert.Equal(0, p.NestingLevel);
	}

	[Fact]
	public void ShouldMoveBackTo0NestingWhenParsedSelfclosedTag()
	{
		var p = XmlParser.Parse("<Grid><Foo/></");
		Assert.Equal(0, p.NestingLevel);
	}

	[Fact]
	public void ShouldNotBeInClosingTagWhenStartTag()
	{
		var p = XmlParser.Parse("<Grid");
		Assert.False(p.IsInClosingTag);
	}

	[Fact]
	public void ShouldReturnCorrectTagName()
	{
		var p = XmlParser.Parse("<Grid><Tag Attribute=\"\"/");
		Assert.Equal("Tag", p.ParseCurrentTagName());
	}

	[Fact]
	public void ShouldSeekEndTagInOverClosedTag()
	{
		var data = "<Grid><Foo/></Grid>";
		var ppos = "<Grid".Length;
		var seek = "<Grid><Foo/></".Length;

		var p = XmlParser.Parse(data.AsMemory(), 0, ppos);
		var result = p.SeekClosingTag();

		Assert.True(result);
		Assert.Equal(seek, p.ParserPos);
	}

	[Fact]
	public void ShouldSeekEndTagInSimpleCase()
	{
		var data = "<Grid></Grid>";
		var ppos = "<Grid".Length;
		var seek = "<Grid></".Length;

		var p = XmlParser.Parse(data.AsMemory(), 0, ppos);
		var result = p.SeekClosingTag();

		Assert.True(result);
		Assert.Equal(seek, p.ParserPos);
	}

	private string GetData(string name, [CallerMemberName] string callerMethod = "")
	{
		var ass = GetType().Assembly;
		if (ass.GetManifestResourceNames()
				.FirstOrDefault(n => n.EndsWith($"{callerMethod}{name}.xml")) is string resName)
		{
			using var stream = ass.GetManifestResourceStream(resName);
			return new StreamReader(stream).ReadToEnd();
		}
		return default;
	}

	#endregion
}