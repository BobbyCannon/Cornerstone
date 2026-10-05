#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Diagnostics;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core;

public partial class BindingExpressionTests
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldGetSimpleClrPropertyValue()
	{
		var data = new SourceControl { ClrProperty = "foo" };
		var target = CreateTargetWithSource(data, o => o.ClrProperty);

		CornerstoneTest.AreEqual("foo", target.String);
	}

	[PresentationTestMethod]
	public void ShouldGetSimplePresentationPropertyValue()
	{
		var data = new SourceControl { StringValue = "foo" };
		var target = CreateTargetWithSource(data, o => o.StringValue);

		CornerstoneTest.AreEqual("foo", target.String);
	}

	[PresentationTestMethod]
	public void ShouldNotKeepPresentationPropertySourceAlive()
	{
		Func<(TargetClass, WeakReference<SourceControl>)> run = () =>
		{
			var source = new SourceControl();
			var target = CreateTargetWithSource(
				source,
				o => o.StringValue,
				TargetClass.StringProperty);
			return (target, new WeakReference<SourceControl>(source));
		};

		var result = run();

		GC.Collect();

		CornerstoneTest.IsFalse(result.Item2.TryGetTarget(out _));
	}

	[PresentationTestMethod]
	public void ShouldTrackSimplePresentationPropertyValue()
	{
		var data = new SourceControl { StringValue = "foo" };
		var target = CreateTargetWithSource(data, o => o.StringValue);
		var result = new List<object>();

		CornerstoneTest.AreEqual("foo", target.String);

		data.StringValue = "bar";

		CornerstoneTest.AreEqual("bar", target.String);
	}

	[PresentationTestMethod]
	public void ShouldUnsubscribeFromPresentationPropertySource()
	{
		var data = new SourceControl { StringValue = "foo" };
		var (target, expression) = CreateTargetAndExpression<SourceControl, object>(
			o => o.StringValue,
			source: data,
			targetProperty: TargetClass.StringProperty);

		CornerstoneTest.IsNotNull(((IPresentationObjectDebug) data).GetPropertyChangedSubscribers());

		expression.Dispose();

		CornerstoneTest.IsNull(((IPresentationObjectDebug) data).GetPropertyChangedSubscribers());
	}

	#endregion
}