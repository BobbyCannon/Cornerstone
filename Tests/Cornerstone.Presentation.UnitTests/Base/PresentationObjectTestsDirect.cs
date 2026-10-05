#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Subjects;
using System.Threading;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class PresentationObjectTestsDirect
{
	#region Methods

	[PresentationTestMethod]
	public void AddOwnerCanOverrideDefaultBindingMode()
	{
		var foo = new DirectProperty<Class1, string>(
			"foo",
			o => "foo",
			null,
			new DirectPropertyMetadata<string>(defaultBindingMode: BindingMode.TwoWay));
		var bar = foo.AddOwner<Class2>(o => "bar", defaultBindingMode: BindingMode.OneWayToSource);

		CornerstoneTest.AreEqual(BindingMode.TwoWay, bar.GetMetadata<Class1>().DefaultBindingMode);
		CornerstoneTest.AreEqual(BindingMode.OneWayToSource, bar.GetMetadata<Class2>().DefaultBindingMode);
	}

	[PresentationTestMethod]
	public void AddOwnerShouldInheritDefaultBindingMode()
	{
		var foo = new DirectProperty<Class1, string>(
			"foo",
			o => "foo",
			null,
			new DirectPropertyMetadata<string>(defaultBindingMode: BindingMode.TwoWay));
		var bar = foo.AddOwner<Class2>(o => "bar");

		CornerstoneTest.AreEqual(BindingMode.TwoWay, bar.GetMetadata<Class1>().DefaultBindingMode);
		CornerstoneTest.AreEqual(BindingMode.TwoWay, bar.GetMetadata<Class2>().DefaultBindingMode);
	}

	[PresentationTestMethod]
	public void BindBindsAddOwneredPropertyValue()
	{
		var target = new Class2();
		var source = new Subject<string>();

		var sub = target.Bind(Class1.FooProperty, source);

		CornerstoneTest.AreEqual("initial2", target.Foo);
		source.OnNext("first");
		CornerstoneTest.AreEqual("first", target.Foo);
		source.OnNext("second");
		CornerstoneTest.AreEqual("second", target.Foo);

		sub.Dispose();

		source.OnNext("third");
		CornerstoneTest.AreEqual("second", target.Foo);
	}

	[PresentationTestMethod]
	public void BindBindsAddOwneredPropertyValueNonGeneric()
	{
		var target = new Class2();
		var source = new Subject<string>();

		var sub = target.Bind((PresentationProperty) Class1.FooProperty, source);

		CornerstoneTest.AreEqual("initial2", target.Foo);
		source.OnNext("first");
		CornerstoneTest.AreEqual("first", target.Foo);
		source.OnNext("second");
		CornerstoneTest.AreEqual("second", target.Foo);

		sub.Dispose();

		source.OnNext("third");
		CornerstoneTest.AreEqual("second", target.Foo);
	}

	[PresentationTestMethod]
	public void BindBindsPropertyValue()
	{
		var target = new Class1();
		var source = new Subject<string>();

		var sub = target.Bind(Class1.FooProperty, source);

		CornerstoneTest.AreEqual("initial", target.Foo);
		source.OnNext("first");
		CornerstoneTest.AreEqual("first", target.Foo);
		source.OnNext("second");
		CornerstoneTest.AreEqual("second", target.Foo);

		sub.Dispose();

		source.OnNext("third");
		CornerstoneTest.AreEqual("second", target.Foo);
	}

	[PresentationTestMethod]
	public void BindBindsPropertyValueNonGeneric()
	{
		var target = new Class1();
		var source = new Subject<string>();

		var sub = target.Bind((PresentationProperty) Class1.FooProperty, source);

		CornerstoneTest.AreEqual("initial", target.Foo);
		source.OnNext("first");
		CornerstoneTest.AreEqual("first", target.Foo);
		source.OnNext("second");
		CornerstoneTest.AreEqual("second", target.Foo);

		sub.Dispose();

		source.OnNext("third");
		CornerstoneTest.AreEqual("second", target.Foo);
	}

	[PresentationTestMethod]
	public void BindExecutesOnUIThread()
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

			target.Bind(Class1.FooProperty, source);

			ThreadRunHelper.RunOnDedicatedThreadAndWait(() => source.OnNext("foobar"));
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual("foobar", target.Foo);
			CornerstoneTest.AreEqual(1, raised);
		}
	}

	[PresentationTestMethod]
	public void BindHandlesWrongType()
	{
		var target = new Class1();
		var source = new Subject<object>();

		var sub = target.Bind(Class1.FooProperty, source);

		source.OnNext(45);

		CornerstoneTest.AreEqual("unset", target.Foo);
	}

	[PresentationTestMethod]
	public void BindHandlesWrongValueType()
	{
		var target = new Class1();
		var source = new Subject<object>();

		var sub = target.Bind(Class1.BazProperty, source);

		source.OnNext("foo");

		CornerstoneTest.AreEqual(-1, target.Baz);
	}

	[PresentationTestMethod]
	public void BindNonGenericAcceptsUnsetValue()
	{
		var target = new Class1();
		var source = new Subject<object>();

		var sub = target.Bind((PresentationProperty) Class1.BazProperty, source);

		CornerstoneTest.AreEqual(5, target.Baz);
		source.OnNext(6);
		CornerstoneTest.AreEqual(6, target.Baz);
		source.OnNext(PresentationProperty.UnsetValue);
		CornerstoneTest.AreEqual(-1, target.Baz);
	}

	[PresentationTestMethod]
	public void BindRaisesPropertyChanged()
	{
		var target = new Class1();
		var source = new Subject<BindingValue<string>>();
		var raised = false;

		target.PropertyChanged += (s, e) =>
			raised = (e.Property == Class1.FooProperty) &&
				((string) e.OldValue == "initial") &&
				((string) e.NewValue == "newvalue") &&
				(e.Priority == BindingPriority.LocalValue);

		target.Bind(Class1.FooProperty, source);
		source.OnNext("newvalue");

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void BindingErrorRevertsToDefaultValue()
	{
		var target = new Class1();
		var source = new Subject<BindingValue<string>>();

		target.Bind(Class1.FooProperty, source);
		source.OnNext("initial");
		source.OnNext(BindingValue<string>.BindingError(new InvalidOperationException("Foo")));

		CornerstoneTest.AreEqual("unset", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void BindingErrorWithFallbackValueBarCausesTargetUpdate()
	{
		var target = new Class1();
		var source = new Subject<BindingValue<string>>();

		target.Bind(Class1.FooProperty, source);
		source.OnNext("initial");
		source.OnNext(BindingValue<string>.BindingError(new InvalidOperationException("Foo"), "bar"));

		CornerstoneTest.AreEqual("bar", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void BindingErrorWithFallbackValueCausesTargetUpdate()
	{
		var target = new Class1();
		var source = new Subject<BindingValue<string>>();

		target.Bind(Class1.FooProperty, source);
		source.OnNext("initial");
		source.OnNext(BindingValue<string>.BindingError(new InvalidOperationException("Foo"), "fallback"));

		CornerstoneTest.AreEqual("fallback", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void ClearValueRaisesPropertyChanged()
	{
		var target = new Class1();
		var raised = 0;

		target.SetValue(Class1.FooProperty, "newvalue");
		target.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.Same(target, s);
			CornerstoneTest.AreEqual(BindingPriority.LocalValue, e.Priority);
			CornerstoneTest.AreEqual(Class1.FooProperty, e.Property);
			CornerstoneTest.AreEqual("newvalue", (string) e.OldValue);
			CornerstoneTest.AreEqual("unset", (string) e.NewValue);
			++raised;
		};

		target.ClearValue(Class1.FooProperty);

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void ClearValueRestoresDefaultvalue()
	{
		var target = new Class1();

		CornerstoneTest.AreEqual("initial", target.GetValue(Class1.FooProperty));
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
	public void GetObservableReturnsValues()
	{
		var target = new Class1();
		var values = new List<string>();

		target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));
		target.Foo = "newvalue";

		CornerstoneTest.AreEqual(new[] { "initial", "newvalue" }, values);
	}

	[PresentationTestMethod]
	public void GetValueGetsDefaultValue()
	{
		var target = new Class1();

		CornerstoneTest.AreEqual("initial", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void GetValueGetsValueNonGeneric()
	{
		var target = new Class1();

		CornerstoneTest.AreEqual("initial", target.GetValue((PresentationProperty) Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void GetValueGetsValueOnAddOwneredProperty()
	{
		var target = new Class2();

		CornerstoneTest.AreEqual("initial2", target.GetValue(Class2.FooProperty));
	}

	[PresentationTestMethod]
	public void GetValueGetsValueOnAddOwneredPropertyUsingOriginal()
	{
		var target = new Class2();

		CornerstoneTest.AreEqual("initial2", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void GetValueGetsValueOnAddOwneredPropertyUsingOriginalNonGeneric()
	{
		var target = new Class2();

		CornerstoneTest.AreEqual("initial2", target.GetValue((PresentationProperty) Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void GetValueOnUnregisteredPropertyThrowsException()
	{
		var target = new Class2();

		Assert.Throws<ArgumentException>(() => target.GetValue(Class1.BarProperty));
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
	public void ReadOnlyPropertyCannotBeBound()
	{
		var target = new Class1();
		var source = new Subject<string>();

		Assert.Throws<ArgumentException>(() =>
			target.Bind(Class1.BarProperty, source));
	}

	[PresentationTestMethod]
	public void ReadOnlyPropertyCannotBeBoundNonGeneric()
	{
		var target = new Class1();
		var source = new Subject<string>();

		Assert.Throws<ArgumentException>(() =>
			target.Bind(Class1.BarProperty, source));
	}

	[PresentationTestMethod]
	public void ReadOnlyPropertyCannotBeSet()
	{
		var target = new Class1();

		Assert.Throws<ArgumentException>(() =>
			target.SetValue(Class1.BarProperty, "newvalue"));
	}

	[PresentationTestMethod]
	public void ReadOnlyPropertyCannotBeSetNonGeneric()
	{
		var target = new Class1();

		Assert.Throws<ArgumentException>(() =>
			target.SetValue((PresentationProperty) Class1.BarProperty, "newvalue"));
	}

	[PresentationTestMethod]
	public void SetValueNonGenericCoercesUnsetValueToDefaultValue()
	{
		var target = new Class1();

		target.SetValue(Class1.BazProperty, PresentationProperty.UnsetValue);

		CornerstoneTest.AreEqual(-1, target.Baz);
	}

	[PresentationTestMethod]
	public void SetValueOnUnregisteredPropertyThrowsException()
	{
		var target = new Class2();

		Assert.Throws<ArgumentException>(() => target.SetValue(Class1.BarProperty, "value"));
	}

	[PresentationTestMethod]
	public void SetValueRaisesChanged()
	{
		var target = new Class1();
		var raised = false;

		Class1.FooProperty.Changed.Subscribe(e =>
			raised = (e.Property == Class1.FooProperty) &&
				(e.OldValue.GetValueOrDefault() == "initial") &&
				(e.NewValue.GetValueOrDefault() == "newvalue") &&
				(e.Priority == BindingPriority.LocalValue));

		target.SetValue(Class1.FooProperty, "newvalue");

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void SetValueRaisesPropertyChanged()
	{
		var target = new Class1();
		var raised = false;

		target.PropertyChanged += (s, e) =>
			raised = (e.Property == Class1.FooProperty) &&
				((string) e.OldValue == "initial") &&
				((string) e.NewValue == "newvalue") &&
				(e.Priority == BindingPriority.LocalValue);

		target.SetValue(Class1.FooProperty, "newvalue");

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void SetValueSetsValue()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "newvalue");

		CornerstoneTest.AreEqual("newvalue", target.Foo);
	}

	[PresentationTestMethod]
	public void SetValueSetsValueNonGeneric()
	{
		var target = new Class1();

		target.SetValue((PresentationProperty) Class1.FooProperty, "newvalue");

		CornerstoneTest.AreEqual("newvalue", target.Foo);
	}

	[PresentationTestMethod]
	public void SetValueSetsValueOnAddOwneredPropertyUsingOriginal()
	{
		var target = new Class2();

		target.SetValue(Class1.FooProperty, "newvalue");

		CornerstoneTest.AreEqual("newvalue", target.Foo);
	}

	[PresentationTestMethod]
	public void SetValueSetsValueOnAddOwneredPropertyUsingOriginalNonGeneric()
	{
		var target = new Class2();

		target.SetValue((PresentationProperty) Class1.FooProperty, "newvalue");

		CornerstoneTest.AreEqual("newvalue", target.Foo);
	}

	[PresentationTestMethod]
	public void SetValueShouldNotCauseStackOverflowAndHaveCorrectValues()
	{
		var viewModel = new TestStackOverflowViewModel
		{
			Value = 50
		};

		var target = new Class1();

		target.Bind(Class1.DoubleValueProperty, new Binding("Value")
		{
			Mode = BindingMode.TwoWay,
			Source = viewModel
		});

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
	public void SettingObjectPropertyToDoNothingDoesNothing()
	{
		var target = new Class1();

		target.SetValue(Class1.FrankProperty, "newvalue");
		target.SetValue(Class1.FrankProperty, BindingOperations.DoNothing);

		CornerstoneTest.AreEqual("newvalue", target.GetValue(Class1.FrankProperty));
	}

	[PresentationTestMethod]
	public void SettingObjectPropertyToUnsetValueRevertsToDefaultValue()
	{
		var target = new Class1();

		target.SetValue(Class1.FrankProperty, "newvalue");
		target.SetValue(Class1.FrankProperty, PresentationProperty.UnsetValue);

		CornerstoneTest.AreEqual("Kups", target.GetValue(Class1.FrankProperty));
	}

	[PresentationTestMethod]
	public void UnsetValueIsUsedOnAddOwneredProperty()
	{
		var target = new Class2();

		target.SetValue(Class1.FooProperty, PresentationProperty.UnsetValue);

		CornerstoneTest.AreEqual("unset", target.Foo);
	}

	#endregion

	#region Classes

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly DirectProperty<Class1, string> BarProperty =
			PresentationProperty.RegisterDirect<Class1, string>(nameof(Bar), o => o.Bar);

		public static readonly DirectProperty<Class1, int> BazProperty =
			PresentationProperty.RegisterDirect<Class1, int>(
				nameof(Baz),
				o => o.Baz,
				(o, v) => o.Baz = v,
				-1);

		public static readonly DirectProperty<Class1, double> DoubleValueProperty =
			PresentationProperty.RegisterDirect<Class1, double>(
				nameof(DoubleValue),
				o => o.DoubleValue,
				(o, v) => o.DoubleValue = v);

		public static readonly DirectProperty<Class1, string> FooProperty =
			PresentationProperty.RegisterDirect<Class1, string>(
				nameof(Foo),
				o => o.Foo,
				(o, v) => o.Foo = v,
				"unset");

		public static readonly DirectProperty<Class1, object> FrankProperty =
			PresentationProperty.RegisterDirect<Class1, object>(
				nameof(Frank),
				o => o.Frank,
				(o, v) => o.Frank = v,
				"Kups");

		private int _baz = 5;
		private double _doubleValue;

		private string _foo = "initial";
		private object _frank;

		#endregion

		#region Properties

		public string Bar { get; } = "bar";

		public int Baz
		{
			get => _baz;
			set => SetAndRaise(BazProperty, ref _baz, value);
		}

		public double DoubleValue
		{
			get => _doubleValue;
			set => SetAndRaise(DoubleValueProperty, ref _doubleValue, value);
		}

		public string Foo
		{
			get => _foo;
			set => SetAndRaise(FooProperty, ref _foo, value);
		}

		public object Frank
		{
			get => _frank;
			set => SetAndRaise(FrankProperty, ref _frank, value);
		}

		#endregion
	}

	private class Class2 : PresentationObject
	{
		#region Fields

		public static readonly DirectProperty<Class2, string> FooProperty =
			Class1.FooProperty.AddOwner<Class2>(o => o.Foo, (o, v) => o.Foo = v);

		private string _foo = "initial2";

		#endregion

		#region Constructors

		static Class2()
		{
		}

		#endregion

		#region Properties

		public string Foo
		{
			get => _foo;
			set => SetAndRaise(FooProperty, ref _foo, value);
		}

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

	#endregion
}