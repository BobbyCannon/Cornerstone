#region References

using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core.Parsers;

public partial class BindingExpressionGrammarTests
{
	#region Methods

	[PresentationTestMethod]
	public void ExpressionCannotEndWithNullConditional()
	{
		Assert.Throws<ExpressionParseException>(() => Parse("Foo.Bar?."));
	}

	[PresentationTestMethod]
	public void ExpressionCannotEndWithPeriod()
	{
		Assert.Throws<ExpressionParseException>(() => Parse("Foo.Bar."));
	}

	[PresentationTestMethod]
	public void ExpressionCannotEndWithQuestionMark()
	{
		Assert.Throws<ExpressionParseException>(() => Parse("Foo.Bar?"));
	}

	[PresentationTestMethod]
	public void ExpressionCannotHaveDigitAfterIndexer()
	{
		Assert.Throws<ExpressionParseException>(() => Parse("Foo.Bar[3,4]5"));
	}

	[PresentationTestMethod]
	public void ExpressionCannotHaveEmptyIndexer()
	{
		Assert.Throws<ExpressionParseException>(() => Parse("Foo.Bar[]"));
	}

	[PresentationTestMethod]
	public void ExpressionCannotHaveExtraCommaAtEndOfIndexer()
	{
		Assert.Throws<ExpressionParseException>(() => Parse("Foo.Bar[3,4,]"));
	}

	[PresentationTestMethod]
	public void ExpressionCannotHaveExtraCommaAtStartOfIndexer()
	{
		Assert.Throws<ExpressionParseException>(() => Parse("Foo.Bar[,3,4]"));
	}

	[PresentationTestMethod]
	public void ExpressionCannotHaveExtraCommaInIndexer()
	{
		Assert.Throws<ExpressionParseException>(() => Parse("Foo.Bar[3,,4]"));
	}

	[PresentationTestMethod]
	public void ExpressionCannotHaveLetterAfterIndexer()
	{
		Assert.Throws<ExpressionParseException>(() => Parse("Foo.Bar[3,4]A"));
	}

	[PresentationTestMethod]
	public void ExpressionCannotStartWithPeriodThenToken()
	{
		Assert.Throws<ExpressionParseException>(() => Parse(".Bar"));
	}

	[PresentationTestMethod]
	public void IdentifierCannotStartWithDigit()
	{
		Assert.Throws<ExpressionParseException>(() => Parse("1Foo"));
	}

	[PresentationTestMethod]
	public void IdentifierCannotStartWithNullConditional()
	{
		Assert.Throws<ExpressionParseException>(() => Parse("?.Foo"));
	}

	[PresentationTestMethod]
	public void IdentifierCannotStartWithQuestionMark()
	{
		Assert.Throws<ExpressionParseException>(() => Parse("?Foo"));
	}

	[PresentationTestMethod]
	public void IdentifierCannotStartWithSymbol()
	{
		Assert.Throws<ExpressionParseException>(() => Parse("Foo.%Bar"));
	}

	#endregion
}