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
public class ExpressionObserverBuilderTestsAttachedProperty : ScopedTestBase
{
	#region Fields

	private readonly Func<string, string, Type> _typeResolver;

	#endregion

	#region Constructors

	public ExpressionObserverBuilderTestsAttachedProperty()
	{
		var foo = Owner.FooProperty;
		_typeResolver = (_, name) => name == "Owner" ? typeof(Owner) : null;
	}

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void ShouldFailWithAttachedPropertyWithMoreThan2Parts()
	{
		var data = new Class1();

		Assert.Throws<ExpressionParseException>(() => Build(data, "(Owner.Foo.Bar)", _typeResolver));
	}

	[PresentationTestMethod]
	public void ShouldFailWithAttachedPropertyWithOnly1Part()
	{
		var data = new Class1();

		Assert.Throws<ExpressionParseException>(() => Build(data, "(Owner.)", _typeResolver));
	}

	[PresentationTestMethod]
	public async Task ShouldGetAttachedPropertyValue()
	{
		var data = new Class1();
		var target = Build(data, "(Owner.Foo)", _typeResolver);
		var result = await target.Take(1);

		CornerstoneTest.AreEqual("foo", result);

		CornerstoneTest.IsNull(((IPresentationObjectDebug) data).GetPropertyChangedSubscribers());
	}

	[PresentationTestMethod]
	public async Task ShouldGetAttachedPropertyValueWithNamespace()
	{
		var data = new Class1();
		var target = Build(
			data,
			"(NS:Owner.Foo)",
			(ns, name) => (ns == "NS") && (name == "Owner") ? typeof(Owner) : null);
		var result = await target.Take(1);
		CornerstoneTest.AreEqual("foo", result);
		CornerstoneTest.IsNull(((IPresentationObjectDebug) data).GetPropertyChangedSubscribers());
	}

	[PresentationTestMethod]
	public async Task ShouldGetChainedAttachedPropertyValue()
	{
		var data = new Class1
		{
			Next = new Class1
			{
				[Owner.FooProperty] = "bar"
			}
		};

		var target = Build(data, "Next.(Owner.Foo)", _typeResolver);
		var result = await target.Take(1);

		CornerstoneTest.AreEqual("bar", result);

		CornerstoneTest.IsNull(((IPresentationObjectDebug) data).GetPropertyChangedSubscribers());
	}

	[PresentationTestMethod]
	public async Task ShouldGetChainedAttachedPropertyValueWithTypeCast()
	{
		var expected = new Class1();

		var data = new Class1();
		data.SetValue(Owner.SomethingProperty, new Class1 { Next = expected });

		var target = Build(data, "((Class1)(Owner.Something)).Next", (ns, name) => name == "Class1" ? typeof(Class1) : _typeResolver(ns, name));
		var result = await target.Take(1);

		CornerstoneTest.AreEqual(expected, result);

		CornerstoneTest.IsNull(((IPresentationObjectDebug) data).GetPropertyChangedSubscribers());
	}

	[PresentationTestMethod]
	public void ShouldNotKeepSourceAlive()
	{
		var run = () =>
		{
			var source = new Class1();
			var target = Build(source, "(Owner.Foo)", _typeResolver);
			return (target, source: new WeakReference(source));
		};

		var result = run();
		result.target.Subscribe(x => { });

		// Mono trickery
		GC.Collect(2);
		GC.WaitForPendingFinalizers();
		GC.WaitForPendingFinalizers();
		GC.Collect(2);

		CornerstoneTest.IsNull(result.source.Target);
	}

	[PresentationTestMethod]
	public void ShouldTrackChainedAttachedValue()
	{
		var data = new Class1
		{
			Next = new Class1
			{
				[Owner.FooProperty] = "foo"
			}
		};

		var target = Build(data, "Next.(Owner.Foo)", _typeResolver);
		var result = new List<object>();

		var sub = target.Subscribe(x => result.Add(x));
		data.Next.SetValue(Owner.FooProperty, "bar");

		CornerstoneTest.AreEqual(new[] { "foo", "bar" }, result);

		sub.Dispose();

		CornerstoneTest.IsNull(((IPresentationObjectDebug) data).GetPropertyChangedSubscribers());
	}

	[PresentationTestMethod]
	public void ShouldTrackSimpleAttachedValue()
	{
		var data = new Class1();
		var target = Build(data, "(Owner.Foo)", _typeResolver);
		var result = new List<object>();

		var sub = target.Subscribe(x => result.Add(x));
		data.SetValue(Owner.FooProperty, "bar");

		CornerstoneTest.AreEqual(new[] { "foo", "bar" }, result);

		sub.Dispose();

		CornerstoneTest.IsNull(((IPresentationObjectDebug) data).GetPropertyChangedSubscribers());
	}

	private static IObservable<object> Build(object source, string path, Func<string, string, Type> typeResolver)
	{
		var r = new CharacterReader(path);
		var grammar = BindingExpressionGrammar.Parse(ref r).Nodes;
		var nodes = ExpressionNodeFactory.CreateFromAst(grammar, typeResolver, null, out _);
		return new BindingExpression(source, nodes, PresentationProperty.UnsetValue).ToObservable();
	}

	#endregion

	#region Classes

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly StyledProperty<Class1> NextProperty =
			PresentationProperty.Register<Class1, Class1>(nameof(Next));

		#endregion

		#region Properties

		public Class1 Next
		{
			get => GetValue(NextProperty);
			set => SetValue(NextProperty, value);
		}

		#endregion
	}

	private static class Owner
	{
		#region Fields

		public static readonly AttachedProperty<string> FooProperty =
			PresentationProperty.RegisterAttached<Class1, string>(
				"Foo",
				typeof(Owner),
				"foo");

		public static readonly AttachedProperty<PresentationObject> SomethingProperty =
			PresentationProperty.RegisterAttached<Class1, PresentationObject>(
				"Something",
				typeof(Owner));

		#endregion
	}

	#endregion
}