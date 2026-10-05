#region References

using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class PresentationObjectTestsCoercion
{
	#region Methods

	[PresentationTestMethod]
	public void ClearValueRespectsCoercedDefaultValue()
	{
		var target = new Class1();
		var raised = 0;

		target.Foo = 30;
		target.MinFoo = 20;

		target.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(Class1.FooProperty, e.Property);
			CornerstoneTest.AreEqual(30, e.OldValue);
			CornerstoneTest.AreEqual(20, e.NewValue);
			CornerstoneTest.AreEqual(BindingPriority.Unset, e.Priority);
			++raised;
		};

		target.ClearValue(Class1.FooProperty);

		CornerstoneTest.AreEqual(20, target.Foo);
		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void CoerceValueCallsCoerceCallbackOnlyOnce()
	{
		var target = new Class1 { Foo = 99 };

		target.MaxFoo = 50;

		target.CoerceFooInvocations.Clear();
		target.CoerceValue(Class1.FooProperty);

		CornerstoneTest.AreEqual(new[] { 99 }, target.CoerceFooInvocations);
	}

	[PresentationTestMethod]
	public void CoerceValueRaisesPropertyChanged()
	{
		var target = new Class1 { Foo = 99 };
		var raised = 0;

		target.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(Class1.FooProperty, e.Property);
			CornerstoneTest.AreEqual(99, e.OldValue);
			CornerstoneTest.AreEqual(50, e.NewValue);
			CornerstoneTest.AreEqual(BindingPriority.LocalValue, e.Priority);
			++raised;
		};

		CornerstoneTest.AreEqual(99, target.Foo);

		target.MaxFoo = 50;
		target.CoerceValue(Class1.FooProperty);

		CornerstoneTest.AreEqual(50, target.Foo);
		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void CoerceValueRaisesPropertyChangedCoreForBaseValue()
	{
		var target = new Class1 { Foo = 99 };

		target.SetValue(Class1.FooProperty, 88, BindingPriority.Animation);

		CornerstoneTest.AreEqual(88, target.Foo);
		CornerstoneTest.AreEqual(99, target.GetBaseValue(Class1.FooProperty));

		target.MaxFoo = 50;
		target.CoreChanges.Clear();
		target.CoerceValue(Class1.FooProperty);

		CornerstoneTest.AreEqual(2, target.CoreChanges.Count);
	}

	[PresentationTestMethod]
	public void CoerceValueUpdatesBaseValue()
	{
		var target = new Class1 { Foo = 99 };

		target.SetValue(Class1.FooProperty, 88, BindingPriority.Animation);

		CornerstoneTest.AreEqual(88, target.Foo);
		CornerstoneTest.AreEqual(99, target.GetBaseValue(Class1.FooProperty));

		target.MaxFoo = 50;
		target.CoerceValue(Class1.FooProperty);

		CornerstoneTest.AreEqual(50, target.Foo);
		CornerstoneTest.AreEqual(50, target.GetBaseValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void CoerceValueUpdatesInheritedValue()
	{
		var parent = new Class1 { Inherited = 99 };
		var child = new PresentationObject { InheritanceParent = parent };
		var raised = 0;

		child.InheritanceParent = parent;
		child.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(Class1.InheritedProperty, e.Property);
			CornerstoneTest.AreEqual(99, e.OldValue);
			CornerstoneTest.AreEqual(50, e.NewValue);
			CornerstoneTest.AreEqual(BindingPriority.Inherited, e.Priority);
			++raised;
		};

		CornerstoneTest.AreEqual(99, child.GetValue(Class1.InheritedProperty));

		parent.MaxFoo = 50;
		parent.CoerceValue(Class1.InheritedProperty);

		CornerstoneTest.AreEqual(50, child.GetValue(Class1.InheritedProperty));
		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void CoerceValueUpdatesValue()
	{
		var target = new Class1 { Foo = 99 };

		CornerstoneTest.AreEqual(99, target.Foo);

		target.MaxFoo = 50;
		target.CoerceValue(Class1.FooProperty);

		CornerstoneTest.AreEqual(50, target.Foo);
	}

	[PresentationTestMethod]
	public void CoercedValueCanBeRestoredFromPreviouslyActiveBinding()
	{
		var target = new Class1();
		var source1 = new Subject<BindingValue<int>>();
		var source2 = new Subject<BindingValue<int>>();

		target.Bind(Class1.FooProperty, source1, BindingPriority.Style);
		source1.OnNext(150);

		target.Bind(Class1.FooProperty, source2);
		source2.OnNext(160);

		CornerstoneTest.AreEqual(100, target.Foo);

		target.MaxFoo = 200;
		source2.OnCompleted();

		CornerstoneTest.AreEqual(150, target.Foo);
	}

	[PresentationTestMethod]
	public void CoercedValueCanBeRestoredIfLimitChanged()
	{
		var target = new Class1();

		target.Foo = 150;
		CornerstoneTest.AreEqual(100, target.Foo);

		target.MaxFoo = 200;
		target.CoerceValue(Class1.FooProperty);

		CornerstoneTest.AreEqual(150, target.Foo);
	}

	[PresentationTestMethod]
	public void CoercesBoundValue()
	{
		var target = new Class1();
		var source = new Subject<BindingValue<int>>();

		target.Bind(Class1.FooProperty, source);
		source.OnNext(150);

		CornerstoneTest.AreEqual(100, target.Foo);
	}

	[PresentationTestMethod]
	public void CoercesSetValue()
	{
		var target = new Class1();

		target.Foo = 150;

		CornerstoneTest.AreEqual(100, target.Foo);
	}

	[PresentationTestMethod]
	public void CoercesSetValueAttached()
	{
		var target = new Class1();

		target.SetValue(Class1.AttachedProperty, 150);

		CornerstoneTest.AreEqual(100, target.GetValue(Class1.AttachedProperty));
	}

	[PresentationTestMethod]
	public void CoercesSetValueAttachedOnClassNotDerivedFromOwner()
	{
		var target = new Class2();

		target.SetValue(Class1.AttachedProperty, 150);

		CornerstoneTest.AreEqual(100, target.GetValue(Class1.AttachedProperty));
	}

	[PresentationTestMethod]
	public void CoercionCanBeOverridden()
	{
		var target = new Class2();

		target.Foo = 150;

		CornerstoneTest.AreEqual(-150, target.Foo);
	}

	[PresentationTestMethod]
	public void DeactivatingStyleRespectsCoercedDefaultValue()
	{
		var target = new Control1
		{
			MinFoo = 20
		};

		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.OfType<Control1>().Class("foo"))
				{
					Setters =
					{
						new Setter(Control1.FooProperty, 50)
					}
				}
			},
			Child = target
		};

		var raised = 0;

		target.Classes.Add("foo");
		root.LayoutManager.ExecuteInitialLayoutPass();

		CornerstoneTest.AreEqual(50, target.Foo);

		target.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(Control1.FooProperty, e.Property);
			CornerstoneTest.AreEqual(50, e.OldValue);
			CornerstoneTest.AreEqual(20, e.NewValue);
			CornerstoneTest.AreEqual(BindingPriority.Unset, e.Priority);
			++raised;
		};

		target.Classes.Remove("foo");

		CornerstoneTest.AreEqual(20, target.Foo);
		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void DefaultValueCanBeCoerced()
	{
		var target = new Class1();
		var raised = 0;

		target.MinFoo = 20;

		target.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(Class1.FooProperty, e.Property);
			CornerstoneTest.AreEqual(11, e.OldValue);
			CornerstoneTest.AreEqual(20, e.NewValue);
			CornerstoneTest.AreEqual(BindingPriority.Unset, e.Priority);
			++raised;
		};

		target.CoerceValue(Class1.FooProperty);

		CornerstoneTest.AreEqual(20, target.Foo);
		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void DefaultValueIsCoercedOnlyOnce()
	{
		var target = new Class1();

		target.MinFoo = 20;
		target.CoerceFooInvocations.Clear();
		target.CoerceValue(Class1.FooProperty);

		CornerstoneTest.AreEqual(new[] { 11 }, target.CoerceFooInvocations);
	}

	[PresentationTestMethod]
	public void IfInitialStateHasCoercedDefaultValueThenCoerceValueMustBeCalled()
	{
		// This test is just explicitly describing an edge-case. If the initial state of the
		// object results in a coerced property value then CoerceValue must be called before
		// coercion takes effect. Confirmed as matching the behavior of WPF.
		var target = new Class3();

		CornerstoneTest.AreEqual(11, target.Foo);

		target.CoerceValue(Class3.FooProperty);

		CornerstoneTest.AreEqual(50, target.Foo);
	}

	[PresentationTestMethod]
	public void SecondCoerceOfDefaultValueIsPassedUncoercedValue()
	{
		var target = new Class1();

		target.MinFoo = 20;
		target.CoerceFooInvocations.Clear();
		target.CoerceValue(Class1.FooProperty);
		target.CoerceValue(Class1.FooProperty);

		CornerstoneTest.AreEqual(new[] { 11, 11 }, target.CoerceFooInvocations);
	}

	#endregion

	#region Classes

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly StyledProperty<int> FooProperty =
			PresentationProperty.Register<Class1, int>(
				"Foo",
				11,
				coerce: CoerceFoo);

		public static readonly AttachedProperty<int> AttachedProperty =
			PresentationProperty.RegisterAttached<Class1, PresentationObject, int>(
				"Attached",
				11,
				coerce: CoerceFoo);

		public static readonly StyledProperty<int> InheritedProperty =
			PresentationProperty.RegisterAttached<Class1, Class1, int>(
				"Attached",
				11,
				true,
				coerce: CoerceFoo);

		#endregion

		#region Properties

		public List<int> CoerceFooInvocations { get; } = new();
		public List<PresentationPropertyChangedEventArgs> CoreChanges { get; } = new();

		public int Foo
		{
			get => GetValue(FooProperty);
			set => SetValue(FooProperty, value);
		}

		public int Inherited
		{
			get => GetValue(InheritedProperty);
			set => SetValue(InheritedProperty, value);
		}

		public int MaxFoo { get; set; } = 100;

		public int MinFoo { get; set; }

		#endregion

		#region Methods

		public static int CoerceFoo(PresentationObject instance, int value)
		{
			(instance as Class1)?.CoerceFooInvocations.Add(value);
			return instance is Class1 o ? Math.Clamp(value, o.MinFoo, o.MaxFoo) : Math.Clamp(value, 0, 100);
		}

		protected override void OnPropertyChangedCore(PresentationPropertyChangedEventArgs change)
		{
			CoreChanges.Add(Clone(change));
			base.OnPropertyChangedCore(change);
		}

		private static PresentationPropertyChangedEventArgs Clone(PresentationPropertyChangedEventArgs change)
		{
			var e = (PresentationPropertyChangedEventArgs<int>) change;
			return new PresentationPropertyChangedEventArgs<int>(
				change.Sender,
				e.Property,
				e.OldValue,
				e.NewValue,
				change.Priority,
				change.IsEffectiveValueChange);
		}

		#endregion
	}

	private class Class2 : PresentationObject
	{
		#region Fields

		public static readonly StyledProperty<int> FooProperty =
			Class1.FooProperty.AddOwner<Class2>();

		#endregion

		#region Constructors

		static Class2()
		{
			FooProperty.OverrideMetadata<Class2>(
				new StyledPropertyMetadata<int>(
					coerce: CoerceFoo));
		}

		#endregion

		#region Properties

		public int Foo
		{
			get => GetValue(FooProperty);
			set => SetValue(FooProperty, value);
		}

		#endregion

		#region Methods

		public static int CoerceFoo(PresentationObject instance, int value)
		{
			return -value;
		}

		#endregion
	}

	private class Class3 : PresentationObject
	{
		#region Fields

		public static readonly StyledProperty<int> FooProperty =
			PresentationProperty.Register<Class3, int>(
				"Foo",
				11,
				coerce: CoerceFoo);

		#endregion

		#region Properties

		public int Foo
		{
			get => GetValue(FooProperty);
			set => SetValue(FooProperty, value);
		}

		#endregion

		#region Methods

		public static int CoerceFoo(PresentationObject instance, int value)
		{
			var o = (Class3) instance;
			return Math.Clamp(value, 50, 100);
		}

		#endregion
	}

	private class Control1 : Control
	{
		#region Fields

		public static readonly StyledProperty<int> FooProperty =
			PresentationProperty.Register<Control1, int>(
				"Foo",
				11,
				coerce: CoerceFoo);

		#endregion

		#region Properties

		public int Foo
		{
			get => GetValue(FooProperty);
			set => SetValue(FooProperty, value);
		}

		public int MaxFoo { get; } = 100;

		public int MinFoo { get; set; }

		#endregion

		#region Methods

		public static int CoerceFoo(PresentationObject instance, int value)
		{
			var o = (Control1) instance;
			return Math.Clamp(value, o.MinFoo, o.MaxFoo);
		}

		#endregion
	}

	#endregion
}