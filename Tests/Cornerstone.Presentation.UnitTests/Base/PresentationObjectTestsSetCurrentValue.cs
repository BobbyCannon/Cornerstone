#region References

using System;
using System.Reactive.Subjects;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Diagnostics;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Observable = Cornerstone.Presentation.Reactive.Observable;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class PresentationObjectTestsSetCurrentValue
{
	#region Methods

	[PresentationTestMethod]
	public void AnimationBindingOverridesCurrentValueWithLocalValuePriority()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "localvalue");
		target.SetCurrentValue(Class1.FooProperty, "current");

		var s = target.Bind(Class1.FooProperty, Observable.SingleValue("binding"), BindingPriority.Animation);

		CornerstoneTest.AreEqual("binding", target.Foo);
		CornerstoneTest.IsTrue(target.IsSet(Class1.FooProperty));
		CornerstoneTest.AreEqual(BindingPriority.Animation, GetPriority(target, Class1.FooProperty));
		CornerstoneTest.IsFalse(IsOverridden(target, Class1.FooProperty));

		s.Dispose();

		CornerstoneTest.AreEqual("current", target.Foo);
	}

	[PresentationTestMethod]
	public void AnimationValueOverridesCurrentValueWithLocalValuePriority()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "localvalue");
		target.SetCurrentValue(Class1.FooProperty, "current");
		target.SetValue(Class1.FooProperty, "setvalue", BindingPriority.Animation);

		CornerstoneTest.AreEqual("setvalue", target.Foo);
		CornerstoneTest.IsTrue(target.IsSet(Class1.FooProperty));
		CornerstoneTest.AreEqual(BindingPriority.Animation, GetPriority(target, Class1.FooProperty));
		CornerstoneTest.IsFalse(IsOverridden(target, Class1.FooProperty));
	}

	[PresentationTestMethod]
	[DataRow(BindingPriority.LocalValue)]
	[DataRow(BindingPriority.Style)]
	[DataRow(BindingPriority.Animation)]
	public void BindingOverridesCurrentValueWithUnsetPriority(BindingPriority priority)
	{
		var target = new Class1();

		target.SetCurrentValue(Class1.FooProperty, "current");

		var s = target.Bind(Class1.FooProperty, Observable.SingleValue("binding"), priority);

		CornerstoneTest.AreEqual("binding", target.Foo);
		CornerstoneTest.IsTrue(target.IsSet(Class1.FooProperty));
		CornerstoneTest.AreEqual(priority, GetPriority(target, Class1.FooProperty));
		CornerstoneTest.IsFalse(IsOverridden(target, Class1.FooProperty));

		s.Dispose();

		CornerstoneTest.AreEqual("foodefault", target.Foo);
	}

	[PresentationTestMethod]
	public void ClearValueClearsCurrentValueWithInheritedPriority()
	{
		var parent = new Class1();
		var target = new Class1 { InheritanceParent = parent };

		parent.SetValue(Class1.InheritedProperty, "inheritedvalue");
		target.SetCurrentValue(Class1.InheritedProperty, "newvalue");
		target.ClearValue(Class1.InheritedProperty);

		CornerstoneTest.AreEqual("inheritedvalue", target.Inherited);
		CornerstoneTest.IsFalse(target.IsSet(Class1.FooProperty));
		CornerstoneTest.IsFalse(IsOverridden(target, Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void ClearValueClearsCurrentValueWithLocalValuePriority()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "localvalue");
		target.SetCurrentValue(Class1.FooProperty, "newvalue");
		target.ClearValue(Class1.FooProperty);

		CornerstoneTest.AreEqual("foodefault", target.Foo);
		CornerstoneTest.IsFalse(target.IsSet(Class1.FooProperty));
		CornerstoneTest.IsFalse(IsOverridden(target, Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void ClearValueClearsCurrentValueWithStylePriority()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "stylevalue", BindingPriority.Style);
		target.SetCurrentValue(Class1.FooProperty, "newvalue");
		target.ClearValue(Class1.FooProperty);

		CornerstoneTest.AreEqual("stylevalue", target.Foo);
		CornerstoneTest.IsTrue(target.IsSet(Class1.FooProperty));
		CornerstoneTest.IsFalse(IsOverridden(target, Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void ClearValueClearsCurrentValueWithUnsetPriority()
	{
		var target = new Class1();

		target.SetCurrentValue(Class1.FooProperty, "newvalue");
		target.ClearValue(Class1.FooProperty);

		CornerstoneTest.AreEqual("foodefault", target.Foo);
		CornerstoneTest.IsFalse(target.IsSet(Class1.FooProperty));
		CornerstoneTest.IsFalse(IsOverridden(target, Class1.FooProperty));
	}

	[PresentationTestMethod]
	[DataRow(BindingPriority.LocalValue)]
	[DataRow(BindingPriority.Style)]
	[DataRow(BindingPriority.Animation)]
	public void CurrentValueIsReplacedByBindingValue(BindingPriority priority)
	{
		var target = new Class1();
		var source = new BehaviorSubject<string>("initial");

		target.Bind(Class1.FooProperty, source, priority);
		target.SetCurrentValue(Class1.FooProperty, "current");
		source.OnNext("new");

		CornerstoneTest.AreEqual("new", target.Foo);
	}

	[PresentationTestMethod]
	public void CurrentValueIsReplacedByNewStyleActivation1()
	{
		var target = new Class1();
		var root = new TestRoot(target)
		{
			Styles =
			{
				new Style(x => x.OfType<Class1>().Class("foo"))
				{
					Setters =
					{
						new Setter(Class1.FooProperty, "initial"),
						new Setter(Class1.BarProperty, "bar")
					}
				},
				new Style(x => x.OfType<Class1>().Class("bar"))
				{
					Setters =
					{
						new Setter(Class1.FooProperty, "new"),
						new Setter(Class1.BarProperty, "baz")
					}
				}
			}
		};

		root.LayoutManager.ExecuteInitialLayoutPass();

		target.Classes.Add("foo");
		CornerstoneTest.AreEqual("initial", target.Foo);

		target.SetCurrentValue(Class1.FooProperty, "current");
		target.Classes.Add("bar");

		CornerstoneTest.AreEqual("new", target.Foo);
	}

	[PresentationTestMethod]
	public void CurrentValueIsReplacedByNewStyleActivation2()
	{
		var target = new Class1();
		var root = new TestRoot(target)
		{
			Styles =
			{
				new Style(x => x.OfType<Class1>().Class("foo"))
				{
					Setters =
					{
						new Setter(Class1.FooProperty, "foo")
					}
				},
				new Style(x => x.OfType<Class1>().Class("foo"))
				{
					Setters =
					{
						new Setter(Class1.BarProperty, "bar")
					}
				}
			}
		};

		root.LayoutManager.ExecuteInitialLayoutPass();

		target.SetValue(Class1.FooProperty, "template", BindingPriority.Template);
		target.SetCurrentValue(Class1.FooProperty, "current");

		target.Classes.Add("foo");
		CornerstoneTest.AreEqual("foo", target.Foo);
	}

	[PresentationTestMethod]
	public void SetCurrentValueCanBeCoerced()
	{
		var target = new Class1();

		target.SetCurrentValue(Class1.CoercedProperty, 60);
		CornerstoneTest.AreEqual(60, target.GetValue(Class1.CoercedProperty));

		target.CoerceMax = 50;
		target.CoerceValue(Class1.CoercedProperty);
		CornerstoneTest.AreEqual(50, target.GetValue(Class1.CoercedProperty));

		target.CoerceMax = 100;
		target.CoerceValue(Class1.CoercedProperty);
		CornerstoneTest.AreEqual(60, target.GetValue(Class1.CoercedProperty));
	}

	[PresentationTestMethod]
	public void SetCurrentValueIsInherited()
	{
		var parent = new Class1();
		var target = new Class1 { InheritanceParent = parent };

		parent.SetCurrentValue(Class1.InheritedProperty, "newvalue");

		CornerstoneTest.AreEqual("newvalue", target.GetValue(Class1.InheritedProperty));
		CornerstoneTest.IsFalse(target.IsSet(Class1.FooProperty));
		CornerstoneTest.AreEqual(BindingPriority.Inherited, GetPriority(target, Class1.InheritedProperty));
		CornerstoneTest.IsFalse(IsOverridden(target, Class1.InheritedProperty));
	}

	[PresentationTestMethod]
	[DataRow(BindingPriority.LocalValue)]
	[DataRow(BindingPriority.Style)]
	[DataRow(BindingPriority.Animation)]
	public void SetCurrentValueOverridesExistingValue(BindingPriority priority)
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "oldvalue", priority);
		target.SetCurrentValue(Class1.FooProperty, "newvalue");

		CornerstoneTest.AreEqual("newvalue", target.GetValue(Class1.FooProperty));
		CornerstoneTest.IsTrue(target.IsSet(Class1.FooProperty));
		CornerstoneTest.AreEqual(priority, GetPriority(target, Class1.FooProperty));
		CornerstoneTest.IsTrue(IsOverridden(target, Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void SetCurrentValueOverridesInheritedValue()
	{
		var parent = new Class1();
		var target = new Class1 { InheritanceParent = parent };

		parent.SetValue(Class1.InheritedProperty, "inheritedvalue");
		target.SetCurrentValue(Class1.InheritedProperty, "newvalue");

		CornerstoneTest.AreEqual("newvalue", target.GetValue(Class1.InheritedProperty));
		CornerstoneTest.IsTrue(target.IsSet(Class1.InheritedProperty));
		CornerstoneTest.AreEqual(BindingPriority.Unset, GetPriority(target, Class1.InheritedProperty));
		CornerstoneTest.IsTrue(IsOverridden(target, Class1.InheritedProperty));
	}

	[PresentationTestMethod]
	public void SetCurrentValuePersistsWhenTogglingStyle1()
	{
		var target = new Class1();
		var root = new TestRoot(target)
		{
			Styles =
			{
				new Style(x => x.OfType<Class1>().Class("foo"))
				{
					Setters = { new Setter(Class1.BarProperty, "bar") }
				}
			}
		};

		root.LayoutManager.ExecuteInitialLayoutPass();

		target.SetCurrentValue(Class1.FooProperty, "current");

		CornerstoneTest.AreEqual("current", target.Foo);
		CornerstoneTest.AreEqual("bardefault", target.Bar);

		target.Classes.Add("foo");

		CornerstoneTest.AreEqual("current", target.Foo);
		CornerstoneTest.AreEqual("bar", target.Bar);

		target.Classes.Remove("foo");

		CornerstoneTest.AreEqual("current", target.Foo);
		CornerstoneTest.AreEqual("bardefault", target.Bar);
	}

	[PresentationTestMethod]
	public void SetCurrentValuePersistsWhenTogglingStyle2()
	{
		var target = new Class1();
		var root = new TestRoot(target)
		{
			Styles =
			{
				new Style(x => x.OfType<Class1>().Class("foo"))
				{
					Setters =
					{
						new Setter(Class1.BarProperty, "bar"),
						new Setter(Class1.InheritedProperty, "inherited")
					}
				}
			}
		};

		root.LayoutManager.ExecuteInitialLayoutPass();

		target.SetCurrentValue(Class1.FooProperty, "current");

		CornerstoneTest.AreEqual("current", target.Foo);
		CornerstoneTest.AreEqual("bardefault", target.Bar);
		CornerstoneTest.AreEqual("inheriteddefault", target.Inherited);

		target.Classes.Add("foo");

		CornerstoneTest.AreEqual("current", target.Foo);
		CornerstoneTest.AreEqual("bar", target.Bar);
		CornerstoneTest.AreEqual("inherited", target.Inherited);

		target.Classes.Remove("foo");

		CornerstoneTest.AreEqual("current", target.Foo);
		CornerstoneTest.AreEqual("bardefault", target.Bar);
		CornerstoneTest.AreEqual("inheriteddefault", target.Inherited);
	}

	[PresentationTestMethod]
	public void SetCurrentValuePersistsWhenTogglingStyle3()
	{
		var target = new Class1();
		var root = new TestRoot(target)
		{
			Styles =
			{
				new Style(x => x.OfType<Class1>().Class("foo"))
				{
					Setters =
					{
						new Setter(Class1.BarProperty, "bar"),
						new Setter(Class1.InheritedProperty, "inherited")
					}
				}
			}
		};

		root.LayoutManager.ExecuteInitialLayoutPass();

		target.SetValue(Class1.FooProperty, "not current", BindingPriority.Template);
		target.SetCurrentValue(Class1.FooProperty, "current");

		CornerstoneTest.AreEqual("current", target.Foo);
		CornerstoneTest.AreEqual("bardefault", target.Bar);
		CornerstoneTest.AreEqual("inheriteddefault", target.Inherited);

		target.Classes.Add("foo");

		CornerstoneTest.AreEqual("current", target.Foo);
		CornerstoneTest.AreEqual("bar", target.Bar);
		CornerstoneTest.AreEqual("inherited", target.Inherited);

		target.Classes.Remove("foo");

		CornerstoneTest.AreEqual("current", target.Foo);
		CornerstoneTest.AreEqual("bardefault", target.Bar);
		CornerstoneTest.AreEqual("inheriteddefault", target.Inherited);
	}

	[PresentationTestMethod]
	public void SetCurrentValueSetsUnsetValue()
	{
		var target = new Class1();

		target.SetCurrentValue(Class1.FooProperty, "newvalue");

		CornerstoneTest.AreEqual("newvalue", target.GetValue(Class1.FooProperty));
		CornerstoneTest.IsTrue(target.IsSet(Class1.FooProperty));
		CornerstoneTest.AreEqual(BindingPriority.Unset, GetPriority(target, Class1.FooProperty));
		CornerstoneTest.IsTrue(IsOverridden(target, Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void SetCurrentValueSetsUnsetValueUntyped()
	{
		var target = new Class1();

		target.SetCurrentValue((PresentationProperty) Class1.FooProperty, "newvalue");

		CornerstoneTest.AreEqual("newvalue", target.GetValue(Class1.FooProperty));
		CornerstoneTest.IsTrue(target.IsSet(Class1.FooProperty));
		CornerstoneTest.AreEqual(BindingPriority.Unset, GetPriority(target, Class1.FooProperty));
		CornerstoneTest.IsTrue(IsOverridden(target, Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void SetCurrentValueUnsetClearsCurrentValue()
	{
		var target = new Class1();

		target.SetCurrentValue(Class1.FooProperty, "newvalue");
		target.SetCurrentValue(Class1.FooProperty, PresentationProperty.UnsetValue);

		CornerstoneTest.AreEqual("foodefault", target.Foo);
		CornerstoneTest.IsFalse(target.IsSet(Class1.FooProperty));
		CornerstoneTest.IsFalse(IsOverridden(target, Class1.FooProperty));
	}

	[PresentationTestMethod]
	[DataRow(BindingPriority.LocalValue)]
	[DataRow(BindingPriority.Style)]
	[DataRow(BindingPriority.Animation)]
	public void SetValueOverridesCurrentValueWithUnsetPriority(BindingPriority priority)
	{
		var target = new Class1();

		target.SetCurrentValue(Class1.FooProperty, "current");
		target.SetValue(Class1.FooProperty, "setvalue", priority);

		CornerstoneTest.AreEqual("setvalue", target.Foo);
		CornerstoneTest.IsTrue(target.IsSet(Class1.FooProperty));
		CornerstoneTest.AreEqual(priority, GetPriority(target, Class1.FooProperty));
		CornerstoneTest.IsFalse(IsOverridden(target, Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void StyleTriggerBindingOverridesCurrentValueWithStylePriority()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "style", BindingPriority.Style);
		target.SetCurrentValue(Class1.FooProperty, "current");

		var s = target.Bind(Class1.FooProperty, Observable.SingleValue("binding"), BindingPriority.StyleTrigger);

		CornerstoneTest.AreEqual("binding", target.Foo);
		CornerstoneTest.IsTrue(target.IsSet(Class1.FooProperty));
		CornerstoneTest.AreEqual(BindingPriority.StyleTrigger, GetPriority(target, Class1.FooProperty));
		CornerstoneTest.IsFalse(IsOverridden(target, Class1.FooProperty));

		s.Dispose();

		CornerstoneTest.AreEqual("style", target.Foo);
	}

	[PresentationTestMethod]
	public void StyleTriggerValueOverridesCurrentValueWithStylePriority()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "style", BindingPriority.Style);
		target.SetCurrentValue(Class1.FooProperty, "current");
		target.SetValue(Class1.FooProperty, "setvalue", BindingPriority.StyleTrigger);

		CornerstoneTest.AreEqual("setvalue", target.Foo);
		CornerstoneTest.IsTrue(target.IsSet(Class1.FooProperty));
		CornerstoneTest.AreEqual(BindingPriority.StyleTrigger, GetPriority(target, Class1.FooProperty));
		CornerstoneTest.IsFalse(IsOverridden(target, Class1.FooProperty));
	}

	private BindingPriority GetPriority(PresentationObject target, PresentationProperty property)
	{
		return target.GetDiagnostic(property).Priority;
	}

	private bool IsOverridden(PresentationObject target, PresentationProperty property)
	{
		return target.GetDiagnostic(property).IsOverriddenCurrentValue;
	}

	#endregion

	#region Classes

	private class Class1 : Control
	{
		#region Fields

		public static readonly StyledProperty<string> BarProperty =
			PresentationProperty.Register<Class1, string>(nameof(Bar), "bardefault");

		public static readonly StyledProperty<string> FooProperty =
			PresentationProperty.Register<Class1, string>(nameof(Foo), "foodefault");

		public static readonly StyledProperty<string> InheritedProperty =
			PresentationProperty.Register<Class1, string>(nameof(Inherited), "inheriteddefault", true);

		public static readonly StyledProperty<double> CoercedProperty =
			PresentationProperty.Register<Class1, double>(nameof(Coerced), coerce: Coerce);

		#endregion

		#region Properties

		public string Bar => GetValue(BarProperty);
		public double CoerceMax { get; set; } = 100;
		public double Coerced => GetValue(CoercedProperty);

		public string Foo => GetValue(FooProperty);
		public string Inherited => GetValue(InheritedProperty);

		#endregion

		#region Methods

		private static double Coerce(PresentationObject sender, double value)
		{
			return Math.Min(value, ((Class1) sender).CoerceMax);
		}

		#endregion
	}

	private class ViewModel : NotifyingBase
	{
		#region Fields

		private string _value;

		#endregion

		#region Properties

		public string Value
		{
			get => _value;
			set
			{
				if (_value != value)
				{
					_value = value;
					RaisePropertyChanged();
				}
			}
		}

		#endregion
	}

	#endregion
}