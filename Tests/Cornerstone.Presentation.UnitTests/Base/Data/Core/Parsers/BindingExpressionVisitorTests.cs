#region References

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Data.Core.ExpressionNodes;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core.Parsers;

[TestClass]
public class BindingExpressionVisitorTests
{
	#region Methods

	[PresentationTestMethod]
	public void BuildNodesShouldCreateNodeForDowncast()
	{
		// Downcasts (base to derived) create a cast node - the binding system will handle runtime errors
		Expression<Func<TestClass, DerivedTestClass>> expr = x => (DerivedTestClass) x;

		var nodes = BuildNodes(expr);

		var node = CornerstoneTest.Single(nodes);
		CornerstoneTest.IsType<FuncTransformNode>(node);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldCreateNodeForDowncastInPropertyChain()
	{
		// Practical example: casting to access derived type properties
		Expression<Func<TestClass, string>> expr = x => ((DerivedTestClass) x.Child!).DerivedProperty;

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(3, nodes.Count);
		var childNode = CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.AreEqual("Child", childNode.PropertyName);
		CornerstoneTest.IsType<FuncTransformNode>(nodes[1]);
		var derivedNode = CornerstoneTest.IsType<PropertyAccessorNode>(nodes[2]);
		CornerstoneTest.AreEqual("DerivedProperty", derivedNode.PropertyName);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldCreateNodeForTypeAsOperator()
	{
		// TypeAs operator creates a cast node
		Expression<Func<TestClass, object>> expr = x => x.Child as object;

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(2, nodes.Count);
		var propertyNode = CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.AreEqual("Child", propertyNode.PropertyName);
		CornerstoneTest.IsType<FuncTransformNode>(nodes[1]);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldCreateNodeForUpcast()
	{
		// Upcasts (derived to base) create a cast node
		Expression<Func<DerivedTestClass, TestClass>> expr = x => (TestClass) x;

		var nodes = BuildNodes(expr);

		var node = CornerstoneTest.Single(nodes);
		CornerstoneTest.IsType<FuncTransformNode>(node);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldCreateNodeForUpcastInPropertyChain()
	{
		// Cast creates a node, then property access creates another
		Expression<Func<DerivedTestClass, string>> expr = x => ((TestClass) x).StringProperty;

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(2, nodes.Count);
		CornerstoneTest.IsType<FuncTransformNode>(nodes[0]);
		var propertyNode = CornerstoneTest.IsType<PropertyAccessorNode>(nodes[1]);
		CornerstoneTest.AreEqual("StringProperty", propertyNode.PropertyName);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldCreateNodesForCastingThroughObject()
	{
		// Casting through object creates cast nodes
		Expression<Func<TestClass, string>> expr = x => (string) (object) x.StringProperty!;

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(3, nodes.Count);
		var propertyNode = CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.AreEqual("StringProperty", propertyNode.PropertyName);
		CornerstoneTest.IsType<FuncTransformNode>(nodes[1]); // cast to object
		CornerstoneTest.IsType<FuncTransformNode>(nodes[2]); // cast to string
	}

	[PresentationTestMethod]
	public void BuildNodesShouldHandleEmptyExpression()
	{
		Expression<Func<TestClass, TestClass>> expr = x => x;

		var nodes = BuildNodes(expr);

		CornerstoneTest.Empty(nodes);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldHandleUnaryPlusOperator()
	{
		// Unary plus is typically optimized away by the C# compiler and doesn't appear in the
		// expression tree, so it doesn't throw an exception.
		Expression<Func<TestClass, int>> expr = x => +x.IntProperty;

		var nodes = BuildNodes(expr);

		var node = CornerstoneTest.Single(nodes);
		var propertyNode = CornerstoneTest.IsType<PropertyAccessorNode>(node);
		CornerstoneTest.AreEqual("IntProperty", propertyNode.PropertyName);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParseArrayIndex()
	{
		Expression<Func<TestClass, string>> expr = x => x.ArrayProperty![0];

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(2, nodes.Count);

		var propertyNode = CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.AreEqual("ArrayProperty", propertyNode.PropertyName);

		CornerstoneTest.IsType<ArrayIndexerNode>(nodes[1]);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParseChainedIndexers()
	{
		Expression<Func<TestClass, string>> expr = x => x.NestedIndexedProperty![0]![1];

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(3, nodes.Count);

		var propertyNode = CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.AreEqual("NestedIndexedProperty", propertyNode.PropertyName);

		CornerstoneTest.IsType<PropertyAccessorNode>(nodes[1]); // List indexer
		CornerstoneTest.IsType<PropertyAccessorNode>(nodes[2]); // List indexer
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParseIndexer()
	{
		Expression<Func<TestClass, TestClass>> expr = x => x.IndexedProperty![0];

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(2, nodes.Count);

		var propertyNode = CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.AreEqual("IndexedProperty", propertyNode.PropertyName);

		CornerstoneTest.IsType<PropertyAccessorNode>(nodes[1]); // List indexer, not array
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParseIndexerWithStringKey()
	{
		Expression<Func<TestClass, int>> expr = x => x.DictionaryProperty!["key"];

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(2, nodes.Count);

		var propertyNode = CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.AreEqual("DictionaryProperty", propertyNode.PropertyName);

		CornerstoneTest.IsType<PropertyAccessorNode>(nodes[1]); // Dictionary indexer
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParseIndexerWithVariableKey()
	{
		var key = "test";
		Expression<Func<TestClass, int>> expr = x => x.DictionaryProperty![key];

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(2, nodes.Count);
		CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.IsType<PropertyAccessorNode>(nodes[1]); // Dictionary indexer
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParseLogicalNot()
	{
		Expression<Func<TestClass, bool>> expr = x => !x.BoolProperty;

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(2, nodes.Count);

		var propertyNode = CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.AreEqual("BoolProperty", propertyNode.PropertyName);

		CornerstoneTest.IsType<LogicalNotNode>(nodes[1]);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParseLogicalNotAfterStreamBinding()
	{
		Expression<Func<TestClass, bool>> expr = x => !x.BoolTaskProperty!.StreamBinding();

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(3, nodes.Count);
		CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.IsType<StreamNode>(nodes[1]);
		CornerstoneTest.IsType<LogicalNotNode>(nodes[2]);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParseLogicalNotInChain()
	{
		Expression<Func<TestClass, bool>> expr = x => !x.Child!.BoolProperty;

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(3, nodes.Count);
		CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.IsType<PropertyAccessorNode>(nodes[1]);
		CornerstoneTest.IsType<LogicalNotNode>(nodes[2]);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParseLongPropertyChain()
	{
		Expression<Func<TestClass, string>> expr = x => x.Child!.Child!.StringProperty;

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(3, nodes.Count);
		CornerstoneTest.All(nodes, n => CornerstoneTest.IsType<PropertyAccessorNode>(n));
		CornerstoneTest.AreEqual("Child", ((PropertyAccessorNode) nodes[0]).PropertyName);
		CornerstoneTest.AreEqual("Child", ((PropertyAccessorNode) nodes[1]).PropertyName);
		CornerstoneTest.AreEqual("StringProperty", ((PropertyAccessorNode) nodes[2]).PropertyName);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParseMultiDimensionalArray()
	{
		Expression<Func<TestClass, string>> expr = x => x.MultiDimensionalArray![0, 1];

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(2, nodes.Count);

		var propertyNode = CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.AreEqual("MultiDimensionalArray", propertyNode.PropertyName);

		CornerstoneTest.IsType<ArrayIndexerNode>(nodes[1]);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParseMultipleLogicalNotOperators()
	{
		Expression<Func<TestClass, bool>> expr = x => !!x.BoolProperty;

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(3, nodes.Count);
		CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.IsType<LogicalNotNode>(nodes[1]);
		CornerstoneTest.IsType<LogicalNotNode>(nodes[2]);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParseObservableStreamBinding()
	{
		Expression<Func<TestClass, int>> expr = x => x.ObservableProperty!.StreamBinding();

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(2, nodes.Count);

		var propertyNode = CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.AreEqual("ObservableProperty", propertyNode.PropertyName);

		CornerstoneTest.IsType<StreamNode>(nodes[1]);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParsePresentationPropertyAccess()
	{
		Expression<Func<StyledElement, object>> expr = x => x[StyledElement.DataContextProperty];

		var nodes = BuildNodes(expr);

		var node = CornerstoneTest.Single(nodes);
		var avaloniaPropertyNode = CornerstoneTest.IsType<PropertyAccessorNode>(node);
		CornerstoneTest.AreEqual("DataContext", avaloniaPropertyNode.PropertyName); // PresentationProperty accessed as property
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParsePresentationPropertyAccessInChain()
	{
		Expression<Func<TestClass, object>> expr = x => x.StyledChild![StyledElement.DataContextProperty];

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(2, nodes.Count);

		var propertyNode = CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.AreEqual("StyledChild", propertyNode.PropertyName);

		var avaloniaPropertyNode = CornerstoneTest.IsType<PropertyAccessorNode>(nodes[1]);
		CornerstoneTest.AreEqual("DataContext", avaloniaPropertyNode.PropertyName); // PresentationProperty accessed as property
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParsePropertyAfterIndexer()
	{
		Expression<Func<TestClass, string>> expr = x => x.IndexedProperty![0]!.StringProperty;

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(3, nodes.Count);
		CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.IsType<PropertyAccessorNode>(nodes[1]); // List indexer
		CornerstoneTest.IsType<PropertyAccessorNode>(nodes[2]);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParsePropertyChain()
	{
		Expression<Func<TestClass, string>> expr = x => x.Child!.StringProperty;

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(2, nodes.Count);

		var firstNode = CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.AreEqual("Child", firstNode.PropertyName);

		var secondNode = CornerstoneTest.IsType<PropertyAccessorNode>(nodes[1]);
		CornerstoneTest.AreEqual("StringProperty", secondNode.PropertyName);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParseSimpleProperty()
	{
		Expression<Func<TestClass, string>> expr = x => x.StringProperty;

		var nodes = BuildNodes(expr);

		var node = CornerstoneTest.Single(nodes);
		var propertyNode = CornerstoneTest.IsType<PropertyAccessorNode>(node);
		CornerstoneTest.AreEqual("StringProperty", propertyNode.PropertyName);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParseStreamBindingInPropertyChain()
	{
		Expression<Func<TestClass, string>> expr = x => x.Child!.TaskProperty!.StreamBinding();

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(3, nodes.Count);
		CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.IsType<PropertyAccessorNode>(nodes[1]);
		CornerstoneTest.IsType<StreamNode>(nodes[2]);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParseTaskStreamBinding()
	{
		Expression<Func<TestClass, string>> expr = x => x.TaskProperty!.StreamBinding();

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(2, nodes.Count);

		var propertyNode = CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.AreEqual("TaskProperty", propertyNode.PropertyName);

		CornerstoneTest.IsType<StreamNode>(nodes[1]);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldParseVoidTaskStreamBinding()
	{
		Expression<Func<TestClass, object>> expr = x => x.VoidTaskProperty!.StreamBinding();

		var nodes = BuildNodes(expr);

		CornerstoneTest.AreEqual(2, nodes.Count);

		var propertyNode = CornerstoneTest.IsType<PropertyAccessorNode>(nodes[0]);
		CornerstoneTest.AreEqual("VoidTaskProperty", propertyNode.PropertyName);

		CornerstoneTest.IsType<StreamNode>(nodes[1]);
	}

	[PresentationTestMethod]
	public void BuildNodesShouldThrowForAdditionOperator()
	{
		Expression<Func<TestClass, int>> expr = x => x.IntProperty + 1;

		var ex = Assert.Throws<ExpressionParseException>(() =>
			BuildNodes(expr));

		CornerstoneTest.Contains(ex.Message, "Invalid expression type");
		CornerstoneTest.Contains(ex.Message, "Add");
	}

	[PresentationTestMethod]
	public void BuildNodesShouldThrowForConditionalExpression()
	{
		Expression<Func<TestClass, string>> expr = x => x.BoolProperty ? "true" : "false";

		var ex = Assert.Throws<ExpressionParseException>(() =>
			BuildNodes(expr));

		CornerstoneTest.Contains(ex.Message, "Invalid expression type");
		CornerstoneTest.Contains(ex.Message, "Conditional");
	}

	[PresentationTestMethod]
	public void BuildNodesShouldThrowForEqualityOperator()
	{
		Expression<Func<TestClass, bool>> expr = x => x.IntProperty == 42;

		var ex = Assert.Throws<ExpressionParseException>(() =>
			BuildNodes(expr));

		CornerstoneTest.Contains(ex.Message, "Invalid expression type");
		CornerstoneTest.Contains(ex.Message, "Equal");
	}

	[PresentationTestMethod]
	public void BuildNodesShouldThrowForMethodCallThatIsNotIndexerOrStreamBinding()
	{
		Expression<Func<TestClass, string>> expr = x => x.StringProperty!.ToUpper();

		var ex = Assert.Throws<ExpressionParseException>(() =>
			BuildNodes(expr));

		CornerstoneTest.Contains(ex.Message, "Invalid method call");
		CornerstoneTest.Contains(ex.Message, "ToUpper");
	}

	[PresentationTestMethod]
	public void BuildNodesShouldThrowForMultiplicationOperator()
	{
		Expression<Func<TestClass, int>> expr = x => x.IntProperty * 2;

		var ex = Assert.Throws<ExpressionParseException>(() =>
			BuildNodes(expr));

		CornerstoneTest.Contains(ex.Message, "Invalid expression type");
		CornerstoneTest.Contains(ex.Message, "Multiply");
	}

	[PresentationTestMethod]
	public void BuildNodesShouldThrowForSubtractionOperator()
	{
		Expression<Func<TestClass, int>> expr = x => x.IntProperty - 1;

		var ex = Assert.Throws<ExpressionParseException>(() =>
			BuildNodes(expr));

		CornerstoneTest.Contains(ex.Message, "Invalid expression type");
		CornerstoneTest.Contains(ex.Message, "Subtract");
	}

	[PresentationTestMethod]
	public void BuildNodesShouldThrowForUnaryMinusOperator()
	{
		Expression<Func<TestClass, int>> expr = x => -x.IntProperty;

		var ex = Assert.Throws<ExpressionParseException>(() =>
			BuildNodes(expr));

		CornerstoneTest.Contains(ex.Message, "Invalid expression type");
		CornerstoneTest.Contains(ex.Message, "Negate");
	}

	[PresentationTestMethod]
	public void BuildNodesShouldThrowForValueTypeCast()
	{
		// Value type conversions should throw
		Expression<Func<TestClass, long>> expr = x => x.IntProperty;

		var ex = Assert.Throws<ExpressionParseException>(() =>
			BuildNodes(expr));

		CornerstoneTest.Contains(ex.Message, "Invalid expression type");
		CornerstoneTest.Contains(ex.Message, "Convert");
	}

	private static List<ExpressionNode> BuildNodes<TIn, TOut>(Expression<Func<TIn, TOut>> expression)
	{
		return BindingExpressionVisitorExtensions.BuildNodes(expression);
	}

	#endregion

	#region Classes

	public class DerivedTestClass : TestClass
	{
		#region Properties

		public string DerivedProperty { get; set; }

		#endregion
	}

	public class TestClass
	{
		#region Properties

		public string[] ArrayProperty { get; set; }
		public bool BoolProperty { get; set; }
		public Task<bool> BoolTaskProperty { get; set; }
		public TestClass Child { get; set; }
		public Dictionary<string, int> DictionaryProperty { get; set; }
		public List<TestClass> IndexedProperty { get; set; }
		public int IntProperty { get; set; }
		public string[,] MultiDimensionalArray { get; set; }
		public List<List<string>> NestedIndexedProperty { get; set; }
		public IObservable<int> ObservableProperty { get; set; }
		public string StringProperty { get; set; }
		public StyledElement StyledChild { get; set; }
		public Task<string> TaskProperty { get; set; }
		public Task VoidTaskProperty { get; set; }

		#endregion
	}

	#endregion
}