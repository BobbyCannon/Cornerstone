#region References

using System;
using System.Reactive.Subjects;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core;

public partial class BindingExpressionTests
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldGetPropertyValueFromObservable()
	{
		using var sync = UnitTestSynchronizationContext.Begin();
		var source = new BehaviorSubject<ViewModel>(new() { StringValue = "foo" });
		var data = new ViewModel { NextObservable = source };
		var target = CreateTargetWithSource(data, o => o.NextObservable!.StreamBinding().StringValue);

		CornerstoneTest.AreEqual("foo", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldGetSimpleObservableValue()
	{
		using var sync = UnitTestSynchronizationContext.Begin();
		var source = new BehaviorSubject<string>("foo");
		var data = new { Foo = source };
		var target = CreateTargetWithSource(data, o => o.Foo.StreamBinding());

		CornerstoneTest.AreEqual("foo", target.String);

		source.OnNext("bar");

		CornerstoneTest.AreEqual("bar", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldGetSimpleObservableValueWithDataValidationEnabled()
	{
		using var sync = UnitTestSynchronizationContext.Begin();
		var source = new BehaviorSubject<string>("foo");
		var data = new { Foo = source };
		var target = CreateTargetWithSource(
			data,
			o => o.Foo.StreamBinding(),
			enableDataValidation: true);

		CornerstoneTest.AreEqual("foo", target.String);

		source.OnNext("bar");

		CornerstoneTest.AreEqual("bar", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldNotGetObservableValueWithoutStreaming()
	{
		using var sync = UnitTestSynchronizationContext.Begin();
		var source = new BehaviorSubject<string>("foo");
		var data = new { Foo = source };
		var target = CreateTargetWithSource(data, o => o.Foo);

		CornerstoneTest.Same(source, target);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldWorkWithValueType()
	{
		using var sync = UnitTestSynchronizationContext.Begin();
		var source = new BehaviorSubject<int>(1);
		var data = new { Foo = source };
		var target = CreateTargetWithSource(data, o => o.Foo.StreamBinding());

		CornerstoneTest.AreEqual(1, target.Int);

		source.OnNext(42);

		CornerstoneTest.AreEqual(42, target.Int);

		GC.KeepAlive(data);
	}

	#endregion
}