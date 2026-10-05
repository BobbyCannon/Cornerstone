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
	public void OneWayToSourceLostFocusShouldUpdateSourceOnLostFocus()
	{
		using var app = StartWithFocusSupport();
		var data = new ViewModel { StringValue = "foo" };
		var target = CreateTargetWithSource(
			data,
			o => o.StringValue,
			mode: BindingMode.OneWayToSource,
			updateSourceTrigger: UpdateSourceTrigger.LostFocus);
		var root = new TestRoot(target) { Focusable = true };

		target.Focus();

		CornerstoneTest.IsNull(target.String);
		CornerstoneTest.AreEqual("foo", data.StringValue);

		target.String = "bar";

		CornerstoneTest.AreEqual("bar", target.String);
		CornerstoneTest.AreEqual("foo", data.StringValue);

		root.Focus();

		CornerstoneTest.AreEqual("bar", target.String);
		CornerstoneTest.AreEqual("bar", data.StringValue);
	}

	[PresentationTestMethod]
	public void TwoWayExplicitShouldUpdateSourceOnCallToUpdateSource()
	{
		using var app = StartWithFocusSupport();
		var data = new ViewModel { StringValue = "foo" };
		var (target, expression) = CreateTargetAndExpression<ViewModel, string>(
			o => o.StringValue,
			mode: BindingMode.TwoWay,
			source: data,
			updateSourceTrigger: UpdateSourceTrigger.Explicit);
		var root = new TestRoot(target) { Focusable = true };

		target.Focus();

		CornerstoneTest.AreEqual("foo", target.String);
		CornerstoneTest.AreEqual("foo", data.StringValue);

		target.String = "bar";

		CornerstoneTest.AreEqual("bar", target.String);
		CornerstoneTest.AreEqual("foo", data.StringValue);

		root.Focus();

		CornerstoneTest.AreEqual("bar", target.String);
		CornerstoneTest.AreEqual("foo", data.StringValue);

		expression.UpdateSource();

		CornerstoneTest.AreEqual("bar", target.String);
		CornerstoneTest.AreEqual("bar", data.StringValue);
	}

	[PresentationTestMethod]
	public void TwoWayExplicitShouldUpdateTargetOnCallToUpdateTarget()
	{
		var data = new ViewModel { StringValue = "foo" };
		var (target, expression) = CreateTargetAndExpression<ViewModel, string>(
			o => o.StringValue,
			mode: BindingMode.TwoWay,
			source: data,
			updateSourceTrigger: UpdateSourceTrigger.Explicit);

		CornerstoneTest.AreEqual("foo", target.String);
		CornerstoneTest.AreEqual("foo", data.StringValue);

		target.String = "bar";

		CornerstoneTest.AreEqual("bar", target.String);
		CornerstoneTest.AreEqual("foo", data.StringValue);

		// UpdateTarget forces a transfer from source to target, discarding the
		// value set on the target.
		expression.UpdateTarget();

		CornerstoneTest.AreEqual("foo", target.String);
		CornerstoneTest.AreEqual("foo", data.StringValue);
	}

	[PresentationTestMethod]
	public void TwoWayLostFocusShouldUpdateSourceOnLostFocus()
	{
		using var app = StartWithFocusSupport();
		var data = new ViewModel { StringValue = "foo" };
		var target = CreateTargetWithSource(
			data,
			o => o.StringValue,
			mode: BindingMode.TwoWay,
			updateSourceTrigger: UpdateSourceTrigger.LostFocus);
		var root = new TestRoot(target) { Focusable = true };

		target.Focus();

		CornerstoneTest.AreEqual("foo", target.String);
		CornerstoneTest.AreEqual("foo", data.StringValue);

		target.String = "bar";

		CornerstoneTest.AreEqual("bar", target.String);
		CornerstoneTest.AreEqual("foo", data.StringValue);

		root.Focus();

		CornerstoneTest.AreEqual("bar", target.String);
		CornerstoneTest.AreEqual("bar", data.StringValue);
	}

	[PresentationTestMethod]
	public void TwoWayPropertyChangedShouldUpdateSourceOnPropertyChanged()
	{
		var data = new ViewModel { StringValue = "foo" };
		var target = CreateTargetWithSource(data, o => o.StringValue, mode: BindingMode.TwoWay);

		CornerstoneTest.AreEqual("foo", target.String);
		CornerstoneTest.AreEqual("foo", data.StringValue);

		target.String = "bar";

		CornerstoneTest.AreEqual("bar", target.String);
		CornerstoneTest.AreEqual("bar", data.StringValue);
	}

	#endregion
}