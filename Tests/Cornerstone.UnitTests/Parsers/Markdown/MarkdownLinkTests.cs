#region References

using Cornerstone.Parsers.Markdown;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.Markdown;

[TestClass]
public class MarkdownLinkTests
{
	#region Methods

	[TestMethod]
	public void ToHeadingIdBasic()
	{
		Assert.AreEqual("keystone", MarkdownLink.ToHeadingId("Keystone"));
		Assert.AreEqual("bus-state-engine", MarkdownLink.ToHeadingId("Bus : State : Engine"));
		Assert.AreEqual("what-it-is", MarkdownLink.ToHeadingId("What it is"));
		Assert.AreEqual("architecture-application-shell", MarkdownLink.ToHeadingId("Architecture & application shell"));
	}

	[TestMethod]
	public void ToHeadingIdEmpty()
	{
		Assert.AreEqual(string.Empty, MarkdownLink.ToHeadingId(""));
		Assert.AreEqual(string.Empty, MarkdownLink.ToHeadingId("   "));
	}

	#endregion
}
