#region References

using System.Collections.Generic;
using System.Reactive.Subjects;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.PropertyStore;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Reactive.Testing.ReactiveTest;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.PropertyStore;

[TestClass]
public class ValueStoreTestsFrames
{
	#region Methods

	[PresentationTestMethod]
	public void AddingFrameRaisesPropertyChanged()
	{
		var target = new Class1();
		var subject = new BehaviorSubject<string>("bar");
		var result = new List<PropertyChange>();
		var style = new Style
		{
			Setters =
			{
				new Setter(Class1.FooProperty, "foo"),
				new Setter(Class1.BarProperty, subject.ToBinding())
			}
		};

		target.PropertyChanged += (s, e) => { result.Add(new(e.Property, e.OldValue, e.NewValue)); };

		var frame = InstanceStyle(style, target);
		target.GetValueStore().AddFrame(frame);

		CornerstoneTest.AreEqual(new PropertyChange[]
		{
			new(Class1.FooProperty, "foodefault", "foo"),
			new(Class1.BarProperty, "bardefault", "bar")
		}, result);
	}

	[PresentationTestMethod]
	public void CompletingObservableRemovesImmediateValueFrame()
	{
		var target = new Class1();
		var source = new BehaviorSubject<BindingValue<string>>("foo");

		target.Bind(Class1.FooProperty, source, BindingPriority.Animation);

		var valueStore = target.GetValueStore();
		CornerstoneTest.AreEqual(1, valueStore.Frames.Count);
		CornerstoneTest.IsType<ImmediateValueFrame>(valueStore.Frames[0]);

		source.OnCompleted();

		CornerstoneTest.AreEqual(0, valueStore.Frames.Count);
	}

	[PresentationTestMethod]
	public void DisposingBindingRemovesImmediateValueFrame()
	{
		var target = new Class1();
		var source = new Binding { Priority = BindingPriority.Style };
		var expression = target.Bind(Class1.FooProperty, source);

		var valueStore = target.GetValueStore();
		CornerstoneTest.AreEqual(1, valueStore.Frames.Count);
		CornerstoneTest.IsType<ImmediateValueFrame>(valueStore.Frames[0]);

		expression.Dispose();

		CornerstoneTest.AreEqual(0, valueStore.Frames.Count);
	}

	[PresentationTestMethod]
	public void RemovingFrameRaisesPropertyChanged()
	{
		var target = new Class1();
		var subject = new BehaviorSubject<string>("bar");
		var result = new List<PropertyChange>();
		var style = new Style
		{
			Setters =
			{
				new Setter(Class1.FooProperty, "foo"),
				new Setter(Class1.BarProperty, subject.ToBinding())
			}
		};
		var frame = InstanceStyle(style, target);
		target.GetValueStore().AddFrame(frame);

		target.PropertyChanged += (s, e) => { result.Add(new(e.Property, e.OldValue, e.NewValue)); };

		target.GetValueStore().RemoveFrame(frame);

		CornerstoneTest.AreEqual(new PropertyChange[]
		{
			new(Class1.FooProperty, "foo", "foodefault"),
			new(Class1.BarProperty, "bar", "bardefault")
		}, result);
	}

	[PresentationTestMethod]
	public void RemovingFrameUnsubscribesBinding()
	{
		var target = new Class1();
		var scheduler = new TestScheduler();
		var obs = scheduler.CreateColdObservable(OnNext(0, "bar"));
		var style = new Style
		{
			Setters =
			{
				new Setter(Class1.FooProperty, "foo"),
				new Setter(Class1.BarProperty, obs.ToBinding())
			}
		};
		var frame = InstanceStyle(style, target);

		target.GetValueStore().AddFrame(frame);
		target.GetValueStore().RemoveFrame(frame);

		CornerstoneTest.Single(obs.Subscriptions);
		CornerstoneTest.AreEqual(0, obs.Subscriptions[0].Subscribe);
		CornerstoneTest.AreNotEqual(Subscription.Infinite, obs.Subscriptions[0].Unsubscribe);
	}

	private static StyleInstance InstanceStyle(Style style, StyledElement target)
	{
		var result = new StyleInstance(style, null, FrameType.Style);

		foreach (var setter in style.Setters)
		{
			result.Add(setter.Instance(result, target));
		}

		return result;
	}

	#endregion

	#region Classes

	private class Class1 : StyledElement
	{
		#region Fields

		public static readonly StyledProperty<string> BarProperty =
			PresentationProperty.Register<Class1, string>("Bar", "bardefault", true);

		public static readonly StyledProperty<string> FooProperty =
			PresentationProperty.Register<Class1, string>("Foo", "foodefault");

		#endregion
	}

	#endregion

	#region Records

	private record PropertyChange(
		PresentationProperty Property,
		object OldValue,
		object NewValue);

	#endregion
}