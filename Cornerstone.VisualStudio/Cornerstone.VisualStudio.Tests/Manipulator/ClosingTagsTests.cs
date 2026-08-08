#region References

using Cornerstone.VisualStudio.Tests.Manipulator.Util;
using Xunit;

#endregion

namespace Cornerstone.VisualStudio.Tests.Manipulator;

public class ClosingTagsTests : ManipulatorTestBase
{
	#region Methods

	[Fact]
	public void CloseTagAdvancedInContainedTag()
	{
		AssertInsertion("<Grid><Tag Attribute=\"\"$></Tag></Grid>", "/", "<Grid><Tag Attribute=\"\"/></Grid>");
	}

	[Fact]
	public void CloseTagWithSlash()
	{
		AssertInsertion("<Tag$", "/", "<Tag/>");
	}

	[Fact]
	public void CloseTagWithTrailingWhitespace()
	{
		AssertInsertion("<MenuItem Header=\"Header\" $>\r\n      </MenuItem >", "/", @"<MenuItem Header=""Header"" />");
	}

	[Fact]
	public void ConvertTagToSelfClosingWithSlash()
	{
		AssertInsertion("<Tag$></Tag>", "/", "<Tag/>");
	}

	[Fact]
	public void ConvertTagWithAttributesToSelfClosingWithSlash()
	{
		AssertInsertion("<Tag Attribute=\"value\"$></Tag>", "/", "<Tag Attribute=\"value\"/>");
	}

	[Fact]
	public void DoNotCloseEmptyTag()
	{
		AssertInsertion("<$", "/", "</");
	}

	[Fact]
	public void DoNotCloseTagWithAngleBracket()
	{
		// NOTE: Visual studio closes tags by itself, so we cannot implement this in completion engine
		// In visual studio result of such operation will be <Tag></Tag>
		AssertInsertion("<Tag$", ">", "<Tag>");
	}

	[Fact]
	public void DoNotConvertTagsWithNestedTag()
	{
		AssertInsertion("<Tag$><Foo/></Tag>", "/", "<Tag/><Foo/></Tag>");
	}

	[Fact]
	public void DoNotInsertEndingTwice()
	{
		AssertInsertion("<Tag$ >", "/", "<Tag/ >");
	}

	#endregion
}