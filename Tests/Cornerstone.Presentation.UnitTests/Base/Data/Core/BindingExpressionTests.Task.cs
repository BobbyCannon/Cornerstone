#region References

using System;
using System.Threading.Tasks;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core;

public partial class BindingExpressionTests
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldGetCompletedTaskValue()
	{
		using var sync = UnitTestSynchronizationContext.Begin();
		var data = new { Foo = Task.FromResult("foo") };
		var target = CreateTargetWithSource(data, o => o.Foo.StreamBinding());

		CornerstoneTest.AreEqual("foo", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldGetPropertyValueFromTask()
	{
		using var sync = UnitTestSynchronizationContext.Begin();
		var tcs = new TaskCompletionSource<ViewModel>();
		var data = new ViewModel { NextTask = tcs.Task };
		var target = CreateTargetWithSource(data, o => o.NextTask.StreamBinding().StringValue);

		tcs.SetResult(new ViewModel { StringValue = "foo" });
		sync.ExecutePostedCallbacks();

		CornerstoneTest.AreEqual("foo", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldGetSimpleTaskValueWithDataDataValidationEnabled()
	{
		using var sync = UnitTestSynchronizationContext.Begin();
		var tcs = new TaskCompletionSource<string>();
		var data = new { Foo = tcs.Task };
		var target = CreateTargetWithSource(
			data,
			o => o.Foo.StreamBinding(),
			enableDataValidation: true);

		tcs.SetResult("foo");
		sync.ExecutePostedCallbacks();

		// What does it mean to have data validation on a Task Without a use-case it's
		// hard to know what to do here so for the moment the value is returned.
		CornerstoneTest.AreEqual("foo", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldNotGetTaskResultWithoutStreamBinding()
	{
		using var sync = UnitTestSynchronizationContext.Begin();
		var tcs = new TaskCompletionSource<string>();
		var data = new { Foo = tcs.Task };
		var target = CreateTargetWithSource(data, o => o.Foo);

		CornerstoneTest.IsNull(target.String);

		tcs.SetResult("foo");
		sync.ExecutePostedCallbacks();

		CornerstoneTest.IsNull(target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldUpdateDataValidationOnFaultedTask()
	{
		using var sync = UnitTestSynchronizationContext.Begin();
		var data = new { Foo = TaskFromException(new NotSupportedException()) };
		var target = CreateTargetWithSource(
			data,
			o => o.Foo.StreamBinding(),
			enableDataValidation: true);

		AssertBindingError(
			target,
			TargetClass.StringProperty,
			new BindingChainException("Specified method is not supported.", "Foo^", "^"),
			BindingErrorType.Error);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldUpdateDataValidationOnTaskException()
	{
		using var sync = UnitTestSynchronizationContext.Begin();
		var tcs = new TaskCompletionSource<string>();
		var data = new { Foo = tcs.Task };
		var target = CreateTargetWithSource(
			data,
			o => o.Foo.StreamBinding(),
			enableDataValidation: true);

		tcs.SetException(new NotSupportedException());
		sync.ExecutePostedCallbacks();

		AssertBindingError(
			target,
			TargetClass.StringProperty,
			new BindingChainException("Specified method is not supported.", "Foo^", "^"),
			BindingErrorType.Error);

		GC.KeepAlive(data);
	}

	private static Task<string> TaskFromException(Exception e)
	{
		var tcs = new TaskCompletionSource<string>();
		tcs.SetException(e);
		return tcs.Task;
	}

	#endregion
}