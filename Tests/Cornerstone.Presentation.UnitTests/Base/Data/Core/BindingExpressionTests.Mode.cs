#region References

using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core;

public partial class BindingExpressionTests
{
	#region Methods

	[PresentationTestMethod]
	public void CanBindReadonlyPropertyOneWayToSource()
	{
		var data = new ViewModel();
		var target = CreateTarget<ViewModel, string>(
			x => x.StringValue,
			dataContext: data,
			mode: BindingMode.OneWayToSource,
			targetProperty: TargetClass.ReadOnlyStringProperty);

		CornerstoneTest.AreEqual("readonly", data.StringValue);

		target.SetReadOnlyString("foo");

		CornerstoneTest.AreEqual("foo", data.StringValue);
	}

	[PresentationTestMethod]
	public void OneTimeBindingSetsTargetOnlyOnceIfDataContextDoesNotChange()
	{
		var data = new ViewModel { Next = new ViewModel { StringValue = "foo" } };
		var target = CreateTarget<ViewModel, string>(x => x.Next!.StringValue, mode: BindingMode.OneTime);
		target.DataContext = data;

		CornerstoneTest.AreEqual("foo", target.String);

		data.Next!.StringValue = "bar";
		CornerstoneTest.AreEqual("foo", target.String);

		data.Next = new ViewModel { StringValue = "baz" };
		CornerstoneTest.AreEqual("foo", target.String);
	}

	[PresentationTestMethod]
	public void OneTimeBindingWaitsForDataContext()
	{
		var target = CreateTarget<ViewModel, string>(
			x => x.StringValue,
			mode: BindingMode.OneTime);

		CornerstoneTest.IsNull(target.String);
	}

	[PresentationTestMethod]
	public void OneTimeBindingWaitsForDataContextWithMatchingPropertyName()
	{
		var data1 = new { Baz = "baz" };
		var data2 = new ViewModel { StringValue = "foo" };
		var target = CreateTarget<ViewModel, string>(
			x => x.StringValue,
			dataContext: data1,
			mode: BindingMode.OneTime);

		CornerstoneTest.IsNull(target.String);

		target.DataContext = data2;
		CornerstoneTest.AreEqual("foo", target.String);

		data2.StringValue = "bar";
		CornerstoneTest.AreEqual("foo", target.String);
	}

	[PresentationTestMethod]
	public void OneTimeBindingWaitsForDataContextWithMatchingPropertyType()
	{
		var data1 = new { DoubleValue = new object() };
		var data2 = new ViewModel { DoubleValue = 0.5 };
		var target = CreateTarget<ViewModel, double>(
			x => x.DoubleValue,
			dataContext: data1,
			mode: BindingMode.OneTime);

		CornerstoneTest.AreEqual(0, target.Double);

		target.DataContext = data2;
		CornerstoneTest.AreEqual(0.5, target.Double);

		data2.DoubleValue = 0.2;
		CornerstoneTest.AreEqual(0.5, target.Double);
	}

	[PresentationTestMethod]
	public void OneTimeBindingWaitsForDataContextWithoutPropertyPath()
	{
		var target = CreateTarget<string, string>(
			x => x,
			mode: BindingMode.OneTime);

		target.DataContext = "foo";

		CornerstoneTest.AreEqual("foo", target.String);
	}

	[PresentationTestMethod]
	public void OneTimeBindingWaitsForDataContextWithoutPropertyPathWithStringFormat()
	{
		var target = CreateTarget<string, string>(
			x => x,
			mode: BindingMode.OneTime,
			stringFormat: "bar: {0}");

		target.DataContext = "foo";

		CornerstoneTest.AreEqual("bar: foo", target.String);
	}

	[PresentationTestMethod]
	public void OneTimeBindingWithComplexPathSetsTargetWhenDataContextChanges()
	{
		var data1 = new ViewModel { Next = new ViewModel { StringValue = "foo" } };
		var target = CreateTarget<ViewModel, string>(x => x.Next!.StringValue, mode: BindingMode.OneTime);
		target.DataContext = data1;

		CornerstoneTest.AreEqual("foo", target.String);

		var data2 = new ViewModel { Next = new ViewModel { StringValue = "bar" } };
		target.DataContext = data2;
		CornerstoneTest.AreEqual("bar", target.String);
	}

	[PresentationTestMethod]
	public void OneTimeBindingWithSimplePathSetsTargetWhenDataContextChanges()
	{
		var data1 = new ViewModel { StringValue = "foo" };
		var target = CreateTarget<ViewModel, string>(x => x.StringValue, mode: BindingMode.OneTime);
		target.DataContext = data1;

		CornerstoneTest.AreEqual("foo", target.String);

		var data2 = new ViewModel { StringValue = "bar" };
		target.DataContext = data2;
		CornerstoneTest.AreEqual("bar", target.String);
	}

	[PresentationTestMethod]
	public void OneTimeBindingWithoutPathSetsTargetWhenDataContextChanges()
	{
		var target = CreateTarget<string, string>(x => x, mode: BindingMode.OneTime);
		target.DataContext = "foo";

		CornerstoneTest.AreEqual("foo", target.String);

		target.DataContext = "bar";
		CornerstoneTest.AreEqual("bar", target.String);
	}

	[PresentationTestMethod]
	public void OneWayBindingUpdatesTargetWhenChangesAndSourceRaisesPropertyChanged()
	{
		var data = new ViewModel { StringValue = "foo" };
		var target = CreateTarget<ViewModel, string>(
			x => x.StringValue,
			dataContext: data,
			mode: BindingMode.OneWay);

		CornerstoneTest.AreEqual("foo", target.String);

		target.SetCurrentValue(TargetClass.StringProperty, "bar");
		CornerstoneTest.AreEqual("bar", target.String);

		data.RaisePropertyChanged(nameof(data.StringValue));

		CornerstoneTest.AreEqual("foo", target.String);
	}

	[PresentationTestMethod]
	public void OneWayToSourceBindingDoesNotUpdateTargetWhenSourceChanges()
	{
		var data = new ViewModel();
		var target = CreateTarget<ViewModel, string>(
			x => x.StringValue,
			dataContext: data,
			mode: BindingMode.OneWayToSource);

		target.String = "foo";
		CornerstoneTest.AreEqual("foo", data.StringValue);

		data.StringValue = "bar";
		CornerstoneTest.AreEqual("foo", target.String);
	}

	[PresentationTestMethod]
	public void OneWayToSourceBindingUpdatesSourceWhenDataContextChanges()
	{
		var data1 = new ViewModel();
		var data2 = new ViewModel();
		var target = CreateTarget<ViewModel, string>(
			x => x.StringValue,
			dataContext: data1,
			mode: BindingMode.OneWayToSource);

		target.String = "foo";
		CornerstoneTest.AreEqual("foo", data1.StringValue);

		target.DataContext = data2;
		CornerstoneTest.AreEqual("foo", data2.StringValue);
	}

	[PresentationTestMethod]
	public void OneWayToSourceBindingUpdatesSourceWhenTargetChanges()
	{
		var data = new ViewModel();
		var target = CreateTarget<ViewModel, string>(
			x => x.StringValue,
			dataContext: data,
			mode: BindingMode.OneWayToSource);

		CornerstoneTest.IsNull(data.StringValue);

		target.String = "foo";

		CornerstoneTest.AreEqual("foo", data.StringValue);
	}

	#endregion
}