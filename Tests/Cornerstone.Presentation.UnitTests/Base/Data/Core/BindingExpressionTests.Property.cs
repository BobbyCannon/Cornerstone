#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core;

public partial class BindingExpressionTests
{
	#region Methods

	[PresentationTestMethod]
	public void CanConvertIntToEnumTwoWay()
	{
		var data = new ViewModel { IntValue = 1 };
		var target = CreateTargetWithSource(
			data,
			o => o.IntValue,
			mode: BindingMode.TwoWay,
			targetProperty: DockPanel.DockProperty);

		CornerstoneTest.AreEqual(Dock.Bottom, DockPanel.GetDock(target));

		DockPanel.SetDock(target, Dock.Right);

		CornerstoneTest.AreEqual(2, data.IntValue);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ConverterShouldBeCalledOnPropertyChangedEvenIfPropertyNotChanged()
	{
		// Issue #16137
		var data = new ViewModel();
		var converter = new PrefixConverter("foo");
		var target = CreateTargetWithSource(
			data,
			o => o.IntValue,
			converter: converter,
			targetProperty: TargetClass.StringProperty);

		CornerstoneTest.AreEqual("foo0", target.String);

		converter.Prefix = "bar";
		data.RaisePropertyChanged(nameof(data.IntValue));

		CornerstoneTest.AreEqual("bar0", target.String);
	}

	[PresentationTestMethod]
	public void PropertyChangedEventArgsWithNullPropertyNameShouldTriggerUpdate()
	{
		var data = new ViewModel { StringValue = "foo" };
		var target = CreateTargetWithSource(data, o => o.StringValue);

		CornerstoneTest.AreEqual("foo", target.String);

		data.SetStringValueWithoutRaising("bar");

		CornerstoneTest.AreEqual("foo", target.String);

		data.RaisePropertyChanged(null);

		CornerstoneTest.AreEqual("bar", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldGetSimplePropertyChain()
	{
		var data = new { Foo = new { Bar = new { Baz = "baz" } } };
		var target = CreateTargetWithSource(data, o => o.Foo.Bar.Baz);

		CornerstoneTest.AreEqual("baz", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldGetSimplePropertyFromBaseClass()
	{
		var data = new DerivedViewModel { StringValue = "foo" };
		var target = CreateTargetWithSource(data, o => o.StringValue);

		CornerstoneTest.AreEqual("foo", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldGetSimplePropertyValue()
	{
		var data = new { Foo = "foo" };
		var target = CreateTargetWithSource(data, o => o.Foo);

		CornerstoneTest.AreEqual("foo", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldGetSimplePropertyValueNull()
	{
		var data = new { Foo = (string) null };
		var target = CreateTargetWithSource(data, o => o.Foo);

		CornerstoneTest.IsNull(target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldNotKeepSourceAlive()
	{
		Func<(TargetClass, WeakReference<ViewModel>)> run = () =>
		{
			var source = new ViewModel { StringValue = "foo" };
			var target = CreateTargetWithSource(source, o => o.StringValue);
			return (target, new(source));
		};

		var result = run();

		// Mono trickery
		GC.Collect(2);
		GC.WaitForPendingFinalizers();
		GC.WaitForPendingFinalizers();
		GC.Collect(2);

		CornerstoneTest.IsFalse(result.Item2.TryGetTarget(out _));
	}

	[PresentationTestMethod]
	public void ShouldNotThrowExceptionOnDuplicateProperties()
	{
		// Repro of https://github.com/AvaloniaUI/Avalonia/issues/4733.
		var source = new DerivedViewModelWithDuplicateProperty { StringValue = "NewName" };
		var target = CreateTargetWithSource(source, x => x.StringValue);

		CornerstoneTest.AreEqual("NewName", target.String);
	}

	[PresentationTestMethod]
	public void ShouldTrackEndOfPropertyChainChanging()
	{
		var data = new ViewModel { Next = new() { StringValue = "bar" } };
		var target = CreateTargetWithSource(data, o => o.Next!.StringValue);

		CornerstoneTest.AreEqual("bar", target.String);

		data.Next.StringValue = "baz";

		CornerstoneTest.AreEqual("baz", target.String);

		data.Next.StringValue = null;

		CornerstoneTest.IsNull(target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldTrackPropertyChainBreakingWithMissingMemberThenMending()
	{
		var data = new ViewModel { ObjectValue = new ViewModel { StringValue = "bar" } };
		var target = CreateTargetWithSource(data, o => (o.ObjectValue as ViewModel)!.StringValue);

		CornerstoneTest.AreEqual("bar", target.String);

		var old = data.ObjectValue;
		data.ObjectValue = new { MissingMember = "Yes" };

		CornerstoneTest.IsNull(target.String);

		data.ObjectValue = old;

		CornerstoneTest.AreEqual("bar", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldTrackPropertyChainBreakingWithNullThenMending()
	{
		var data = new ViewModel
		{
			Next = new()
			{
				Next = new()
				{
					StringValue = "bar"
				}
			}
		};

		var target = CreateTargetWithSource(data, o => o.Next!.Next!.StringValue);

		CornerstoneTest.AreEqual("bar", target.String);

		var old = data.Next;
		data.Next = null;

		CornerstoneTest.IsNull(target.String);

		data.Next = old;

		CornerstoneTest.AreEqual("bar", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldTrackPropertyChainChanging()
	{
		var data = new ViewModel { Next = new() { StringValue = "bar" } };
		var target = CreateTargetWithSource(data, o => o.Next!.StringValue);
		var old = data.Next;

		CornerstoneTest.AreEqual("bar", target.String);

		data.Next = new() { StringValue = "baz" };

		CornerstoneTest.AreEqual("baz", target.String);

		data.Next = new() { StringValue = null };

		CornerstoneTest.IsNull(target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldTrackSimplePropertyValue()
	{
		var data = new ViewModel { StringValue = "foo" };
		var target = CreateTargetWithSource(data, o => o.StringValue);

		CornerstoneTest.AreEqual("foo", target.String);

		data.StringValue = "bar";

		CornerstoneTest.AreEqual("bar", target.String);

		GC.KeepAlive(data);
	}

	#endregion

	#region Classes

	private class DerivedViewModel : ViewModel
	{
	}

	private class DerivedViewModelWithDuplicateProperty : ViewModel
	{
		#region Properties

		public new string StringValue { get; set; }

		#endregion
	}

	#endregion
}