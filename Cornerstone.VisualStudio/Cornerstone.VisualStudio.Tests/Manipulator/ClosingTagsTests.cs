#region References

using Cornerstone.VisualStudio.Tests.Manipulator.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests.Manipulator;

[TestClass]
public class ClosingTagsTests : ManipulatorTestBase
{
	#region Methods

	[TestMethod]
	public void CloseTagAdvancedInContainedTag()
	{
		AssertInsertion("<Grid><Tag Attribute=\"\"$></Tag></Grid>", "/", "<Grid><Tag Attribute=\"\"/></Grid>");
	}

	[TestMethod]
	public void CloseTagWithSlash()
	{
		AssertInsertion("<Tag$", "/", "<Tag/>");
	}

	[TestMethod]
	public void CloseTagWithTrailingWhitespace()
	{
		AssertInsertion("<MenuItem Header=\"Header\" $>\r\n      </MenuItem >", "/", @"<MenuItem Header=""Header"" />");
	}

	[TestMethod]
	public void ConvertTagToSelfClosingWithSlash()
	{
		AssertInsertion("<Tag$></Tag>", "/", "<Tag/>");
	}

	[TestMethod]
	public void ConvertTagWithAttributesToSelfClosingWithSlash()
	{
		AssertInsertion("<Tag Attribute=\"value\"$></Tag>", "/", "<Tag Attribute=\"value\"/>");
	}

	[TestMethod]
	public void DoNotCloseEmptyTag()
	{
		AssertInsertion("<$", "/", "</");
	}

	[TestMethod]
	public void DoNotCloseTagWithAngleBracket()
	{
		// NOTE: Visual studio closes tags by itself, so we cannot implement this in completion engine
		// In visual studio result of such operation will be <Tag></Tag>
		AssertInsertion("<Tag$", ">", "<Tag>");
	}

	[TestMethod]
	public void DoNotConvertTagsWithNestedTag()
	{
		AssertInsertion("<Tag$><Foo/></Tag>", "/", "<Tag/><Foo/></Tag>");
	}

	[TestMethod]
	public void DoNotInsertEndingTwice()
	{
		AssertInsertion("<Tag$ >", "/", "<Tag/ >");
	}

	#endregion
}