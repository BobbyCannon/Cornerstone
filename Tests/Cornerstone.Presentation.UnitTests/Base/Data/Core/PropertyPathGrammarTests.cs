#region References

using System.Linq;
using Cornerstone.Presentation.Markup.Parsers;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core;

[TestClass]
public class PropertyPathGrammarTests
{
	#region Methods

	[PresentationTestMethod]
	public void PropertyPathShouldIgnoreTrailingWhitespace()
	{
		Check("  SomeProperty   ", new PropertyPathGrammar.PropertySyntax { Name = "SomeProperty" });
	}

	[PresentationTestMethod]
	public void PropertyPathShouldSupportCasts()
	{
		Check(" ( somens:SomeType.SomeProperty ) :> SomeType.Child as somens:SomeType . SubChild ",
			new PropertyPathGrammar.TypeQualifiedPropertySyntax
			{
				Name = "SomeProperty", TypeName = "SomeType", TypeNamespace = "somens"
			},
			new PropertyPathGrammar.CastTypeSyntax
			{
				TypeName = "SomeType"
			},
			PropertyPathGrammar.ChildTraversalSyntax.Instance,
			new PropertyPathGrammar.PropertySyntax { Name = "Child" },
			new PropertyPathGrammar.CastTypeSyntax
			{
				TypeName = "SomeType",
				TypeNamespace = "somens"
			},
			PropertyPathGrammar.ChildTraversalSyntax.Instance,
			new PropertyPathGrammar.PropertySyntax { Name = "SubChild" }
		);
	}

	[PresentationTestMethod]
	public void PropertyPathShouldSupportEnsureType()
	{
		Check(" ( somens:SomeType.SomeProperty ) := SomeType.Child := somens:SomeType . SubChild ",
			new PropertyPathGrammar.TypeQualifiedPropertySyntax
			{
				Name = "SomeProperty", TypeName = "SomeType", TypeNamespace = "somens"
			},
			new PropertyPathGrammar.EnsureTypeSyntax
			{
				TypeName = "SomeType"
			},
			PropertyPathGrammar.ChildTraversalSyntax.Instance,
			new PropertyPathGrammar.PropertySyntax { Name = "Child" },
			new PropertyPathGrammar.EnsureTypeSyntax
			{
				TypeName = "SomeType",
				TypeNamespace = "somens"
			},
			PropertyPathGrammar.ChildTraversalSyntax.Instance,
			new PropertyPathGrammar.PropertySyntax { Name = "SubChild" }
		);
	}

	[PresentationTestMethod]
	public void PropertyPathShouldSupportPropertyPaths()
	{
		Check(" ( somens:SomeType.SomeProperty ).Child . SubChild ",
			new PropertyPathGrammar.TypeQualifiedPropertySyntax
			{
				Name = "SomeProperty", TypeName = "SomeType", TypeNamespace = "somens"
			},
			PropertyPathGrammar.ChildTraversalSyntax.Instance,
			new PropertyPathGrammar.PropertySyntax { Name = "Child" },
			PropertyPathGrammar.ChildTraversalSyntax.Instance,
			new PropertyPathGrammar.PropertySyntax { Name = "SubChild" }
		);
	}

	[PresentationTestMethod]
	public void PropertyPathShouldSupportQualifiedProperties()
	{
		Check(" ( somens:SomeType.SomeProperty ) ",
			new PropertyPathGrammar.TypeQualifiedPropertySyntax
			{
				Name = "SomeProperty", TypeName = "SomeType", TypeNamespace = "somens"
			});
	}

	[PresentationTestMethod]
	public void PropertyPathShouldSupportSimpleProperties()
	{
		Check("SomeProperty", new PropertyPathGrammar.PropertySyntax { Name = "SomeProperty" });
	}

	private static void Check(string s, params PropertyPathGrammar.ISyntax[] expected)
	{
		var parsed = PropertyPathGrammar.Parse(s).ToList();
		CornerstoneTest.AreEqual(expected.Length, parsed.Count);
		for (var c = 0; c < parsed.Count; c++)
		{
			CornerstoneTest.AreEqual(expected[c], parsed[c]);
		}
	}

	#endregion
}