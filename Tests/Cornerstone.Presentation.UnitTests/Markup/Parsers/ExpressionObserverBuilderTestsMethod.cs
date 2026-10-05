#region References

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Data.Core.Parsers;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Parsers;

[TestClass]
[SkipInAot("Reflection method accessors are not registered when dynamic code is unsupported.")]
public class ExpressionObserverBuilderTestsMethod : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public async Task CanCallMethodReturnedFromObserver()
	{
		var data = new TestObject();
		var observer = Build(data, nameof(TestObject.MethodWithReturnAndParameter));
		var result = await observer.Take(1);

		var callback = (Func<object, int>) result!;

		CornerstoneTest.AreEqual(1, callback(1));

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ShouldGetMethod()
	{
		var data = new TestObject();
		var observer = Build(data, nameof(TestObject.MethodWithoutReturn));
		var result = await observer.Take(1);

		CornerstoneTest.IsNotNull(result);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	[DataRow(nameof(TestObject.MethodWithoutReturn), typeof(Action))]
	[DataRow(nameof(TestObject.MethodWithReturn), typeof(Func<int>))]
	[DataRow(nameof(TestObject.MethodWithReturnAndParameter), typeof(Func<object, int>))]
	[DataRow(nameof(TestObject.StaticMethod), typeof(Action))]
	public async Task ShouldGetMethodWithCorrectDelegateType(string methodName, Type expectedType)
	{
		var data = new TestObject();
		var observer = Build(data, methodName);
		var result = await observer.Take(1);

		CornerstoneTest.IsType(expectedType, result);

		GC.KeepAlive(data);
	}

	private static IObservable<object> Build(object source, string path)
	{
		var r = new CharacterReader(path);
		var grammar = BindingExpressionGrammar.Parse(ref r).Nodes;
		var nodes = ExpressionNodeFactory.CreateFromAst(grammar, null, null, out _);
		return new BindingExpression(source, nodes, PresentationProperty.UnsetValue).ToObservable();
	}

	#endregion

	#region Classes

	private class TestObject
	{
		#region Methods

		public int MethodWithReturn()
		{
			return 0;
		}

		public int MethodWithReturnAndParameter(object i)
		{
			return (int) i;
		}

		public void MethodWithoutReturn()
		{
		}

		public static void StaticMethod()
		{
		}

		#endregion
	}

	#endregion
}