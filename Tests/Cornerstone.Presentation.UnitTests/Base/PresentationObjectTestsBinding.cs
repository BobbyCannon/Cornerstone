#region References

using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Base.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class PresentationObjectTestsBinding : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BindDoesNotThrowExceptionForUnregisteredProperty()
	{
		var target = new Class1();

		target.Bind(Class2.BarProperty, Observable.Never<BindingValue<string>>().StartWith("foo"));

		CornerstoneTest.AreEqual("foo", target.GetValue(Class2.BarProperty));
	}

	[PresentationTestMethod]
	public void BindIgnoresInvalidValueType()
	{
		var target = new Class1();
		target.Bind((PresentationProperty) Class1.FooProperty, Observable.Return((object) 123));
		CornerstoneTest.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void BindNonGenericCanSetNullOnReferenceType()
	{
		var target = new Class1();
		var source = new BehaviorSubject<object>(null);
		var property = Class1.FooProperty;

		target.Bind(property, source);

		CornerstoneTest.IsNull(target.GetValue(property));
	}

	[PresentationTestMethod]
	public void BindNonGenericSetsCurrentValue()
	{
		var target = new Class1();
		var source = new Class1();

		source.SetValue(Class1.FooProperty, "initial");
		target.Bind((PresentationProperty) Class1.FooProperty, source.GetObservable(Class1.FooProperty));

		CornerstoneTest.AreEqual("initial", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void BindRaisesPropertyChanged()
	{
		var target = new Class1();
		var source = new Subject<BindingValue<string>>();
		var raised = 0;

		target.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(Class1.FooProperty, e.Property);
			CornerstoneTest.AreEqual("foodefault", (string) e.OldValue);
			CornerstoneTest.AreEqual("newvalue", (string) e.NewValue);
			CornerstoneTest.AreEqual(BindingPriority.LocalValue, e.Priority);
			++raised;
		};

		target.Bind(Class1.FooProperty, source);
		source.OnNext("newvalue");

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void BindSetsCurrentValue()
	{
		var target = new Class1();
		var source = new BehaviorSubject<BindingValue<string>>("initial");
		var property = Class1.FooProperty;

		target.Bind(property, source);

		CornerstoneTest.AreEqual("initial", target.GetValue(property));
	}

	[PresentationTestMethod]
	public void BindSetsSubsequentValue()
	{
		var target = new Class1();
		var source = new Class1();

		source.SetValue(Class1.FooProperty, "initial");
		target.Bind(Class1.FooProperty, source.GetObservable(Class1.FooProperty));
		source.SetValue(Class1.FooProperty, "subsequent");

		CornerstoneTest.AreEqual("subsequent", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void BindWithSchedulerExecutesOnUIThread()
	{
		using var _ = UnitTestApplication.Start();

		var target = new Class1();
		var source = new Subject<double>();
		target.Bind(Class1.QuxProperty, source);

		ThreadRunHelper.RunOnDedicatedThread(() => source.OnNext(6.7)).GetAwaiter().GetResult();
		CornerstoneTest.AreNotEqual(6.7, target.GetValue(Class1.QuxProperty));
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
		CornerstoneTest.AreEqual(6.7, target.GetValue(Class1.QuxProperty));
	}

	[PresentationTestMethod]
	public void BindingErrorRevertsToDefaultValue()
	{
		var target = new Class1();
		var source = new Subject<BindingValue<string>>();

		target.Bind(Class1.FooProperty, source);
		source.OnNext("initial");
		source.OnNext(BindingValue<string>.BindingError(new InvalidOperationException("Foo")));

		CornerstoneTest.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void BindingErrorWithFallbackValueCausesTargetUpdate()
	{
		var target = new Class1();
		var source = new Subject<BindingValue<string>>();

		target.Bind(Class1.FooProperty, source);
		source.OnNext("initial");
		source.OnNext(BindingValue<string>.BindingError(new InvalidOperationException("Foo"), "bar"));

		CornerstoneTest.AreEqual("bar", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	[DataRow(BindingPriority.LocalValue)]
	[DataRow(BindingPriority.Style)]
	public void BindingProducingUnsetValueDoesNotCauseUnsubscribe(BindingPriority priority)
	{
		var target = new Class1();
		var source = new Subject<BindingValue<string>>();

		target.Bind(Class1.FooProperty, source, priority);

		source.OnNext("foo");
		CornerstoneTest.AreEqual("foo", target.GetValue(Class1.FooProperty));
		source.OnNext(BindingValue<string>.Unset);
		CornerstoneTest.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
		source.OnNext("bar");
		CornerstoneTest.AreEqual("bar", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	[DataRow(BindingPriority.LocalValue)]
	[DataRow(BindingPriority.Style)]
	public void BindingValueBindExecutesOnUIThread(BindingPriority priority)
	{
		using var _ = UnitTestApplication.Start();
		var target = new Class1();
		var source = new Subject<BindingValue<string>>();
		var currentThreadId = Thread.CurrentThread.ManagedThreadId;
		var raised = 0;

		target.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(currentThreadId, Thread.CurrentThread.ManagedThreadId);
			++raised;
		};

		target.Bind(Class1.FooProperty, source, priority);

		ThreadRunHelper.RunOnDedicatedThreadAndWait(() => source.OnNext("foobar"));
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

		CornerstoneTest.AreEqual("foobar", target.GetValue(Class1.FooProperty));
		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void CompletingAnimationBindingRevertsToSetLocalValue()
	{
		var target = new Class1();
		var source = new Subject<BindingValue<string>>();
		var property = Class1.FooProperty;

		target.SetValue(property, "foo");
		target.Bind(property, source, BindingPriority.Animation);
		source.OnNext("bar");
		source.OnCompleted();

		CornerstoneTest.AreEqual("foo", target.GetValue(property));
	}

	[PresentationTestMethod]
	public void CompletingAnimationBindingRevertsToSetLocalValueWithStyleValue()
	{
		var target = new Class1();
		var source = new Subject<BindingValue<string>>();
		var property = Class1.FooProperty;

		target.SetValue(property, "style", BindingPriority.Style);
		target.SetValue(property, "foo");
		target.Bind(property, source, BindingPriority.Animation);
		source.OnNext("bar");
		source.OnCompleted();

		CornerstoneTest.AreEqual("foo", target.GetValue(property));
	}

	[PresentationTestMethod]
	public void CompletingLocalValueBindingRaisesPropertyChanged()
	{
		var target = new Class1();
		var source = new BehaviorSubject<BindingValue<string>>("foo");
		var property = Class1.FooProperty;
		var raised = 0;

		target.Bind(property, source);
		CornerstoneTest.AreEqual("foo", target.GetValue(property));

		target.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(BindingPriority.Unset, e.Priority);
			CornerstoneTest.AreEqual(property, e.Property);
			CornerstoneTest.AreEqual("foo", e.OldValue as string);
			CornerstoneTest.AreEqual("foodefault", e.NewValue as string);
			++raised;
		};

		source.OnCompleted();

		CornerstoneTest.AreEqual("foodefault", target.GetValue(property));
		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void CompletingLocalValueBindingRevertsToDefaultValueEvenWhenLocalValueSetEarlier()
	{
		var target = new Class1();
		var source = new Subject<BindingValue<string>>();
		var property = Class1.FooProperty;

		target.Bind(property, source);
		source.OnNext("foo");
		target.SetValue(property, "bar");
		source.OnNext("baz");
		source.OnCompleted();

		CornerstoneTest.AreEqual("foodefault", target.GetValue(property));
	}

	[PresentationTestMethod]
	public void CompletingLocalValueBindingWithStyleBindingRaisesPropertyChanged()
	{
		var target = new Class1();
		var source = new BehaviorSubject<BindingValue<string>>("foo");
		var property = Class1.FooProperty;
		var raised = 0;

		target.Bind(property, new BehaviorSubject<BindingValue<string>>("bar"), BindingPriority.Style);
		target.Bind(property, source);
		CornerstoneTest.AreEqual("foo", target.GetValue(property));

		target.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(BindingPriority.Style, e.Priority);
			CornerstoneTest.AreEqual(property, e.Property);
			CornerstoneTest.AreEqual("foo", e.OldValue as string);
			CornerstoneTest.AreEqual("bar", e.NewValue as string);
			++raised;
		};

		source.OnCompleted();

		CornerstoneTest.AreEqual("bar", target.GetValue(property));
		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void CompletingSecondLocalValueBindingDoesntRevertToFirst()
	{
		var property = Class1.FooProperty;
		var target = new Class1();
		var source1 = new Subject<BindingValue<string>>();
		var source2 = new Subject<BindingValue<string>>();

		target.Bind(property, source1, BindingPriority.LocalValue);
		target.Bind(property, source2, BindingPriority.LocalValue);

		source1.OnNext("foo");
		source2.OnNext("bar");
		source1.OnNext("baz");
		source2.OnCompleted();

		CornerstoneTest.AreEqual("foodefault", target.GetValue(property));
	}

	[PresentationTestMethod]
	public void CompletingStyleBindingRaisesPropertyChanged()
	{
		var target = new Class1();
		var source = new BehaviorSubject<BindingValue<string>>("foo");
		var property = Class1.FooProperty;
		var raised = 0;

		target.Bind(property, source, BindingPriority.Style);
		CornerstoneTest.AreEqual("foo", target.GetValue(property));

		target.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(BindingPriority.Unset, e.Priority);
			CornerstoneTest.AreEqual(property, e.Property);
			CornerstoneTest.AreEqual("foo", e.OldValue as string);
			CornerstoneTest.AreEqual("foodefault", e.NewValue as string);
			++raised;
		};

		source.OnCompleted();

		CornerstoneTest.AreEqual("foodefault", target.GetValue(property));
		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void CompletingStyleTriggerBindingRevertsToStyleBinding()
	{
		var property = Class1.FooProperty;
		var target = new Class1();
		var source1 = new Subject<BindingValue<string>>();
		var source2 = new Subject<BindingValue<string>>();

		target.Bind(property, source1, BindingPriority.Style);
		target.Bind(property, source2, BindingPriority.StyleTrigger);

		source1.OnNext("foo");
		source2.OnNext("bar");
		source2.OnCompleted();
		source1.OnNext("baz");

		CornerstoneTest.AreEqual("baz", target.GetValue(property));
	}

	[PresentationTestMethod]
	public void DataValidationErrorDoesNotCauseTargetUpdate()
	{
		var target = new Class1();
		var source = new Subject<BindingValue<string>>();

		target.Bind(Class1.FooProperty, source);
		source.OnNext("initial");
		source.OnNext(BindingValue<string>.DataValidationError(new InvalidOperationException("Foo")));

		CornerstoneTest.AreEqual("initial", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void DataValidationErrorWithFallbackValueCausesTargetUpdate()
	{
		var target = new Class1();
		var source = new Subject<BindingValue<string>>();

		target.Bind(Class1.FooProperty, source);
		source.OnNext("initial");
		source.OnNext(BindingValue<string>.DataValidationError(new InvalidOperationException("Foo"), "bar"));

		CornerstoneTest.AreEqual("bar", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void DisposingCompletedBindingDoesNotThrow()
	{
		var target = new Class1();
		var source = new Subject<BindingValue<string>>();
		var subscription = target.Bind(Class1.FooProperty, source);

		source.OnCompleted();

		subscription.Dispose();
	}

	[PresentationTestMethod]
	public void DisposingLocalValueBindingRaisesPropertyChanged()
	{
		var target = new Class1();
		var source = new BehaviorSubject<BindingValue<string>>("foo");
		var property = Class1.FooProperty;
		var raised = 0;

		var sub = target.Bind(property, source);
		CornerstoneTest.AreEqual("foo", target.GetValue(property));

		target.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(BindingPriority.Unset, e.Priority);
			CornerstoneTest.AreEqual(property, e.Property);
			CornerstoneTest.AreEqual("foo", e.OldValue as string);
			CornerstoneTest.AreEqual("foodefault", e.NewValue as string);
			++raised;
		};

		sub.Dispose();

		CornerstoneTest.AreEqual("foodefault", target.GetValue(property));
		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void DisposingLocalValueBindingShouldNotRevertToSetLocalValue()
	{
		var target = new Class1();
		var source = new BehaviorSubject<BindingValue<string>>("bar");

		target.SetValue(Class1.FooProperty, "foo");
		var sub = target.Bind(Class1.FooProperty, source);

		CornerstoneTest.AreEqual("bar", target.GetValue(Class1.FooProperty));

		sub.Dispose();

		CornerstoneTest.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void IsAnimatingOnPropertyWithAnimationBindingReturnsTrue()
	{
		var target = new Class1();
		var source = new BehaviorSubject<string>("foo");

		target.Bind(Class1.FooProperty, source, BindingPriority.Animation);

		CornerstoneTest.IsTrue(target.IsAnimating(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void IsAnimatingOnPropertyWithAnimationValueReturnsTrue()
	{
		var target = new Class1();
		var source = new BehaviorSubject<BindingValue<string>>("foo");

		target.Bind(Class1.FooProperty, source, BindingPriority.Animation);

		CornerstoneTest.IsTrue(target.IsAnimating(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void IsAnimatingOnPropertyWithLocalValueAndAnimationBindingReturnsTrue()
	{
		var target = new Class1();
		var source = new BehaviorSubject<string>("foo");

		target.SetValue(Class1.FooProperty, "bar");
		target.Bind(Class1.FooProperty, source, BindingPriority.Animation);

		CornerstoneTest.IsTrue(target.IsAnimating(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void IsAnimatingOnPropertyWithNoValueReturnsFalse()
	{
		var target = new Class1();

		CornerstoneTest.IsFalse(target.IsAnimating(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void IsAnimatingOnPropertyWithNonAnimationBindingReturnsFalse()
	{
		var target = new Class1();
		var source = new Subject<string>();

		target.Bind(Class1.FooProperty, source, BindingPriority.LocalValue);

		CornerstoneTest.IsFalse(target.IsAnimating(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void IsAnimatingReturnsTrueWhenAnimatedValueIsSameAsLocalValue()
	{
		var target = new Class1();
		var source = new BehaviorSubject<string>("foo");

		target.SetValue(Class1.FooProperty, "foo");
		target.Bind(Class1.FooProperty, source, BindingPriority.Animation);

		CornerstoneTest.IsTrue(target.IsAnimating(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void LocalBindingOverwritesLocalValue()
	{
		var target = new Class1();
		var binding = new Subject<BindingValue<string>>();

		target.Bind(Class1.FooProperty, binding);

		binding.OnNext("first");
		CornerstoneTest.AreEqual("first", target.GetValue(Class1.FooProperty));

		target.SetValue(Class1.FooProperty, "second");
		CornerstoneTest.AreEqual("second", target.GetValue(Class1.FooProperty));

		binding.OnNext("third");
		CornerstoneTest.AreEqual("third", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void LocalValueBindGenericToValueTypeAcceptsUnsetValue()
	{
		var target = new Class1();
		var source = new Subject<BindingValue<double>>();

		target.Bind(Class1.QuxProperty, source);
		source.OnNext(6.7);
		source.OnNext(BindingValue<double>.Unset);

		CornerstoneTest.AreEqual(5.6, target.GetValue(Class1.QuxProperty));
		CornerstoneTest.IsTrue(target.IsSet(Class1.QuxProperty));
	}

	[PresentationTestMethod]
	public void LocalValueBindNonGenericToValueTypeAcceptsDoNothing()
	{
		var target = new Class1();
		var source = new Subject<object>();

		target.Bind(Class1.QuxProperty, source);
		source.OnNext(6.7);
		source.OnNext(BindingOperations.DoNothing);

		CornerstoneTest.AreEqual(6.7, target.GetValue(Class1.QuxProperty));
	}

	[PresentationTestMethod]
	public void LocalValueBindNonGenericToValueTypeAcceptsUnsetValue()
	{
		var target = new Class1();
		var source = new Subject<object>();

		target.Bind(Class1.QuxProperty, source);
		source.OnNext(6.7);
		source.OnNext(PresentationProperty.UnsetValue);

		CornerstoneTest.AreEqual(5.6, target.GetValue(Class1.QuxProperty));
		CornerstoneTest.IsTrue(target.IsSet(Class1.QuxProperty));
	}

	[PresentationTestMethod]
	public void LocalValueBindingIsNotUnsubscribedWhenLocalValueIsSet()
	{
		var source = new TestSubject<BindingValue<string>>("foo");
		var target = new Class1();

		target.Bind(Class1.FooProperty, source);
		CornerstoneTest.AreEqual(1, source.SubscriberCount);

		target.SetValue(Class1.FooProperty, "foo");
		CornerstoneTest.AreEqual(1, source.SubscriberCount);
	}

	[PresentationTestMethod]
	public void LocalValueBindingShouldOverrideStyleBinding()
	{
		var target = new Class1();
		var source1 = new BehaviorSubject<BindingValue<string>>("foo");
		var source2 = new BehaviorSubject<BindingValue<string>>("bar");

		target.Bind(Class1.FooProperty, source1, BindingPriority.Style);

		CornerstoneTest.AreEqual("foo", target.GetValue(Class1.FooProperty));

		target.Bind(Class1.FooProperty, source2, BindingPriority.LocalValue);

		CornerstoneTest.AreEqual("bar", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	[DataRow(BindingPriority.LocalValue)]
	[DataRow(BindingPriority.Style)]
	public void ObservableIsNotUnsubscribedWhenAnimationBindingIsAdded(BindingPriority priority)
	{
		var source1 = new TestSubject<BindingValue<string>>("foo");
		var source2 = new TestSubject<BindingValue<string>>("bar");
		var target = new Class1();

		target.Bind(Class1.FooProperty, source1, priority);
		CornerstoneTest.AreEqual(1, source1.SubscriberCount);

		target.Bind(Class1.FooProperty, source2, BindingPriority.Animation);
		CornerstoneTest.AreEqual(1, source1.SubscriberCount);
		CornerstoneTest.AreEqual(1, source2.SubscriberCount);
	}

	[PresentationTestMethod]
	[DataRow(BindingPriority.LocalValue)]
	[DataRow(BindingPriority.Style)]
	public void ObservableIsNotUnsubscribedWhenAnimationValueIsSet(BindingPriority priority)
	{
		var source = new TestSubject<BindingValue<string>>("foo");
		var target = new Class1();

		target.Bind(Class1.FooProperty, source, priority);
		CornerstoneTest.AreEqual(1, source.SubscriberCount);

		target.SetValue(Class1.FooProperty, "bar", BindingPriority.Animation);
		CornerstoneTest.AreEqual(1, source.SubscriberCount);
	}

	[PresentationTestMethod]
	[DataRow(BindingPriority.Style)]
	public void ObservableIsUnsubscribedWhenNewBindingOfHigherPriorityIsAdded(BindingPriority priority)
	{
		var source1 = new TestSubject<BindingValue<string>>("foo");
		var source2 = new TestSubject<BindingValue<string>>("bar");
		var target = new Class1();

		target.Bind(Class1.FooProperty, source1, priority);
		CornerstoneTest.AreEqual(1, source1.SubscriberCount);

		target.Bind(Class1.FooProperty, source2, priority - 1);
		CornerstoneTest.AreEqual(1, source2.SubscriberCount);
		CornerstoneTest.AreEqual(0, source1.SubscriberCount);
	}

	[PresentationTestMethod]
	[DataRow(BindingPriority.LocalValue)]
	[DataRow(BindingPriority.Style)]
	[DataRow(BindingPriority.Animation)]
	public void ObservableIsUnsubscribedWhenNewBindingOfSamePriorityIsAdded(BindingPriority priority)
	{
		var source1 = new TestSubject<BindingValue<string>>("foo");
		var source2 = new TestSubject<BindingValue<string>>("bar");
		var target = new Class1();

		target.Bind(Class1.FooProperty, source1, priority);
		CornerstoneTest.AreEqual(1, source1.SubscriberCount);

		target.Bind(Class1.FooProperty, source2, priority);
		CornerstoneTest.AreEqual(1, source2.SubscriberCount);
		CornerstoneTest.AreEqual(0, source1.SubscriberCount);
	}

	[PresentationTestMethod]
	[DataRow(BindingPriority.Style)]
	public void ObservableIsUnsubscribedWhenNewValueOfHigherPriorityIsAdded(BindingPriority priority)
	{
		var source = new TestSubject<BindingValue<string>>("foo");
		var target = new Class1();

		target.Bind(Class1.FooProperty, source, priority);
		CornerstoneTest.AreEqual(1, source.SubscriberCount);

		target.SetValue(Class1.FooProperty, "foo", priority - 1);
		CornerstoneTest.AreEqual(0, source.SubscriberCount);
	}

	[PresentationTestMethod]
	[DataRow(BindingPriority.Style)]
	[DataRow(BindingPriority.Animation)]
	public void ObservableIsUnsubscribedWhenNewValueOfSamePriorityIsAdded(BindingPriority priority)
	{
		var source = new TestSubject<BindingValue<string>>("foo");
		var target = new Class1();

		target.Bind(Class1.FooProperty, source, priority);
		CornerstoneTest.AreEqual(1, source.SubscriberCount);

		target.SetValue(Class1.FooProperty, "foo", priority);
		CornerstoneTest.AreEqual(0, source.SubscriberCount);
	}

	[PresentationTestMethod]
	public void ObservableIsUnsubscribedWhenSubscriptionDisposed()
	{
		var source = new TestSubject<BindingValue<string>>("foo");
		var target = new Class1();

		var subscription = target.Bind(Class1.FooProperty, source);
		CornerstoneTest.AreEqual(1, source.SubscriberCount);

		subscription.Dispose();
		CornerstoneTest.AreEqual(0, source.SubscriberCount);
	}

	[PresentationTestMethod]
	public void OneTimeBindingIgnoresBindingErrors()
	{
		var target = new Class1();
		var source = new Subject<object>();

		target.Bind(Class1.QuxProperty, source);

		source.OnNext(new BindingNotification(new Exception(), BindingErrorType.Error));
		CornerstoneTest.AreEqual(5.6, target.GetValue(Class1.QuxProperty));

		source.OnNext(6.7);
		CornerstoneTest.AreEqual(6.7, target.GetValue(Class1.QuxProperty));
	}

	[PresentationTestMethod]
	public void OneTimeBindingIgnoresUnsetValue()
	{
		var target = new Class1();
		var source = new Subject<object>();

		target.Bind(Class1.QuxProperty, source);

		source.OnNext(PresentationProperty.UnsetValue);
		CornerstoneTest.AreEqual(5.6, target.GetValue(Class1.QuxProperty));

		source.OnNext(6.7);
		CornerstoneTest.AreEqual(6.7, target.GetValue(Class1.QuxProperty));
	}

	[PresentationTestMethod]
	public void ProducesCorrectValuesAndBaseValuesWithMultipleAnimationBindings()
	{
		var target = new Class1();
		var source1 = new BehaviorSubject<BindingValue<double>>(12.2);
		var source2 = new BehaviorSubject<BindingValue<double>>(13.3);

		target.SetValue(Class1.QuxProperty, 11.1);
		target.Bind(Class1.QuxProperty, source1, BindingPriority.Animation);

		CornerstoneTest.AreEqual(12.2, target.GetValue(Class1.QuxProperty));
		CornerstoneTest.AreEqual(11.1, target.GetBaseValue(Class1.QuxProperty));

		target.Bind(Class1.QuxProperty, source2, BindingPriority.Animation);

		CornerstoneTest.AreEqual(13.3, target.GetValue(Class1.QuxProperty));
		CornerstoneTest.AreEqual(11.1, target.GetBaseValue(Class1.QuxProperty));

		source2.OnCompleted();

		CornerstoneTest.AreEqual(12.2, target.GetValue(Class1.QuxProperty));
		CornerstoneTest.AreEqual(11.1, target.GetBaseValue(Class1.QuxProperty));

		source1.OnCompleted();

		CornerstoneTest.AreEqual(11.1, target.GetValue(Class1.QuxProperty));
		CornerstoneTest.AreEqual(11.1, target.GetBaseValue(Class1.QuxProperty));
	}

	[PresentationTestMethod]
	public void PropertyChangedNotRaisedWhenValueUnchanged()
	{
		var target = new Class1();
		var source = new Subject<BindingValue<string>>();
		var raised = 0;

		target.PropertyChanged += (s, e) => ++raised;
		target.Bind(Class1.FooProperty, source);
		source.OnNext("newvalue");
		source.OnNext("newvalue");

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void SecondLocalValueBindingUnsubscribesFirst()
	{
		var property = Class1.FooProperty;
		var target = new Class1();
		var source1 = new Subject<BindingValue<string>>();
		var source2 = new Subject<BindingValue<string>>();

		target.Bind(property, source1, BindingPriority.LocalValue);
		target.Bind(property, source2, BindingPriority.LocalValue);

		source1.OnNext("foo");
		CornerstoneTest.AreEqual("foodefault", target.GetValue(property));

		source2.OnNext("bar");
		CornerstoneTest.AreEqual("bar", target.GetValue(property));

		source1.OnNext("baz");
		CornerstoneTest.AreEqual("bar", target.GetValue(property));
	}

	[PresentationTestMethod]
	public void SetValueShouldNotCauseStackOverflowAndHaveCorrectValues()
	{
		var viewModel = new TestStackOverflowViewModel
		{
			Value = 50
		};

		var target = new Class1();

		target.Bind(Class1.DoubleValueProperty,
			new Binding("Value") { Mode = BindingMode.TwoWay, Source = viewModel });

		var child = new Class1();

		child[!!Class1.DoubleValueProperty] = target[!!Class1.DoubleValueProperty];

		CornerstoneTest.AreEqual(1, viewModel.SetterInvokedCount);

		// Issues #855 and #824 were causing a StackOverflowException at this point.
		target.DoubleValue = 51.001;

		CornerstoneTest.AreEqual(2, viewModel.SetterInvokedCount);

		double expected = 51;

		CornerstoneTest.AreEqual(expected, viewModel.Value);
		CornerstoneTest.AreEqual(expected, target.DoubleValue);
		CornerstoneTest.AreEqual(expected, child.DoubleValue);
	}

	[PresentationTestMethod]
	public void SettingLocalValueOverridesBindingUntilBindingProducesNextValue()
	{
		var target = new Class1();
		var source = new Subject<BindingValue<string>>();
		var property = Class1.FooProperty;

		target.Bind(property, source);
		source.OnNext("foo");
		CornerstoneTest.AreEqual("foo", target.GetValue(property));

		target.SetValue(property, "bar");
		CornerstoneTest.AreEqual("bar", target.GetValue(property));

		source.OnNext("baz");
		CornerstoneTest.AreEqual("baz", target.GetValue(property));
	}

	[PresentationTestMethod]
	public void SettingStyleValueOverridesBindingPermanently()
	{
		var target = new Class1();
		var source = new Subject<string>();

		target.Bind(Class1.FooProperty, source, BindingPriority.Style);
		source.OnNext("foo");
		CornerstoneTest.AreEqual("foo", target.GetValue(Class1.FooProperty));

		target.SetValue(Class1.FooProperty, "bar", BindingPriority.Style);
		CornerstoneTest.AreEqual("bar", target.GetValue(Class1.FooProperty));

		source.OnNext("baz");
		CornerstoneTest.AreEqual("bar", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void StyleBindNonGenericToValueTypeAcceptsDoNothing()
	{
		var target = new Class1();
		var source = new Subject<object>();

		target.Bind(Class1.QuxProperty, source, BindingPriority.Style);
		source.OnNext(6.7);
		source.OnNext(BindingOperations.DoNothing);

		CornerstoneTest.AreEqual(6.7, target.GetValue(Class1.QuxProperty));
	}

	[PresentationTestMethod]
	public void StyleBindNonGenericToValueTypeAcceptsUnsetValue()
	{
		var target = new Class1();
		var source = new Subject<object>();

		target.Bind(Class1.QuxProperty, source, BindingPriority.Style);
		source.OnNext(6.7);
		source.OnNext(PresentationProperty.UnsetValue);

		CornerstoneTest.AreEqual(5.6, target.GetValue(Class1.QuxProperty));
		CornerstoneTest.IsTrue(target.IsSet(Class1.QuxProperty));
	}

	[PresentationTestMethod]
	public void StyleBindingOverridesDefaultValue()
	{
		var target = new Class1();

		target.Bind(Class1.FooProperty, Single("stylevalue"), BindingPriority.Style);

		CornerstoneTest.AreEqual("stylevalue", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void StyleBindingShouldNotOverrideLocalValueBinding()
	{
		var target = new Class1();
		var source1 = new BehaviorSubject<BindingValue<string>>("foo");
		var source2 = new BehaviorSubject<BindingValue<string>>("bar");

		target.Bind(Class1.FooProperty, source1, BindingPriority.LocalValue);

		CornerstoneTest.AreEqual("foo", target.GetValue(Class1.FooProperty));

		target.Bind(Class1.FooProperty, source2, BindingPriority.Style);

		CornerstoneTest.AreEqual("foo", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void TwoWayBindingShouldNotCallSetterOnCreation()
	{
		var target = new Class1();
		var source = new TestTwoWayBindingViewModel();

		target.Bind(Class1.DoubleValueProperty, new Binding(nameof(source.Value))
		{
			Mode = BindingMode.TwoWay,
			Source = source
		});

		CornerstoneTest.IsFalse(source.SetterCalled);
	}

	[PresentationTestMethod]
	public void TwoWayBindingShouldNotCallSetterOnCreationIndexer()
	{
		var target = new Class1();
		var source = new TestTwoWayBindingViewModel();

		target.Bind(Class1.DoubleValueProperty, new Binding("[0]")
		{
			Mode = BindingMode.TwoWay,
			Source = source
		});

		CornerstoneTest.IsFalse(source.SetterCalled);
	}

	[PresentationTestMethod]
	public void TwoWayBindingShouldNotFailWithNullDataContext()
	{
		var target = new TextBlock();
		target.DataContext = null;

		target.Bind(TextBlock.TextProperty, new Binding("Missing") { Mode = BindingMode.TwoWay });
	}

	[PresentationTestMethod]
	public void TwoWayBindingShouldNotFailWithNullDataContextIndexer()
	{
		var target = new TextBlock();
		target.DataContext = null;

		target.Bind(TextBlock.TextProperty, new Binding("[0]") { Mode = BindingMode.TwoWay });
	}

	[PresentationTestMethod]
	public void TwoWayBindingShouldNotUpdateSourceWhenHigherPriorityBindingAdded()
	{
		var target = new Class1();
		var source = new TestTwoWayBindingViewModel();
		var binding1 = new Binding(nameof(source.Value))
		{
			Mode = BindingMode.TwoWay,
			Source = source
		};
		var binding2 = new BehaviorSubject<double>(123.4);

		target.Bind(Class1.DoubleValueProperty, binding1, BindingPriority.LocalValue);
		target.Bind(Class1.DoubleValueProperty, binding2, BindingPriority.Animation);

		// Animation is not a user edit of the two-way target, so the source should stay unchanged.
		CornerstoneTest.IsFalse(source.SetterCalled);
	}

	[PresentationTestMethod]
	public void TwoWayBindingShouldNotUpdateSourceWhenHigherPriorityValueSet()
	{
		var target = new Class1();
		var source = new TestTwoWayBindingViewModel();
		var binding = new Binding(nameof(source.Value))
		{
			Mode = BindingMode.TwoWay,
			Source = source
		};

		target.Bind(Class1.DoubleValueProperty, binding, BindingPriority.LocalValue);
		target.SetValue(Class1.DoubleValueProperty, 123.4, BindingPriority.Animation);

		// Animation is not a user edit of the two-way target, so the source should stay unchanged.
		CornerstoneTest.IsFalse(source.SetterCalled);
	}

	[PresentationTestMethod]
	public void TwoWayBindingShouldUpdateSource()
	{
		var target = new Class1();
		var source = new TestTwoWayBindingViewModel();

		target.Bind(Class1.DoubleValueProperty, new Binding(nameof(source.Value))
		{
			Mode = BindingMode.TwoWay,
			Source = source
		});

		target.DoubleValue = 123.4;

		CornerstoneTest.IsTrue(source.SetterCalled);
		CornerstoneTest.AreEqual(source.Value, 123.4);
	}

	[PresentationTestMethod]
	public void TwoWayBindingWithPriorityWorks()
	{
		var obj1 = new Class1();
		var obj2 = new Class1();

		obj1.SetValue(Class1.FooProperty, "initial1", BindingPriority.Style);
		obj2.SetValue(Class1.FooProperty, "initial2", BindingPriority.Style);

		obj1.Bind(Class1.FooProperty, obj2.GetObservable(Class1.FooProperty), BindingPriority.Style);
		obj2.Bind(Class1.FooProperty, obj1.GetObservable(Class1.FooProperty), BindingPriority.Style);

		CornerstoneTest.AreEqual("initial2", obj1.GetValue(Class1.FooProperty));
		CornerstoneTest.AreEqual("initial2", obj2.GetValue(Class1.FooProperty));

		obj1.SetValue(Class1.FooProperty, "first", BindingPriority.Style);

		CornerstoneTest.AreEqual("first", obj1.GetValue(Class1.FooProperty));
		CornerstoneTest.AreEqual("first", obj2.GetValue(Class1.FooProperty));

		obj2.SetValue(Class1.FooProperty, "second", BindingPriority.Style);

		CornerstoneTest.AreEqual("first", obj1.GetValue(Class1.FooProperty));
		CornerstoneTest.AreEqual("second", obj2.GetValue(Class1.FooProperty));

		obj1.SetValue(Class1.FooProperty, "third", BindingPriority.Style);

		CornerstoneTest.AreEqual("third", obj1.GetValue(Class1.FooProperty));
		CornerstoneTest.AreEqual("second", obj2.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void TwoWaySeparateBindingWorks()
	{
		var obj1 = new Class1();
		var obj2 = new Class1();

		obj1.SetValue(Class1.FooProperty, "initial1");
		obj2.SetValue(Class1.FooProperty, "initial2");

		obj1.Bind(Class1.FooProperty, obj2.GetObservable(Class1.FooProperty));
		obj2.Bind(Class1.FooProperty, obj1.GetObservable(Class1.FooProperty));

		CornerstoneTest.AreEqual("initial2", obj1.GetValue(Class1.FooProperty));
		CornerstoneTest.AreEqual("initial2", obj2.GetValue(Class1.FooProperty));

		obj1.SetValue(Class1.FooProperty, "first");

		CornerstoneTest.AreEqual("first", obj1.GetValue(Class1.FooProperty));
		CornerstoneTest.AreEqual("first", obj2.GetValue(Class1.FooProperty));

		obj2.SetValue(Class1.FooProperty, "second");

		CornerstoneTest.AreEqual("second", obj1.GetValue(Class1.FooProperty));
		CornerstoneTest.AreEqual("second", obj2.GetValue(Class1.FooProperty));

		obj1.SetValue(Class1.FooProperty, "third");

		CornerstoneTest.AreEqual("third", obj1.GetValue(Class1.FooProperty));
		CornerstoneTest.AreEqual("third", obj2.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void TwoWayStyleBindingShouldNotUpdateSourceWhenAnimatedBindingAdded()
	{
		var target = new Class1();
		var source1 = new TestTwoWayBindingViewModel();
		var source2 = new BehaviorSubject<double>(123.4);

		target.Bind(Class1.DoubleValueProperty, new Binding(nameof(source1.Value))
		{
			Mode = BindingMode.TwoWay,
			Source = source1
		});
		target.Bind(Class1.DoubleValueProperty, source2, BindingPriority.Animation);

		// Setter should not be called because the TwoWay binding with Style priority
		// should be overridden by the animated binding and the binding made inactive.
		CornerstoneTest.IsFalse(source1.SetterCalled);
	}

	[PresentationTestMethod]
	public void TwoWayStyleBindingShouldNotUpdateSourceWhenStyleTriggerValueSet()
	{
		var target = new Class1();
		var source = new TestTwoWayBindingViewModel();

		target.Bind(Class1.DoubleValueProperty, new Binding(nameof(source.Value))
		{
			Mode = BindingMode.TwoWay,
			Source = source
		});
		target.SetValue(Class1.DoubleValueProperty, 123.4, BindingPriority.Animation);

		// Setter should not be called because the TwoWay binding with Style priority
		// should be overridden by the animated value and the binding made inactive.
		CornerstoneTest.IsFalse(source.SetterCalled);
	}

	[PresentationTestMethod]
	[DataRow(BindingPriority.LocalValue)]
	[DataRow(BindingPriority.Style)]
	public void TypedBindExecutesOnUIThread(BindingPriority priority)
	{
		using (UnitTestApplication.Start())
		{
			var target = new Class1();
			var source = new Subject<string>();
			var currentThreadId = Thread.CurrentThread.ManagedThreadId;
			var raised = 0;

			target.PropertyChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(currentThreadId, Thread.CurrentThread.ManagedThreadId);
				++raised;
			};

			target.Bind(Class1.FooProperty, source, priority);

			ThreadRunHelper.RunOnDedicatedThreadAndWait(() => source.OnNext("foobar"));
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual("foobar", target.GetValue(Class1.FooProperty));
			CornerstoneTest.AreEqual(1, raised);
		}
	}

	[PresentationTestMethod]
	[DataRow(BindingPriority.LocalValue)]
	[DataRow(BindingPriority.Style)]
	public void UntypedBindExecutesOnUIThread(BindingPriority priority)
	{
		using (UnitTestApplication.Start())
		{
			var target = new Class1();
			var source = new Subject<object>();
			var currentThreadId = Thread.CurrentThread.ManagedThreadId;
			var raised = 0;

			target.PropertyChanged += (s, e) =>
			{
				CornerstoneTest.AreEqual(currentThreadId, Thread.CurrentThread.ManagedThreadId);
				++raised;
			};

			target.Bind(Class1.FooProperty, source, priority);

			ThreadRunHelper.RunOnDedicatedThreadAndWait(() => source.OnNext("foobar"));
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual("foobar", target.GetValue(Class1.FooProperty));
			CornerstoneTest.AreEqual(1, raised);
		}
	}

	[PresentationTestMethod]
	public void thisOperatorBindsOneTime()
	{
		var target1 = new Class1();
		var target2 = new Class1();

		target1.SetValue(Class1.FooProperty, "first");
		target2[!Class1.FooProperty] = target1[Class1.FooProperty.Bind().WithMode(BindingMode.OneTime)];
		target1.SetValue(Class1.FooProperty, "second");

		CornerstoneTest.AreEqual("first", target2.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void thisOperatorBindsOneWay()
	{
		var target1 = new Class1();
		var target2 = new Class2();
		var binding = Class2.BarProperty.Bind().WithMode(BindingMode.OneWay);

		target1.SetValue(Class1.FooProperty, "first");
		target2[binding] = target1[!Class1.FooProperty];
		target1.SetValue(Class1.FooProperty, "second");

		CornerstoneTest.AreEqual("second", target2.GetValue(Class2.BarProperty));
	}

	[PresentationTestMethod]
	public void thisOperatorBindsTwoWay()
	{
		var target1 = new Class1();
		var target2 = new Class1();

		target1.SetValue(Class1.FooProperty, "first");
		target2[!Class1.FooProperty] = target1[!!Class1.FooProperty];
		CornerstoneTest.AreEqual("first", target2.GetValue(Class1.FooProperty));
		target1.SetValue(Class1.FooProperty, "second");
		CornerstoneTest.AreEqual("second", target2.GetValue(Class1.FooProperty));
		target2.SetValue(Class1.FooProperty, "third");
		CornerstoneTest.AreEqual("third", target1.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void thisOperatorDoesntAcceptObservable()
	{
		var target = new Class1();

		Assert.Throws<ArgumentException>(() => { target[Class1.FooProperty] = Observable.Return("newvalue"); });
	}

	[PresentationTestMethod]
	public void thisOperatorReturnsValueProperty()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "newvalue");

		CornerstoneTest.AreEqual("newvalue", target[Class1.FooProperty]);
	}

	[PresentationTestMethod]
	public void thisOperatorSetsValueProperty()
	{
		var target = new Class1();

		target[Class1.FooProperty] = "newvalue";

		CornerstoneTest.AreEqual("newvalue", target.GetValue(Class1.FooProperty));
	}

	/// <summary>
	/// Returns an observable that returns a single value but does not complete.
	/// </summary>
	/// <typeparam name="T"> The type of the observable. </typeparam>
	/// <param name="value"> The value. </param>
	/// <returns> The observable. </returns>
	private IObservable<BindingValue<T>> Single<T>(T value)
	{
		return Observable.Never<BindingValue<T>>().StartWith(value);
	}

	#endregion

	#region Classes

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly StyledProperty<double> DoubleValueProperty =
			PresentationProperty.Register<Class1, double>(nameof(DoubleValue));

		public static readonly StyledProperty<string> FooProperty =
			PresentationProperty.Register<Class1, string>("Foo", "foodefault");

		public static readonly StyledProperty<double> QuxProperty =
			PresentationProperty.Register<Class1, double>("Qux", 5.6);

		#endregion

		#region Properties

		public double DoubleValue
		{
			get => GetValue(DoubleValueProperty);
			set => SetValue(DoubleValueProperty, value);
		}

		#endregion
	}

	private class Class2 : Class1
	{
		#region Fields

		public static readonly StyledProperty<string> BarProperty =
			PresentationProperty.Register<Class2, string>("Bar", "bardefault");

		#endregion
	}

	private class TestStackOverflowViewModel : INotifyPropertyChanged
	{
		#region Constants

		public const int MaxInvokedCount = 1000;

		#endregion

		#region Fields

		private double _value;

		#endregion

		#region Properties

		public int SetterInvokedCount { get; private set; }

		public double Value
		{
			get => _value;
			set
			{
				if (_value != value)
				{
					SetterInvokedCount++;
					if (SetterInvokedCount < MaxInvokedCount)
					{
						_value = (int) value;
						if (_value > 75)
						{
							_value = 75;
						}
						if (_value < 25)
						{
							_value = 25;
						}
					}
					else
					{
						_value = value;
					}

					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
				}
			}
		}

		#endregion

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	private class TestTwoWayBindingViewModel
	{
		#region Fields

		private double _value;

		#endregion

		#region Properties

		public double this[int index]
		{
			get => _value;
			set
			{
				_value = value;
				SetterCalled = true;
			}
		}

		public bool SetterCalled { get; private set; }

		public double Value
		{
			get => _value;
			set
			{
				_value = value;
				SetterCalled = true;
			}
		}

		#endregion
	}

	#endregion
}