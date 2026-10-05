#region References

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Data.Core.Parsers;
using Cornerstone.Presentation.Diagnostics;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Parsers;

[TestClass]
public class ExpressionObserverBuilderTestsPresentationProperty : ScopedTestBase
{
	#region Constructors

	public ExpressionObserverBuilderTestsPresentationProperty()
	{
		var foo = Class1.FooProperty;
	}

	#endregion

	#region Methods

	[PresentationTestMethod]
	public async Task ShouldGetPresentationPropertyByName()
	{
		var data = new Class1();
		var target = Build(data, "Foo").ToObservable();
		var result = await target.Take(1);

		CornerstoneTest.AreEqual("foo", result);

		CornerstoneTest.IsNull(((IPresentationObjectDebug) data).GetPropertyChangedSubscribers());
	}

	[PresentationTestMethod]
	public void ShouldTrackPresentationPropertyByName()
	{
		var data = new Class1();
		var target = Build(data, "Foo");
		var result = new List<object>();

		var sub = target.ToObservable().Subscribe(x => result.Add(x));
		data.SetValue(Class1.FooProperty, "bar");

		CornerstoneTest.AreEqual(new[] { "foo", "bar" }, result);

		sub.Dispose();

		CornerstoneTest.IsNull(((IPresentationObjectDebug) data).GetPropertyChangedSubscribers());
	}

	private static BindingExpression Build(object source, string path)
	{
		var r = new CharacterReader(path);
		var grammar = BindingExpressionGrammar.Parse(ref r).Nodes;
		var nodes = ExpressionNodeFactory.CreateFromAst(grammar, null, null, out _);
		return new BindingExpression(source, nodes, PresentationProperty.UnsetValue);
	}

	#endregion

	#region Classes

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly StyledProperty<string> FooProperty =
			PresentationProperty.Register<Class1, string>("Foo", "foo");

		#endregion

		#region Properties

		public string ClrProperty { get; } = "clr-property";

		#endregion
	}

	#endregion
}