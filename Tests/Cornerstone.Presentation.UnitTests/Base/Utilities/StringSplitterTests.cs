#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Utilities;

[TestClass]
public class StringSplitterTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow('(', '(')]
	[DataRow('[', '[')]
	[DataRow('.', '.')]
	public void SplitRespectingBracketsSameOpeningAndClosingBracketThrowsArgumentException(char bracket1, char bracket2)
	{
		var input = "a,b,c";

		Assert.Throws<ArgumentException>(() =>
			StringSplitter.SplitRespectingBrackets(input, ',', bracket1, bracket2));
	}

	[PresentationTestMethod]
	[DataRow("(")]
	[DataRow(")")]
	[DataRow(")a,b(")]
	[DataRow("a,b),c")]
	[DataRow("a,(b,c")]
	[DataRow("a,b))c")]
	[DataRow("a,((b,c)")]
	[DataRow("a,(b,(c)),d)")]
	[DataRow("x,[y,z", '[', ']')]
	[DataRow("x,y],z", '[', ']')]
	[DataRow("Type1,Type2(Inner1,Inner2)),Type3")]
	[DataRow("Property1,Property2(Parameter1,Parameter2,Property3")]
	[DataRow("OuterType(InnerType(DeepType(Value1,Value2),MiddleType(Value3)")]
	public void SplitRespectingBracketsUnmatchedBracketsThrowsFormatException(string input, char openingBracket = '(', char closingBracket = ')')
	{
		Assert.Throws<FormatException>(() =>
			StringSplitter.SplitRespectingBrackets(input, ',', openingBracket, closingBracket));
	}

	[PresentationTestMethod]
	[DataRow("a,(b,c),d", '[', ']', new[] { "a", "(b", "c)", "d" })]
	[DataRow("a,[b,c],d", '[', ']', new[] { "a", "[b,c]", "d" })]
	[DataRow("x,<y,z>,w", '<', '>', new[] { "x", "<y,z>", "w" })]
	[DataRow("Property1,Property2[Index1,Index2],Property3", '[', ']', new[] { "Property1", "Property2[Index1,Index2]", "Property3" })]
	public void SplitRespectingBracketsWithBracketsCustomBrackets(string input, char openingBracket, char closingBracket, string[] expected)
	{
		var result = StringSplitter.SplitRespectingBrackets(input, ',', openingBracket, closingBracket);
		CornerstoneTest.AreEqual(expected, result);
	}

	[PresentationTestMethod]
	[DataRow("(a)(b,c)", new[] { "(a)(b,c)" })]
	[DataRow("a,(),b", new[] { "a", "()", "b" })]
	[DataRow("a,(b,c),d", new[] { "a", "(b,c)", "d" })]
	[DataRow("a,(b,(c,d)),e", new[] { "a", "(b,(c,d))", "e" })]
	[DataRow(",a,(b,c),d,", new[] { "", "a", "(b,c)", "d", "" })]
	[DataRow("(a,b),(c,d),(e,f)", new[] { "(a,b)", "(c,d)", "(e,f)" })]
	[DataRow("a,(b,(c,(d,e))),f", new[] { "a", "(b,(c,(d,e)))", "f" })]
	[DataRow("Button,TextBox(Width=100,Height=50),Label", new[] { "Button", "TextBox(Width=100,Height=50)", "Label" })]
	[DataRow("string,List(int),Dictionary(string,object)", new[] { "string", "List(int)", "Dictionary(string,object)" })]
	[DataRow("FirstItem,Item(param1,param2,param3),x,VeryLongItemName(a,b),Short", new[] { "FirstItem", "Item(param1,param2,param3)", "x", "VeryLongItemName(a,b)", "Short" })]
	[DataRow("BindingPath,Converter(Type=MyConverter,Parameter=Value123),Mode=TwoWay", new[] { "BindingPath", "Converter(Type=MyConverter,Parameter=Value123)", "Mode=TwoWay" })]
	[DataRow("Observable(List(Dictionary(string,int))),SimpleType,AnotherObservable(string)", new[] { "Observable(List(Dictionary(string,int)))", "SimpleType", "AnotherObservable(string)" })]
	[DataRow("OuterType(InnerType(DeepType(VeryDeepValue1,VeryDeepValue2),InnerValue),OuterValue)", new[] { "OuterType(InnerType(DeepType(VeryDeepValue1,VeryDeepValue2),InnerValue),OuterValue)" })]
	[DataRow("0 4 6 -1 #FF000000,0 2 4 -1 rgba(0,0,0,0.06),inset 0 1 2 0 rgba(255,255,255,0.1)", new[] { "0 4 6 -1 #FF000000", "0 2 4 -1 rgba(0,0,0,0.06)", "inset 0 1 2 0 rgba(255,255,255,0.1)" })]
	public void SplitRespectingBracketsWithBracketsDefaultBrackets(string input, string[] expected)
	{
		var result = StringSplitter.SplitRespectingBrackets(input, ',');
		CornerstoneTest.AreEqual(expected, result);
	}

	[PresentationTestMethod]
	[DataRow("a,(b,c;d);e", new[] { "a", "(b,c;d)", "e" })]
	[DataRow("Width=100,Height=200;Margin(10;20;30;40),Padding=5", new[] { "Width=100", "Height=200", "Margin(10;20;30;40)", "Padding=5" })]
	public void SplitRespectingBracketsWithBracketsMultipleSeparators(string input, string[] expected)
	{
		var result = StringSplitter.SplitRespectingBrackets(input, [',', ';']);
		CornerstoneTest.AreEqual(expected, result);
	}

	[PresentationTestMethod]
	[DataRow("a,,(b,c),,d", StringSplitOptions.None, new[] { "a", "", "(b,c)", "", "d" })]
	[DataRow("a,,(b,c),,d", StringSplitOptions.RemoveEmptyEntries, new[] { "a", "(b,c)", "d" })]
	[DataRow(",a,(b,c),d,", StringSplitOptions.None, new[] { "", "a", "(b,c)", "d", "" })]
	[DataRow(",a,(b,c),d,", StringSplitOptions.RemoveEmptyEntries, new[] { "a", "(b,c)", "d" })]
	[DataRow(" a , (b, c) , d ", StringSplitOptions.None, new[] { " a ", " (b, c) ", " d " })]
	[DataRow(" a , (b, c) , d ", StringSplitOptions.TrimEntries, new[] { "a", "(b, c)", "d" })]
	[DataRow(" a ,  , (b, c) ,  , d ", StringSplitOptions.None, new[] { " a ", "  ", " (b, c) ", "  ", " d " })]
	[DataRow(" a ,  , (b, c) ,  , d ", StringSplitOptions.TrimEntries, new[] { "a", "", "(b, c)", "", "d" })]
	[DataRow(" a ,  , (b, c) ,  , d ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new[] { "a", "(b, c)", "d" })]
	[DataRow(" , a , ( b , ( c , d ) ) , , e , ", StringSplitOptions.None, new[] { " ", " a ", " ( b , ( c , d ) ) ", " ", " e ", " " })]
	[DataRow(" , a , ( b , ( c , d ) ) , , e , ", StringSplitOptions.TrimEntries, new[] { "", "a", "( b , ( c , d ) )", "", "e", "" })]
	[DataRow(" , a , ( b , ( c , d ) ) , , e , ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new[] { "a", "( b , ( c , d ) )", "e" })]
	public void SplitRespectingBracketsWithBracketsWithOptions(string input, StringSplitOptions options, string[] expected)
	{
		var result = StringSplitter.SplitRespectingBrackets(input, ',', options: options);
		CornerstoneTest.AreEqual(expected, result);
	}

	[PresentationTestMethod]
	[DataRow("a,b;c,d")]
	[DataRow("a,b;,;c,d")]
	[DataRow(" a , b ; c , d ")]
	[DataRow(" a , b ; ; c , d ")]
	[DataRow(" a , b ;,; c , d ")]
	[DataRow(" ; a , b ; c , d ; ")]
	public void SplitRespectingBracketsWithoutBracketsMultipleSeparators(string input)
	{
		char[] separators = [',', ';'];
		foreach (var options in EnumerateStringSplitOptionsCombinations())
		{
			var result = StringSplitter.SplitRespectingBrackets(input, separators, options: options);
			var expected = input.Split(separators, options);
			CornerstoneTest.AreEqual(expected, result);
		}
	}

	[PresentationTestMethod]
	public void SplitRespectingBracketsWithoutBracketsNullReturnsEmptyArray()
	{
		var result = StringSplitter.SplitRespectingBrackets(null, ',');
		CornerstoneTest.Empty(result);
	}

	[PresentationTestMethod]
	[DataRow("")]
	[DataRow("   ")]
	[DataRow("\t\n")]
	[DataRow("abc")]
	[DataRow("a,b,c")]
	[DataRow("a,,c")]
	[DataRow("a,,,b")]
	[DataRow(",a,b,")]
	[DataRow(" a , b , c ")]
	[DataRow(" a ,,,, c ")]
	[DataRow(" a ,  , c ")]
	[DataRow(" a, b ,c ")]
	[DataRow(" , a , b , ")]
	[DataRow("  a  ,  b  ,  c  ")]
	[DataRow("First,Second,Third")]
	[DataRow("Header\nBody\nFooter\n", '\n')]
	[DataRow("Width;Height;Margin;Padding", ';')]
	[DataRow("Cornerstone.Presentation.Utilities.StringSplitter", '.')]
	public void SplitRespectingBracketsWithoutBracketsSingleSeparator(string input, char separator = ',')
	{
		foreach (var options in EnumerateStringSplitOptionsCombinations())
		{
			var result = StringSplitter.SplitRespectingBrackets(input, separator, options: options);
			var expected = input.Split(separator, options);
			CornerstoneTest.AreEqual(expected, result);
		}
	}

	private static IEnumerable<StringSplitOptions> EnumerateStringSplitOptionsCombinations()
	{
		yield return StringSplitOptions.None;
		yield return StringSplitOptions.RemoveEmptyEntries;
		yield return StringSplitOptions.TrimEntries;
		yield return StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries;
	}

	#endregion
}