#region References

using Cornerstone.VisualStudio.Core.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests.Parsing;

[TestClass]
public class SelectorParserTest
{
	#region Methods

	[TestMethod]
	public void ParseColonAfterPropertySelector()
	{
		var parser = SelectorParser.Parse("Button[IsDefault=True]:");

		Assert.AreEqual(SelectorStatement.Middle, parser.PreviousStatement);
		Assert.AreEqual("IsDefault", parser.PropertyName);
		Assert.AreEqual(SelectorStatement.Colon, parser.Statement);
		Assert.AreEqual("", parser.Class);
	}

	[TestMethod]
	public void ParseIsSelector()
	{
		var parser = SelectorParser.Parse(":is(B");

		Assert.AreEqual(SelectorStatement.FunctionArgs, parser.PreviousStatement);
		Assert.AreEqual("is", parser.FunctionName);
		Assert.AreEqual(SelectorStatement.TypeName, parser.Statement);
		Assert.AreEqual("B", parser.TypeName);
	}

	[TestMethod]
	public void ParseNotInfiniteLoop()
	{
		var parser = SelectorParser.Parse("Button:not(:disabled)");

		Assert.AreEqual(SelectorStatement.FunctionArgs, parser.PreviousStatement);
		Assert.AreEqual("not", parser.FunctionName);
		Assert.AreEqual(SelectorStatement.Middle, parser.Statement);
		Assert.AreEqual("disabled", parser.Class);
	}

	[TestMethod]
	public void ParseNotSelector()
	{
		var parser = SelectorParser.Parse(":not(B");

		Assert.AreEqual(SelectorStatement.CanHaveType, parser.PreviousStatement);
		Assert.AreEqual("not", parser.FunctionName);
		Assert.AreEqual(SelectorStatement.FunctionArgs, parser.Statement);
		Assert.AreEqual("B", parser.TypeName);
	}

	#endregion
}