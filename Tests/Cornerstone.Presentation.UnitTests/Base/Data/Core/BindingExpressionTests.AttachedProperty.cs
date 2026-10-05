#region References

using System;
using Cornerstone.Presentation.Diagnostics;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core;

public partial class BindingExpressionTests
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldGetAttachedPropertyValue()
	{
		var data = new SourceControl { [AttachedProperties.AttachedStringProperty] = "foo" };
		var target = CreateTargetWithSource(
			data,
			o => o[AttachedProperties.AttachedStringProperty],
			TargetClass.StringProperty);

		CornerstoneTest.AreEqual("foo", target.String);
	}

	[PresentationTestMethod]
	public void ShouldGetChainedAttachedPropertyValue()
	{
		var data = new SourceControl
		{
			Next = new() { [AttachedProperties.AttachedStringProperty] = "foo" }
		};

		var target = CreateTargetWithSource(
			data,
			o => o.Next![AttachedProperties.AttachedStringProperty],
			TargetClass.StringProperty);

		CornerstoneTest.AreEqual("foo", target.String);
	}

	[PresentationTestMethod]
	public void ShouldNotKeepAttachedPropertySourceAlive()
	{
		Func<(TargetClass, WeakReference<SourceControl>)> run = () =>
		{
			var source = new SourceControl();
			var target = CreateTargetWithSource(
				source,
				o => o.Next![AttachedProperties.AttachedStringProperty],
				TargetClass.StringProperty);
			return (target, new WeakReference<SourceControl>(source));
		};

		var result = run();

		GC.Collect();

		CornerstoneTest.IsFalse(result.Item2.TryGetTarget(out _));
		GC.KeepAlive(result.Item1);
	}

	[PresentationTestMethod]
	public void ShouldTrackChainedAttachedValue()
	{
		var data = new SourceControl
		{
			Next = new() { [AttachedProperties.AttachedStringProperty] = "foo" }
		};

		var target = CreateTargetWithSource(
			data,
			o => o.Next![AttachedProperties.AttachedStringProperty],
			TargetClass.StringProperty);

		CornerstoneTest.AreEqual("foo", target.String);

		data.Next!.SetValue(AttachedProperties.AttachedStringProperty, "bar");

		CornerstoneTest.AreEqual("bar", target.String);
	}

	[PresentationTestMethod]
	public void ShouldTrackSimpleAttachedValue()
	{
		var data = new SourceControl { [AttachedProperties.AttachedStringProperty] = "foo" };
		var target = CreateTargetWithSource(
			data,
			o => o[AttachedProperties.AttachedStringProperty],
			TargetClass.StringProperty);

		CornerstoneTest.AreEqual("foo", target.String);

		data.SetValue(AttachedProperties.AttachedStringProperty, "bar");

		CornerstoneTest.AreEqual("bar", target.String);
	}

	[PresentationTestMethod]
	public void ShouldUnsubscribeFromAttachedPropertySource()
	{
		var data = new SourceControl { [AttachedProperties.AttachedStringProperty] = "foo" };
		var (target, expression) = CreateTargetAndExpression<SourceControl, object>(
			o => o[AttachedProperties.AttachedStringProperty],
			source: data,
			targetProperty: TargetClass.StringProperty);

		CornerstoneTest.IsNotNull(((IPresentationObjectDebug) data).GetPropertyChangedSubscribers());

		expression.Dispose();

		CornerstoneTest.IsNull(((IPresentationObjectDebug) data).GetPropertyChangedSubscribers());
	}

	[PresentationTestMethod]
	public void ShouldUnsubscribeFromChainedSource()
	{
		var data = new SourceControl
		{
			Next = new() { [AttachedProperties.AttachedStringProperty] = "foo" }
		};

		var (target, expression) = CreateTargetAndExpression<SourceControl, object>(
			o => o.Next![AttachedProperties.AttachedStringProperty],
			source: data,
			targetProperty: TargetClass.StringProperty);

		CornerstoneTest.IsNotNull(((IPresentationObjectDebug) data.Next).GetPropertyChangedSubscribers());

		expression.Dispose();

		CornerstoneTest.IsNull(((IPresentationObjectDebug) data.Next).GetPropertyChangedSubscribers());
	}

	#endregion
}