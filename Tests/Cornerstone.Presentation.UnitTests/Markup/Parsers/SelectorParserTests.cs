#region References

using System;
using System.Runtime.CompilerServices;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Markup.Parsers;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Parsers;

[TestClass]
public class SelectorParserTests : ScopedTestBase
{
	#region Constructors

	static SelectorParserTests()
	{
		//Ensure the attached properties are registered before run tests
		RuntimeHelpers.RunClassConstructor(typeof(Grid).TypeHandle);
		RuntimeHelpers.RunClassConstructor(typeof(Auth).TypeHandle);
	}

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void ParsesAttacchedPropertySelector()
	{
		var target = new SelectorParser((ns, type) =>
		{
			return (ns, type) switch
			{
				("", nameof(TextBlock)) => typeof(TextBlock),
				("", nameof(Grid)) => typeof(Grid),
				_ => null
			};
		});
		var result = target.Parse("TextBlock[(Grid.Column)=1]");
	}

	[PresentationTestMethod]
	public void ParsesAttacchedPropertySelectorWithNamespace()
	{
		var target = new SelectorParser((ns, type) =>
		{
			return (ns, type) switch
			{
				("", nameof(TextBlock)) => typeof(TextBlock),
				("l", nameof(Auth)) => typeof(Auth),
				_ => null
			};
		});
		var result = target.Parse("TextBlock[(l|Auth.Name)=Admin]");
	}

	[PresentationTestMethod]
	public void ParsesBooleanPropertySelector()
	{
		var target = new SelectorParser((ns, type) => typeof(TextBlock));
		var result = target.Parse("TextBlock[IsPointerOver=True]");
	}

	[PresentationTestMethod]
	public void ParsesCommaSeparatedSelectors()
	{
		var target = new SelectorParser((ns, type) => typeof(TextBlock));
		var result = target.Parse("TextBlock, TextBlock:foo");
	}

	[PresentationTestMethod]
	public void ThrowsIfIsTypeNotFound()
	{
		var target = new SelectorParser((ns, type) => null);
		Assert.Throws<InvalidOperationException>(() => target.Parse(":is(NotFound)"));
	}

	[PresentationTestMethod]
	public void ThrowsIfOfTypeTypeNotFound()
	{
		var target = new SelectorParser((ns, type) => null);
		Assert.Throws<InvalidOperationException>(() => target.Parse("NotFound"));
	}

	#endregion

	#region Classes

	private class Auth
	{
		#region Fields

		public static readonly AttachedProperty<string> NameProperty =
			PresentationProperty.RegisterAttached<Auth, PresentationObject, string>("Name");

		#endregion

		#region Methods

		public static string GetName(PresentationObject avaloniaObject)
		{
			return avaloniaObject.GetValue(NameProperty);
		}

		public static void SetName(PresentationObject avaloniaObject, string value)
		{
			avaloniaObject.SetValue(NameProperty, value);
		}

		#endregion
	}

	#endregion
}