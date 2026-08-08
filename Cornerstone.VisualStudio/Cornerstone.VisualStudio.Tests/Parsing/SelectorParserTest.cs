#region References

using Cornerstone.VisualStudio.Core.Parsing;
using Xunit;

#endregion

namespace Cornerstone.VisualStudio.Tests.Parsing;

public class SelectorParserTest
{
	#region Methods

	[Fact]
	public void ParseColonAfterPropertySelector()
	{
		var parser = SelectorParser.Parse("Button[IsDefault=True]:");

		Assert.Equal(SelectorStatement.Middle, parser.PreviousStatement);
		Assert.Equal("IsDefault", parser.PropertyName);
		Assert.Equal(SelectorStatement.Colon, parser.Statement);
		Assert.Equal("", parser.Class);
	}

	[Fact]
	public void ParseIsSelector()
	{
		var parser = SelectorParser.Parse(":is(B");

		Assert.Equal(SelectorStatement.FunctionArgs, parser.PreviousStatement);
		Assert.Equal("is", parser.FunctionName);
		Assert.Equal(SelectorStatement.TypeName, parser.Statement);
		Assert.Equal("B", parser.TypeName);
	}

	[Fact]
	public void ParseNotInfiniteLoop()
	{
		var parser = SelectorParser.Parse("Button:not(:disabled)");

		Assert.Equal(SelectorStatement.FunctionArgs, parser.PreviousStatement);
		Assert.Equal("not", parser.FunctionName);
		Assert.Equal(SelectorStatement.Middle, parser.Statement);
		Assert.Equal("disabled", parser.Class);
	}

	[Fact]
	public void ParseNotSelector()
	{
		var parser = SelectorParser.Parse(":not(B");

		Assert.Equal(SelectorStatement.CanHaveType, parser.PreviousStatement);
		Assert.Equal("not", parser.FunctionName);
		Assert.Equal(SelectorStatement.FunctionArgs, parser.Statement);
		Assert.Equal("B", parser.TypeName);
	}

	#endregion
}