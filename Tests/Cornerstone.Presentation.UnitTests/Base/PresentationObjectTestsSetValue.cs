#region References

using System;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class PresentationObjectTestsSetValue
{
	#region Methods

	[PresentationTestMethod]
	public void ClearValueClearsValue()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "newvalue");
		target.ClearValue(Class1.FooProperty);

		CornerstoneTest.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
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
			CornerstoneTest.AreEqual(BindingPriority.Unset, e.Priority);
			CornerstoneTest.AreEqual(Class1.FooProperty, e.Property);
			CornerstoneTest.AreEqual("newvalue", (string) e.OldValue);
			CornerstoneTest.AreEqual("foodefault", (string) e.NewValue);
			++raised;
		};

		target.ClearValue(Class1.FooProperty);

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void ClearValueResetsValueToStylevalue()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "style", BindingPriority.Style);
		target.SetValue(Class1.FooProperty, "local");

		CornerstoneTest.AreEqual("local", target.GetValue(Class1.FooProperty));

		target.ClearValue(Class1.FooProperty);

		CornerstoneTest.AreEqual("style", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void DisposingAnimationSetValueRevertsToPreviousLocalValue()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "foo", BindingPriority.LocalValue);
		var d = target.SetValue(Class1.FooProperty, "bar", BindingPriority.Animation);
		CornerstoneTest.IsNotNull(d);
		d.Dispose();

		CornerstoneTest.AreEqual("foo", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void DisposingStyleSetValueRevertsToDefaultValue()
	{
		var target = new Class1();

		var d = target.SetValue(Class1.FooProperty, "foo", BindingPriority.Style);
		CornerstoneTest.IsNotNull(d);
		d.Dispose();

		CornerstoneTest.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void DisposingStyleSetValueRevertsToPreviousStyleValue()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "foo", BindingPriority.Style);
		var d = target.SetValue(Class1.FooProperty, "bar", BindingPriority.Style);
		CornerstoneTest.IsNotNull(d);
		d.Dispose();

		CornerstoneTest.AreEqual("foo", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void IsSetReturnsFalseForClearedProperty()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "foo");
		target.SetValue(Class1.FooProperty, PresentationProperty.UnsetValue);

		CornerstoneTest.IsFalse(target.IsSet(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void IsSetReturnsFalseForSetProperty()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "foo");

		CornerstoneTest.IsTrue(target.IsSet(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void IsSetReturnsFalseForUnsetProperty()
	{
		var target = new Class1();

		CornerstoneTest.IsFalse(target.IsSet(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void SetValueAllowsSettingUnregisteredAttachedProperty()
	{
		var target = new Class1();

		CornerstoneTest.IsFalse(PresentationPropertyRegistry.Instance.IsRegistered(target, AttachedOwner.AttachedProperty));

		target.SetValue(AttachedOwner.AttachedProperty, "bar");

		CornerstoneTest.AreEqual("bar", target.GetValue(AttachedOwner.AttachedProperty));
	}

	[PresentationTestMethod]
	public void SetValueAllowsSettingUnregisteredProperty()
	{
		var target = new Class1();

		CornerstoneTest.IsFalse(PresentationPropertyRegistry.Instance.IsRegistered(target, Class2.BarProperty));

		target.SetValue(Class2.BarProperty, "bar");

		CornerstoneTest.AreEqual("bar", target.GetValue(Class2.BarProperty));
	}

	[PresentationTestMethod]
	public void SetValueAnimationOverridesLocalValue()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "one", BindingPriority.LocalValue);
		CornerstoneTest.AreEqual("one", target.GetValue(Class1.FooProperty));
		target.SetValue(Class1.FooProperty, "two", BindingPriority.Animation);
		CornerstoneTest.AreEqual("two", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void SetValueCanConvertToNullable()
	{
		var target = new Class2();

		target.SetValue((PresentationProperty) Class2.FredProperty, 4.0);

		var value = target.GetValue(Class2.FredProperty);
		CornerstoneTest.IsType<double>(value);
		CornerstoneTest.AreEqual(4, value);
	}

	[PresentationTestMethod]
	public void SetValueDoesntRaisePropertyChangedIfValueNotChanged()
	{
		var target = new Class1();
		var raised = false;

		target.SetValue(Class1.FooProperty, "bar");

		target.PropertyChanged += (s, e) => { raised = true; };

		target.SetValue(Class1.FooProperty, "bar");

		CornerstoneTest.IsFalse(raised);
	}

	[PresentationTestMethod]
	public void SetValueDoesntRaisePropertyChangedIfValueNotChangedFromDefault()
	{
		var target = new Class1();
		var raised = false;

		target.PropertyChanged += (s, e) => { raised = true; };

		target.SetValue(Class1.FooProperty, "foodefault");

		CornerstoneTest.IsFalse(raised);
	}

	[PresentationTestMethod]
	public void SetValueLocalValueOverridesStyle()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "one", BindingPriority.Style);
		CornerstoneTest.AreEqual("one", target.GetValue(Class1.FooProperty));
		target.SetValue(Class1.FooProperty, "two", BindingPriority.LocalValue);
		CornerstoneTest.AreEqual("two", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void SetValueOfIntegerOnDoublePropertyWorks()
	{
		var target = new Class2();

		target.SetValue((PresentationProperty) Class2.FlobProperty, 4);

		var value = target.GetValue(Class2.FlobProperty);
		CornerstoneTest.IsType<double>(value);
		CornerstoneTest.AreEqual(4, value);
	}

	[PresentationTestMethod]
	public void SetValueRaisesPropertyChanged()
	{
		var target = new Class1();
		var raised = false;

		target.PropertyChanged += (s, e) =>
		{
			raised = (s == target) &&
				(e.Property == Class1.FooProperty) &&
				((string) e.OldValue == "foodefault") &&
				((string) e.NewValue == "newvalue");
		};

		target.SetValue(Class1.FooProperty, "newvalue");

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void SetValueRespectsImplicitConversions()
	{
		var target = new Class2();

		target.SetValue((PresentationProperty) Class2.FlobProperty, new ImplicitDouble(4));

		var value = target.GetValue(Class2.FlobProperty);
		CornerstoneTest.IsType<double>(value);
		CornerstoneTest.AreEqual(4, value);
	}

	[PresentationTestMethod]
	public void SetValueRespectsPriority()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "one", BindingPriority.Template);
		CornerstoneTest.AreEqual("one", target.GetValue(Class1.FooProperty));
		target.SetValue(Class1.FooProperty, "two", BindingPriority.Style);
		CornerstoneTest.AreEqual("one", target.GetValue(Class1.FooProperty));
		target.SetValue(Class1.FooProperty, "three", BindingPriority.StyleTrigger);
		CornerstoneTest.AreEqual("three", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void SetValueSetsAttachedValue()
	{
		var target = new Class2();

		target.SetValue(AttachedOwner.AttachedProperty, "newvalue");

		CornerstoneTest.AreEqual("newvalue", target.GetValue(AttachedOwner.AttachedProperty));
	}

	[PresentationTestMethod]
	public void SetValueSetsValue()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "newvalue");

		CornerstoneTest.AreEqual("newvalue", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void SetValueStyleDoesntOverrideLocalValue()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "one", BindingPriority.LocalValue);
		CornerstoneTest.AreEqual("one", target.GetValue(Class1.FooProperty));
		target.SetValue(Class1.FooProperty, "two", BindingPriority.Style);
		CornerstoneTest.AreEqual("one", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void SetValueStylePriorityRaisesPropertyChanged()
	{
		var target = new Class1();
		var raised = false;

		target.PropertyChanged += (s, e) =>
		{
			raised = (s == target) &&
				(e.Property == Class1.FooProperty) &&
				((string) e.OldValue == "foodefault") &&
				((string) e.NewValue == "newvalue");
		};

		target.SetValue(Class1.FooProperty, "newvalue", BindingPriority.Style);

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void SetValueThrowsExceptionForInvalidValueType()
	{
		var target = new Class1();

		Assert.Throws<ArgumentException>(() => { target.SetValue(Class1.FooProperty, 123); });
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
	public void SettingUnsetValueRevertsToDefaultValue()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "newvalue");
		target.SetValue(Class1.FooProperty, PresentationProperty.UnsetValue);

		CornerstoneTest.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
	}

	#endregion

	#region Classes

	private class AttachedOwner
	{
		#region Fields

		public static readonly AttachedProperty<string> AttachedProperty =
			PresentationProperty.RegisterAttached<AttachedOwner, Class2, string>("Attached");

		#endregion
	}

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly StyledProperty<string> FooProperty =
			PresentationProperty.Register<Class1, string>("Foo", "foodefault");

		public static readonly StyledProperty<object> FrankProperty =
			PresentationProperty.Register<Class1, object>("Frank", "Kups");

		#endregion
	}

	private class Class2 : Class1
	{
		#region Fields

		public static readonly StyledProperty<string> BarProperty =
			PresentationProperty.Register<Class2, string>("Bar", "bardefault");

		public static readonly StyledProperty<double> FlobProperty =
			PresentationProperty.Register<Class2, double>("Flob");

		public static readonly StyledProperty<double?> FredProperty =
			PresentationProperty.Register<Class2, double?>("Fred");

		#endregion

		#region Properties

		public Class1 Parent
		{
			get => (Class1) InheritanceParent;
			set => InheritanceParent = value;
		}

		#endregion
	}

	private class ImplicitDouble
	{
		#region Constructors

		public ImplicitDouble(double value)
		{
			Value = value;
		}

		#endregion

		#region Properties

		public double Value { get; }

		#endregion

		#region Methods

		public static implicit operator double(ImplicitDouble v)
		{
			return v.Value;
		}

		#endregion
	}

	#endregion
}