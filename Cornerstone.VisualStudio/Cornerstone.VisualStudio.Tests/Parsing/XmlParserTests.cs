#region References

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Cornerstone.VisualStudio.Core.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests.Parsing;

/// <summary>
/// Tests for XmlParser behavior on which TextManipulator is dependent
/// </summary>
[TestClass]
public class XmlParserTests
{
	#region Methods

	[TestMethod]
	public void ShouldBeInClosingTagWhenInsideEndTag()
	{
		var p = XmlParser.Parse("<Grid></Grid");
		Assert.IsTrue(p.IsInClosingTag);
	}

	[TestMethod]
	public void ShouldBeInClosingTagWhenParsedSlash()
	{
		var p = XmlParser.Parse("<Grid></");
		Assert.IsTrue(p.IsInClosingTag);
	}

	[TestMethod]
	public void ShouldBeInNoneStateWhenOnClosingBrace()
	{
		var parser = XmlParser.Parse("<Grid>");
		Assert.AreEqual(XmlParser.ParserState.None, parser.State);
	}

	[TestMethod]
	public void ShouldFailOnInavlidNesting()
	{
		var data = "<Grid><Foo></Grid>";
		var ppos = "<Grid".Length;
		var seek = data.Length;

		var p = XmlParser.Parse(data.AsMemory(), 0, ppos);
		var result = p.SeekClosingTag();

		Assert.IsFalse(result);
		Assert.AreEqual(seek, p.ParserPos);
	}

	[TestMethod]
	[DataRow("<UserControl x:DataType=\"Button\"><TextBlock Tag=\"\"")]
	[DataRow("<UserControl x:DataType= \"Button\"><TextBlock Tag=\"\"")]
	[DataRow("<UserControl x:DataType = \"Button\"><TextBlock Tag=\"\"")]
	[DataRow("<UserControl x:DataType =\"Button\"><TextBlock Tag=\"\"")]
	[DataRow("<UserControl x:DataType\t=\r\"Button\"><TextBlock Tag=\"\"")]
	[DataRow("<UserControl x:DataType\t=\n\"Button\"><TextBlock Tag=\"\"")]
	[DataRow("<UserControl x:DataType \t=\r\"Button\"><TextBlock Tag=\"\"")]
	[DataRow("<UserControl x:DataType\t =\r\"Button\"><TextBlock Tag=\"\"")]
	public void ShouldFindParentAttributeValue(string source)
	{
		var state = XmlParser.Parse(source.AsMemory(), source.Length, 0);
		Assert.IsNotNull(state.FindParentAttributeValue("(x\\:)?DataType"));
	}

	[TestMethod]
	[DataRow("OneLevel", 492, 1, 1, "Window")]
	[DataRow("OneLevelWithCDATA", 520, 1, 1, "Window")]
	[DataRow("OneLevelWithComment", 512, 1, 1, "Window")]
	[DataRow("TwoLevel", 512, 1, 2, "Window.Styles")]
	[DataRow("TwoLevelWithCDATA", 554, 1, 2, "Window.Styles")]
	[DataRow("TwoLevelWithComment", 88, 1, 2, "Window.Styles")]
	public void ShouldGetParentTagNameAtLevel(string source, int position, int level, int nestingLevelExpected, string expectedParentTag)
	{
		var data = GetData(source);
		var state = XmlParser.Parse(data.AsMemory(), position, 0);
		Assert.IsNotNull(state);
		Assert.AreEqual(nestingLevelExpected, state.NestingLevel);
		var parentTag = state.GetParentTagName(level);
		Assert.AreEqual(expectedParentTag, parentTag);
	}

	[TestMethod]
	public void ShouldMoveBackTo0NestingWhenParsedClosedTag()
	{
		var p = XmlParser.Parse("<Grid><Foo></Foo></");
		Assert.AreEqual(0, p.NestingLevel);
	}

	[TestMethod]
	public void ShouldMoveBackTo0NestingWhenParsedDeclarationTag()
	{
		var p = XmlParser.Parse("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
		Assert.AreEqual(0, p.NestingLevel);
	}

	[TestMethod]
	public void ShouldMoveBackTo0NestingWhenParsedSelfclosedTag()
	{
		var p = XmlParser.Parse("<Grid><Foo/></");
		Assert.AreEqual(0, p.NestingLevel);
	}

	[TestMethod]
	public void ShouldNotBeInClosingTagWhenStartTag()
	{
		var p = XmlParser.Parse("<Grid");
		Assert.IsFalse(p.IsInClosingTag);
	}

	[TestMethod]
	public void ShouldReturnCorrectTagName()
	{
		var p = XmlParser.Parse("<Grid><Tag Attribute=\"\"/");
		Assert.AreEqual("Tag", p.ParseCurrentTagName());
	}

	[TestMethod]
	public void ShouldSeekEndTagInOverClosedTag()
	{
		var data = "<Grid><Foo/></Grid>";
		var ppos = "<Grid".Length;
		var seek = "<Grid><Foo/></".Length;

		var p = XmlParser.Parse(data.AsMemory(), 0, ppos);
		var result = p.SeekClosingTag();

		Assert.IsTrue(result);
		Assert.AreEqual(seek, p.ParserPos);
	}

	[TestMethod]
	public void ShouldSeekEndTagInSimpleCase()
	{
		var data = "<Grid></Grid>";
		var ppos = "<Grid".Length;
		var seek = "<Grid></".Length;

		var p = XmlParser.Parse(data.AsMemory(), 0, ppos);
		var result = p.SeekClosingTag();

		Assert.IsTrue(result);
		Assert.AreEqual(seek, p.ParserPos);
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