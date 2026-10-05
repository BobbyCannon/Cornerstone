#region References

using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Markup.Parsers;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Parsers;

[TestClass]
public class SelectorGrammarTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void Class()
	{
		var result = SelectorGrammar.Parse(".foo");

		CornerstoneTest.AreEqual(new[] { new SelectorGrammar.ClassSyntax { Class = "foo" } }, result);
	}

	[PresentationTestMethod]
	public void DotAloneFails()
	{
		Assert.Throws<ExpressionParseException>(() => SelectorGrammar.Parse(". dot"));
	}

	[PresentationTestMethod]
	public void InvalidClassFails()
	{
		Assert.Throws<ExpressionParseException>(() => SelectorGrammar.Parse(".%foo"));
	}

	[PresentationTestMethod]
	public void InvalidIdentifierFails()
	{
		Assert.Throws<ExpressionParseException>(() => SelectorGrammar.Parse("%foo"));
	}

	[PresentationTestMethod]
	public void Is()
	{
		var result = SelectorGrammar.Parse(":is(Button)");

		CornerstoneTest.AreEqual(new[] { new SelectorGrammar.IsSyntax { TypeName = "Button", Xmlns = "" } }, result);
	}

	[PresentationTestMethod]
	public void IsDescendentNotOfTypeClass()
	{
		var result = SelectorGrammar.Parse(":is(Control) :not(Button.foo)");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.IsSyntax { TypeName = "Control" },
			new SelectorGrammar.DescendantSyntax(),
			new SelectorGrammar.NotSyntax
			{
				Argument = new SelectorGrammar.ISyntax[]
				{
					new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
					new SelectorGrammar.ClassSyntax { Class = "foo" }
				}
			}
		}, result);
	}

	[PresentationTestMethod]
	public void IsName()
	{
		var result = SelectorGrammar.Parse(":is(Button)#foo");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.IsSyntax { TypeName = "Button" },
			new SelectorGrammar.NameSyntax { Name = "foo" }
		}, result);
	}

	[PresentationTestMethod]
	public void Name()
	{
		var result = SelectorGrammar.Parse("#foo");

		CornerstoneTest.AreEqual(new[] { new SelectorGrammar.NameSyntax { Name = "foo" } }, result);
	}

	[PresentationTestMethod]
	public void NamespaceAloneFails()
	{
		Assert.Throws<ExpressionParseException>(() => SelectorGrammar.Parse("ns|"));
	}

	[PresentationTestMethod]
	public void NamespacedIsName()
	{
		var result = SelectorGrammar.Parse(":is(x|Button)#foo");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.IsSyntax { TypeName = "Button", Xmlns = "x" },
			new SelectorGrammar.NameSyntax { Name = "foo" }
		}, result);
	}

	[PresentationTestMethod]
	public void NamespacedOfType()
	{
		var result = SelectorGrammar.Parse("x|Button");

		CornerstoneTest.AreEqual(new[] { new SelectorGrammar.OfTypeSyntax { TypeName = "Button", Xmlns = "x" } }, result);
	}

	[PresentationTestMethod]
	public void NestingChildClass()
	{
		var result = SelectorGrammar.Parse("^ > .foo");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.NestingSyntax(),
			new SelectorGrammar.ChildSyntax(),
			new SelectorGrammar.ClassSyntax { Class = "foo" }
		}, result);
	}

	[PresentationTestMethod]
	public void NestingClass()
	{
		var result = SelectorGrammar.Parse("^.foo");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.NestingSyntax(),
			new SelectorGrammar.ClassSyntax { Class = "foo" }
		}, result);
	}

	[PresentationTestMethod]
	public void NestingCommaNestingClass()
	{
		var result = SelectorGrammar.Parse("^, ^.foo");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.NestingSyntax(),
			new SelectorGrammar.CommaSyntax(),
			new SelectorGrammar.NestingSyntax(),
			new SelectorGrammar.ClassSyntax { Class = "foo" }
		}, result);
	}

	[PresentationTestMethod]
	public void NestingDescendantClass()
	{
		var result = SelectorGrammar.Parse("^ .foo");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.NestingSyntax(),
			new SelectorGrammar.DescendantSyntax(),
			new SelectorGrammar.ClassSyntax { Class = "foo" }
		}, result);
	}

	[PresentationTestMethod]
	public void NestingNthChild()
	{
		var result = SelectorGrammar.Parse("^:nth-child(2n+1)");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.NestingSyntax(),
			new SelectorGrammar.NthChildSyntax
			{
				Step = 2,
				Offset = 1
			}
		}, result);
	}

	[PresentationTestMethod]
	public void NestingProperty()
	{
		var result = SelectorGrammar.Parse("^[Foo=bar]");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.NestingSyntax(),
			new SelectorGrammar.PropertySyntax { Property = "Foo", Value = "bar" }
		}, result);
	}

	[PresentationTestMethod]
	public void NestingTemplateClass()
	{
		var result = SelectorGrammar.Parse("^ /template/ .foo");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.NestingSyntax(),
			new SelectorGrammar.TemplateSyntax(),
			new SelectorGrammar.ClassSyntax { Class = "foo" }
		}, result);
	}

	[PresentationTestMethod]
	public void NotNesting()
	{
		var result = SelectorGrammar.Parse(":not(^)");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.NotSyntax
			{
				Argument = new[] { new SelectorGrammar.NestingSyntax() }
			}
		}, result);
	}

	[PresentationTestMethod]
	public void NotOfType()
	{
		var result = SelectorGrammar.Parse(":not(Button)");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.NotSyntax
			{
				Argument = new SelectorGrammar.ISyntax[]
				{
					new SelectorGrammar.OfTypeSyntax { TypeName = "Button" }
				}
			}
		}, result);
	}

	[PresentationTestMethod]
	public void NotWithoutArgumentFails()
	{
		Assert.Throws<ExpressionParseException>(() => SelectorGrammar.Parse(":not()"));
	}

	[PresentationTestMethod]
	public void NotWithoutClosingParenthesisFails()
	{
		Assert.Throws<ExpressionParseException>(() => SelectorGrammar.Parse(":not(Button"));
	}

	[PresentationTestMethod]
	[DataRow(":nth-child(xn+2)")]
	[DataRow(":nth-child(2n+b)")]
	[DataRow(":nth-child(2n+)")]
	[DataRow(":nth-child(2na)")]
	[DataRow(":nth-child(2x+1)")]
	public void NthChildInvalidInputs(string input)
	{
		Assert.Throws<ExpressionParseException>(() => SelectorGrammar.Parse(input));
	}

	[PresentationTestMethod]
	[DataRow(":nth-child(+1)", 0, 1)]
	[DataRow(":nth-child(1)", 0, 1)]
	[DataRow(":nth-child(-1)", 0, -1)]
	[DataRow(":nth-child(2n+1)", 2, 1)]
	[DataRow(":nth-child(n)", 1, 0)]
	[DataRow(":nth-child(+n)", 1, 0)]
	[DataRow(":nth-child(-n)", -1, 0)]
	[DataRow(":nth-child(-2n)", -2, 0)]
	[DataRow(":nth-child(n+5)", 1, 5)]
	[DataRow(":nth-child(n-5)", 1, -5)]
	[DataRow(":nth-child( 2n + 1 )", 2, 1)]
	[DataRow(":nth-child( 2n - 1 )", 2, -1)]
	public void NthChildVariations(string input, int step, int offset)
	{
		var result = SelectorGrammar.Parse(input);

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.NthChildSyntax
			{
				Step = step,
				Offset = offset
			}
		}, result);
	}

	[PresentationTestMethod]
	[DataRow(":nth-last-child(+1)", 0, 1)]
	[DataRow(":nth-last-child(1)", 0, 1)]
	[DataRow(":nth-last-child(-1)", 0, -1)]
	[DataRow(":nth-last-child(2n+1)", 2, 1)]
	[DataRow(":nth-last-child(n)", 1, 0)]
	[DataRow(":nth-last-child(+n)", 1, 0)]
	[DataRow(":nth-last-child(-n)", -1, 0)]
	[DataRow(":nth-last-child(-2n)", -2, 0)]
	[DataRow(":nth-last-child(n+5)", 1, 5)]
	[DataRow(":nth-last-child(n-5)", 1, -5)]
	[DataRow(":nth-last-child( 2n + 1 )", 2, 1)]
	[DataRow(":nth-last-child( 2n - 1 )", 2, -1)]
	public void NthLastChildVariations(string input, int step, int offset)
	{
		var result = SelectorGrammar.Parse(input);

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.NthLastChildSyntax
			{
				Step = step,
				Offset = offset
			}
		}, result);
	}

	[PresentationTestMethod]
	public void OfType()
	{
		var result = SelectorGrammar.Parse("Button");

		CornerstoneTest.AreEqual(new[] { new SelectorGrammar.OfTypeSyntax { TypeName = "Button", Xmlns = "" } }, result);
	}

	[PresentationTestMethod]
	public void OfTypeAttachedProperty()
	{
		var result = SelectorGrammar.Parse("Button[(Grid.Column)=1]");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
			new SelectorGrammar.AttachedPropertySyntax
			{
				Xmlns = string.Empty,
				TypeName = "Grid",
				Property = "Column",
				Value = "1"
			}
		}, result);
	}

	[PresentationTestMethod]
	public void OfTypeAttachedPropertyWithNamespace()
	{
		var result = SelectorGrammar.Parse("Button[(x|Grid.Column)=1]");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
			new SelectorGrammar.AttachedPropertySyntax
			{
				Xmlns = "x",
				TypeName = "Grid",
				Property = "Column",
				Value = "1"
			}
		}, result);
	}

	[PresentationTestMethod]
	public void OfTypeChildClass()
	{
		var result = SelectorGrammar.Parse("Button > .foo");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
			new SelectorGrammar.ChildSyntax(),
			new SelectorGrammar.ClassSyntax { Class = "foo" }
		}, result);
	}

	[PresentationTestMethod]
	public void OfTypeChildClassNoSpaces()
	{
		var result = SelectorGrammar.Parse("Button>.foo");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
			new SelectorGrammar.ChildSyntax(),
			new SelectorGrammar.ClassSyntax { Class = "foo" }
		}, result);
	}

	[PresentationTestMethod]
	public void OfTypeClass()
	{
		var result = SelectorGrammar.Parse("Button.foo");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
			new SelectorGrammar.ClassSyntax { Class = "foo" }
		}, result);
	}

	[PresentationTestMethod]
	public void OfTypeCommaIsClass()
	{
		var result = SelectorGrammar.Parse("TextBlock, :is(Button).foo");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.OfTypeSyntax { TypeName = "TextBlock" },
			new SelectorGrammar.CommaSyntax(),
			new SelectorGrammar.IsSyntax { TypeName = "Button" },
			new SelectorGrammar.ClassSyntax { Class = "foo" }
		}, result);
	}

	[PresentationTestMethod]
	public void OfTypeDescendantClass()
	{
		var result = SelectorGrammar.Parse("Button .foo");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
			new SelectorGrammar.DescendantSyntax(),
			new SelectorGrammar.ClassSyntax { Class = "foo" }
		}, result);
	}

	[PresentationTestMethod]
	public void OfTypeName()
	{
		var result = SelectorGrammar.Parse("Button#foo");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
			new SelectorGrammar.NameSyntax { Name = "foo" }
		}, result);
	}

	[PresentationTestMethod]
	public void OfTypeNotClass()
	{
		var result = SelectorGrammar.Parse("Button:not(.foo)");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
			new SelectorGrammar.NotSyntax
			{
				Argument = new SelectorGrammar.ISyntax[]
				{
					new SelectorGrammar.ClassSyntax { Class = "foo" }
				}
			}
		}, result);
	}

	[PresentationTestMethod]
	public void OfTypeNthChild()
	{
		var result = SelectorGrammar.Parse("Button:nth-child(2n+1)");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
			new SelectorGrammar.NthChildSyntax
			{
				Step = 2,
				Offset = 1
			}
		}, result);
	}

	[PresentationTestMethod]
	public void OfTypeNthChildEven()
	{
		var result = SelectorGrammar.Parse("Button:nth-child(even)");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
			new SelectorGrammar.NthChildSyntax
			{
				Step = 2,
				Offset = 0
			}
		}, result);
	}

	[PresentationTestMethod]
	public void OfTypeNthChildOdd()
	{
		var result = SelectorGrammar.Parse("Button:nth-child(odd)");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
			new SelectorGrammar.NthChildSyntax
			{
				Step = 2,
				Offset = 1
			}
		}, result);
	}

	[PresentationTestMethod]
	public void OfTypeNthChildWithoutOffset()
	{
		var result = SelectorGrammar.Parse("Button:nth-child(2147483647n)");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
			new SelectorGrammar.NthChildSyntax
			{
				Step = int.MaxValue,
				Offset = 0
			}
		}, result);
	}

	[PresentationTestMethod]
	public void OfTypeNthLastChild()
	{
		var result = SelectorGrammar.Parse("Button:nth-last-child(2n+1)");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
			new SelectorGrammar.NthLastChildSyntax
			{
				Step = 2,
				Offset = 1
			}
		}, result);
	}

	[PresentationTestMethod]
	public void OfTypeProperty()
	{
		var result = SelectorGrammar.Parse("Button[Foo=bar]");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
			new SelectorGrammar.PropertySyntax { Property = "Foo", Value = "bar" }
		}, result);
	}

	[PresentationTestMethod]
	public void OfTypeTemplateClass()
	{
		var result = SelectorGrammar.Parse("Button /template/ .foo");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
			new SelectorGrammar.TemplateSyntax(),
			new SelectorGrammar.ClassSyntax { Class = "foo" }
		}, result);
	}

	[PresentationTestMethod]
	public void OfTypeTemplateNesting()
	{
		var result = SelectorGrammar.Parse("Button /template/ ^");

		CornerstoneTest.AreEqual(new SelectorGrammar.ISyntax[]
		{
			new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
			new SelectorGrammar.TemplateSyntax(),
			new SelectorGrammar.NestingSyntax()
		}, result);
	}

	[PresentationTestMethod]
	public void Pseudoclass()
	{
		var result = SelectorGrammar.Parse(":foo");

		CornerstoneTest.AreEqual(new[] { new SelectorGrammar.ClassSyntax { Class = ":foo" } }, result);
	}

	#endregion
}