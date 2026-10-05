#region References

using System;
using Cornerstone.Presentation.Rendering.Composition.Expressions;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Composition;

[TestClass]
public class CompositionAnimationParserTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow("Vector3(0.5+(4.0-0.5)* -2, 1, 0).X", -6.5)]
	public void EvaluatesExpressionCorrectly(string expression, double value)
	{
		var expr = ExpressionParser.Parse(expression);
		var ctx = new ExpressionEvaluationContext
		{
			ForeignFunctionInterface = BuiltInExpressionFfi.Instance
		};
		var res = expr.Evaluate(ref ctx);
		double doubleRes;
		if (res.Type == VariantType.Double)
		{
			doubleRes = res.Double;
		}
		else
		{
			throw new Exception("Invalid result type: " + res.Type);
		}
		CornerstoneTest.AreEqual(value, doubleRes);
	}

	#endregion
}