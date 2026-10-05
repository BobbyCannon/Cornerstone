#region References

using System.Reactive.Subjects;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class PresentationObjectTestsGetValue
{
	#region Methods

	[PresentationTestMethod]
	public void GetBaseValueIgnoresDefaultValue()
	{
		var target = new Class3();

		target.SetValue(Class1.FooProperty, "animated", BindingPriority.Animation);
		CornerstoneTest.IsFalse(target.GetBaseValue(Class1.FooProperty).HasValue);
	}

	[PresentationTestMethod]
	public void GetBaseValueReturnsLocalValue()
	{
		var target = new Class3();

		target.SetValue(Class1.FooProperty, "local");
		target.SetValue(Class1.FooProperty, "animated", BindingPriority.Animation);
		CornerstoneTest.AreEqual("local", target.GetBaseValue(Class1.FooProperty).Value);
	}

	[PresentationTestMethod]
	public void GetBaseValueReturnsStyleValue()
	{
		var target = new Class3();

		target.SetValue(Class1.FooProperty, "style", BindingPriority.Style);
		target.SetValue(Class1.FooProperty, "animated", BindingPriority.Animation);
		CornerstoneTest.AreEqual("style", target.GetBaseValue(Class1.FooProperty).Value);
	}

	[PresentationTestMethod]
	public void GetBaseValueReturnsStyleValueSetViaUntypedSetters()
	{
		var target = new Class3();

		target.SetValue(Class1.FooProperty, (object) "style", BindingPriority.Style);
		target.SetValue(Class1.FooProperty, (object) "animated", BindingPriority.Animation);
		CornerstoneTest.AreEqual("style", target.GetBaseValue(Class1.FooProperty).Value);
	}

	[PresentationTestMethod]
	public void GetValueDoesntThrowExceptionForUnregisteredProperty()
	{
		var target = new Class3();

		CornerstoneTest.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void GetValueReturnsBoundValue()
	{
		var target = new Class1();
		var property = Class1.FooProperty;

		target.Bind(property, new BehaviorSubject<string>("newvalue"));

		CornerstoneTest.AreEqual("newvalue", target.GetValue(property));
	}

	[PresentationTestMethod]
	public void GetValueReturnsDefaultValue()
	{
		var target = new Class1();

		CornerstoneTest.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void GetValueReturnsInheritedValue()
	{
		var parent = new Class1();
		var child = new Class2 { Parent = parent };

		parent.SetValue(Class1.BazProperty, "changed");

		CornerstoneTest.AreEqual("changed", child.GetValue(Class1.BazProperty));
	}

	[PresentationTestMethod]
	public void GetValueReturnsOverriddenDefaultValue()
	{
		var target = new Class2();

		CornerstoneTest.AreEqual("foooverride", target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void GetValueReturnsSetValue()
	{
		var target = new Class1();
		var property = Class1.FooProperty;

		target.SetValue(property, "newvalue");

		CornerstoneTest.AreEqual("newvalue", target.GetValue(property));
	}

	#endregion

	#region Classes

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly StyledProperty<string> BazProperty =
			PresentationProperty.Register<Class1, string>("Baz", "bazdefault", true);

		public static readonly StyledProperty<string> FooProperty =
			PresentationProperty.Register<Class1, string>("Foo", "foodefault");

		#endregion
	}

	private class Class2 : Class1
	{
		#region Constructors

		static Class2()
		{
			FooProperty.OverrideDefaultValue(typeof(Class2), "foooverride");
		}

		#endregion

		#region Properties

		public Class1 Parent
		{
			get => (Class1) InheritanceParent;
			set => InheritanceParent = value;
		}

		#endregion
	}

	private class Class3 : PresentationObject
	{
	}

	#endregion
}