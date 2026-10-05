#region References

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Data.Core.Parsers;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Parsers;

[TestClass]
public class ExpressionObserverBuilderTestsProperty : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldHaveNullSourceTypeForBrokenChain()
	{
		var data = new { Foo = new { Bar = 1 } };
		var target = Build(data, "Foo.Bar.Baz");

		CornerstoneTest.IsNull(target.SourceType);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ShouldReturnBindingNotificationErrorForBrokenChain()
	{
		var data = new { Foo = new { Bar = 1 } };
		var target = Build(data, "Foo.Bar.Baz").ToObservable();
		var result = await target.Take(1);

		CornerstoneTest.IsType<BindingNotification>(result);

		CornerstoneTest.AreEqual(new BindingNotification(
			new BindingChainException("Could not find a matching property accessor for 'Baz' on 'System.Int32'.", "Foo.Bar.Baz", "Baz"),
			BindingErrorType.Error), result);

		GC.KeepAlive(data);
	}

	private static BindingExpression Build(object source, string path)
	{
		var r = new CharacterReader(path);
		var grammar = BindingExpressionGrammar.Parse(ref r).Nodes;
		var nodes = ExpressionNodeFactory.CreateFromAst(grammar, null, null, out _);
		return new BindingExpression(source, nodes, PresentationProperty.UnsetValue);
	}

	#endregion
}