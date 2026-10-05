#region References

using System;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core;

public abstract partial class BindingExpressionTests
{
	#region Methods

	[PresentationTestMethod]
	public void CanUseUpdateTargetToUpdateFromNonINPCData()
	{
		var data = new PodViewModel { StringValue = "foo" };
		var (target, expression) = CreateTargetAndExpression<PodViewModel, string>(
			o => o.StringValue,
			source: data);

		CornerstoneTest.AreEqual("foo", target.String);

		data.StringValue = "bar";
		CornerstoneTest.AreEqual("foo", target.String);

		expression.UpdateTarget();
		CornerstoneTest.AreEqual("bar", target.String);
	}

	[PresentationTestMethod]
	public void LeafNodeShouldBeNullWhenNodesListIsEmpty()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			// Reproduces issue #20441
			// Create a binding expression with no nodes (e.g., {Binding Source='Elements', Converter={...}})
			var bindingExpression = new BindingExpression(
				"Elements",
				null, // This results in an empty nodes list
				PresentationProperty.UnsetValue,
				converter: new PrefixConverter("Prefix"),
				mode: BindingMode.OneWay,
				targetProperty: TargetClass.StringProperty,
				targetTypeConverter: TargetTypeConverter.GetReflectionConverter());

			// These should not throw
			var leafNode = bindingExpression.LeafNode;
			var description = bindingExpression.Description;

			// LeafNode should be null when there are no nodes
			CornerstoneTest.IsNull(leafNode);
		}
	}

	[PresentationTestMethod]
	public void ShouldConvertDoubleToString()
	{
		var data = new ViewModel { DoubleValue = 5.6 };
		var target = CreateTargetWithSource(
			data,
			o => o.DoubleValue,
			TargetClass.StringProperty);

		CornerstoneTest.AreEqual($"{5.6}", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldConvertStringToDouble()
	{
		var data = new ViewModel { StringValue = $"{5.6}" };
		var target = CreateTargetWithSource(
			data,
			o => o.StringValue,
			TargetClass.DoubleProperty);

		CornerstoneTest.AreEqual(5.6, target.Double);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldGetSourceValue()
	{
		var data = "foo";
		var target = CreateTargetWithSource(data, o => o);

		CornerstoneTest.AreEqual("foo", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldNotPassUnsetValueToConverterUntilFirstValueProduced()
	{
		var data = new ViewModel { StringValue = "Bar" };
		var converter = new PrefixConverter();
		var target = CreateTarget<ViewModel, string>(
			o => o.StringValue,
			converter: converter,
			converterParameter: "foo");

		CornerstoneTest.IsNull(target.String);

		target.DataContext = data;

		CornerstoneTest.AreEqual("fooBar", target.String);
	}

	[PresentationTestMethod]
	public void ShouldPassConverterParameterToConverter()
	{
		var data = new ViewModel { DoubleValue = 5.6 };
		var converter = new PrefixConverter();
		var target = CreateTargetWithSource(
			data,
			o => o.DoubleValue,
			converter: converter,
			converterParameter: "foo",
			targetProperty: TargetClass.StringProperty);

		CornerstoneTest.AreEqual("foo5.6", target.String);
	}

	[PresentationTestMethod]
	public void ShouldUseConverterForNullDataContextWithoutPath()
	{
		var converter = new PrefixConverter();
		var target = CreateTarget<string, string>(
			o => o,
			converter: converter,
			converterParameter: "foo");

		CornerstoneTest.AreEqual("foo", target.String);
	}

	[PresentationTestMethod]
	public void ShouldUseConverterForRelativeSourceSelfBindingWithNoPath()
	{
		var converter = new PrefixConverter();
		var target = CreateTarget<TargetClass, TargetClass>(
			o => o,
			converter: converter,
			converterParameter: "foo",
			relativeSource: new RelativeSource(RelativeSourceMode.Self),
			targetProperty: TargetClass.StringProperty);

		CornerstoneTest.AreEqual("fooTargetClass", target.String);
	}

	[PresentationTestMethod]
	public void ShouldUseFallbackValueForNonConvertibleTargetValue()
	{
		var data = new ViewModel { StringValue = "foo" };
		var target = CreateTargetWithSource(
			data,
			o => o.StringValue,
			fallbackValue: 42,
			targetProperty: TargetClass.IntProperty);

		CornerstoneTest.AreEqual(42, target.Int);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void TargetNullValueShouldBeUsedWhenSourceIsDataContextAndNull()
	{
		var target = CreateTarget<string, string>(
			o => o,
			targetNullValue: "bar");

		CornerstoneTest.AreEqual("bar", target.String);
	}

	[PresentationTestMethod]
	public void TargetNullValueShouldBeUsedWhenSourceStringIsNull()
	{
		var data = new ViewModel { StringValue = "foo" };
		var target = CreateTargetWithSource(
			data,
			o => o.StringValue,
			targetNullValue: "bar");

		CornerstoneTest.AreEqual("foo", target.String);

		data.StringValue = null;
		CornerstoneTest.AreEqual("bar", target.String);

		GC.KeepAlive(data);
	}

	#endregion
}