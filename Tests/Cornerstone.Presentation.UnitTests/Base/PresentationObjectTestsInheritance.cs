#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class PresentationObjectTestsInheritance
{
	#region Methods

	[PresentationTestMethod]
	public void ClearValueClearsInheritedValue()
	{
		var parent = new Class1();
		var child = new Class2 { Parent = parent };

		parent.SetValue(Class1.BazProperty, "changed");

		CornerstoneTest.AreEqual("changed", child.GetValue(Class1.BazProperty));

		parent.ClearValue(Class1.BazProperty);

		CornerstoneTest.AreEqual("bazdefault", parent.GetValue(Class1.BazProperty));
		CornerstoneTest.AreEqual("bazdefault", child.GetValue(Class1.BazProperty));
	}

	[PresentationTestMethod]
	public void ClearValueOnChildRaisesPropertyChangedWithInheritedParentValue()
	{
		var parent = new Class1();
		var child = new Class2 { Parent = parent };
		var raised = 0;

		parent.SetValue(Class1.BazProperty, "parent");
		child.SetValue(Class1.BazProperty, "child");

		child.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.Same(child, e.Sender);
			CornerstoneTest.AreEqual("child", e.OldValue);
			CornerstoneTest.AreEqual("parent", e.NewValue);
			CornerstoneTest.AreEqual(BindingPriority.Inherited, e.Priority);
			++raised;
		};

		child.ClearValue(Class1.BazProperty);

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void ClearValueOnParentRaisesPropertyChangedOnChild()
	{
		var parent = new Class1();
		var child = new Class2 { Parent = parent };
		var raised = 0;

		parent.SetValue(Class1.BazProperty, "changed");

		child.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.Same(child, e.Sender);
			CornerstoneTest.AreEqual("changed", e.OldValue);
			CornerstoneTest.AreEqual("bazdefault", e.NewValue);
			CornerstoneTest.AreEqual(BindingPriority.Inherited, e.Priority);
			++raised;
		};

		parent.ClearValue(Class1.BazProperty);

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void ClearValueOnParentRaisesPropertyChangedOnChildWithInheritedGrandparentValue()
	{
		var grandparent = new Class1();
		var parent = new Class2 { Parent = grandparent };
		var child = new Class2 { Parent = parent };
		var raised = 0;

		grandparent.SetValue(Class1.BazProperty, "grandparent");
		parent.SetValue(Class1.BazProperty, "parent");

		child.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.Same(child, e.Sender);
			CornerstoneTest.AreEqual("parent", e.OldValue);
			CornerstoneTest.AreEqual("grandparent", e.NewValue);
			CornerstoneTest.AreEqual(BindingPriority.Inherited, e.Priority);
			++raised;
		};

		parent.ClearValue(Class1.BazProperty);

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void ClearingValueInInheritanceParentRaisesPropertyChanged()
	{
		var raised = false;

		var parent = new Class1();
		parent.SetValue(Class1.BazProperty, "changed");

		var child = new Class2 { Parent = parent };

		child.PropertyChanged += (s, e) =>
			raised = (s == child) &&
				(e.Property == Class1.BazProperty) &&
				((string) e.OldValue == "changed") &&
				((string) e.NewValue == "bazdefault");

		parent.ClearValue(Class1.BazProperty);

		CornerstoneTest.IsTrue(raised);
		CornerstoneTest.AreEqual("bazdefault", child.GetValue(Class1.BazProperty));
	}

	[PresentationTestMethod]
	public void GetValueReturnsInheritedValue1()
	{
		var parent = new Class1();
		parent.SetValue(Class1.BazProperty, "changed");

		var child = new Class2 { Parent = parent };
		CornerstoneTest.AreEqual("changed", child.GetValue(Class1.BazProperty));
	}

	[PresentationTestMethod]
	public void GetValueReturnsInheritedValue2()
	{
		var parent = new Class1();
		var child = new Class2 { Parent = parent };

		parent.SetValue(Class1.BazProperty, "changed");

		CornerstoneTest.AreEqual("changed", child.GetValue(Class1.BazProperty));
	}

	[PresentationTestMethod]
	public void PropertyChangedIsRaisedInParentBeforeChild()
	{
		var parent = new Class1();
		var child = new Class2 { Parent = parent };
		var result = new List<object>();

		parent.PropertyChanged += (s, e) => result.Add(parent);
		child.PropertyChanged += (s, e) => result.Add(child);

		parent.SetValue(Class1.BazProperty, "changed");

		CornerstoneTest.AreEqual(new[] { parent, child }, result);
	}

	[PresentationTestMethod]
	public void ReparentingRaisesPropertyChangedForOldAndNewInheritedValues()
	{
		var oldParent = new Class1();
		oldParent.SetValue(Class1.BazProperty, "oldvalue");

		var newParent = new Class1();
		newParent.SetValue(Class1.BazProperty, "newvalue");

		var child = new Class2 { Parent = oldParent };
		var raised = 0;

		child.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(child, e.Sender);
			CornerstoneTest.AreEqual("oldvalue", e.GetOldValue<string>());
			CornerstoneTest.AreEqual("newvalue", e.GetNewValue<string>());
			CornerstoneTest.AreEqual(BindingPriority.Inherited, e.Priority);
			++raised;
		};

		child.Parent = newParent;

		CornerstoneTest.AreEqual(1, raised);
		CornerstoneTest.AreEqual("newvalue", child.GetValue(Class1.BazProperty));
	}

	[PresentationTestMethod]
	public void ReparentingRaisesPropertyChangedOnGrandChildForOldAndNewInheritedValues()
	{
		var oldParent = new Class1();
		oldParent.SetValue(Class1.BazProperty, "oldvalue");

		var newParent = new Class1();
		newParent.SetValue(Class1.BazProperty, "newvalue");

		var child = new Class2 { Parent = oldParent };
		var grandchild = new Class2 { Parent = child };
		var raised = 0;

		grandchild.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(grandchild, e.Sender);
			CornerstoneTest.AreEqual("oldvalue", e.GetOldValue<string>());
			CornerstoneTest.AreEqual("newvalue", e.GetNewValue<string>());
			CornerstoneTest.AreEqual(BindingPriority.Inherited, e.Priority);
			++raised;
		};

		child.Parent = newParent;

		CornerstoneTest.AreEqual(1, raised);
		CornerstoneTest.AreEqual("newvalue", grandchild.GetValue(Class1.BazProperty));
	}

	[PresentationTestMethod]
	public void ReparentingRetainsInheritedPropertySetOnChild()
	{
		var oldParent = new Class1();
		oldParent.SetValue(Class1.BazProperty, "oldvalue");

		var newParent = new Class1();
		newParent.SetValue(Class1.BazProperty, "newvalue");

		var child = new Class2 { Parent = oldParent };
		child.SetValue(Class1.BazProperty, "childvalue");

		var grandchild = new Class2 { Parent = child };
		var raised = 0;

		grandchild.PropertyChanged += (s, e) => ++raised;

		child.Parent = newParent;

		CornerstoneTest.AreEqual(0, raised);
		CornerstoneTest.AreEqual("childvalue", child.GetValue(Class1.BazProperty));
		CornerstoneTest.AreEqual("childvalue", grandchild.GetValue(Class1.BazProperty));
	}

	[PresentationTestMethod]
	public void SettingInheritanceParentDoesntRaisePropertyChangedWhenLocalValueSet()
	{
		var raised = false;

		var parent = new Class1();
		parent.SetValue(Class1.BazProperty, "changed");

		var child = new Class2();
		child.SetValue(Class1.BazProperty, "localvalue");
		child.PropertyChanged += (s, e) => raised = true;

		child.Parent = parent;

		CornerstoneTest.IsFalse(raised);
		CornerstoneTest.AreEqual("localvalue", child.GetValue(Class1.BazProperty));
	}

	[PresentationTestMethod]
	public void SettingInheritanceParentRaisesPropertyChangedForAttachedPropertyWhenParentHasValueSet()
	{
		var raised = false;

		var parent = new Class1();
		parent.SetValue(AttachedOwner.AttachedProperty, "changed");

		var child = new Class2();
		child.PropertyChanged += (s, e) =>
			raised = (s == child) &&
				(e.Property == AttachedOwner.AttachedProperty) &&
				((string) e.OldValue == null) &&
				((string) e.NewValue == "changed");

		child.Parent = parent;

		CornerstoneTest.IsTrue(raised);
		CornerstoneTest.AreEqual("changed", child.GetValue(AttachedOwner.AttachedProperty));
	}

	[PresentationTestMethod]
	public void SettingInheritanceParentRaisesPropertyChangedWhenParentAndGrandparentHasValueSet()
	{
		var grandparent = new Class1();
		var parent = new Class2 { Parent = grandparent };
		var raised = false;

		grandparent.SetValue(Class1.BazProperty, "changed1");
		parent.SetValue(Class1.BazProperty, "changed2");

		var child = new Class2();
		child.PropertyChanged += (s, e) =>
			raised = (s == child) &&
				(e.Property == Class1.BazProperty) &&
				((string) e.OldValue == "bazdefault") &&
				((string) e.NewValue == "changed2") &&
				(e.Priority == BindingPriority.Inherited);

		child.Parent = parent;

		CornerstoneTest.IsTrue(raised);
		CornerstoneTest.AreEqual("changed2", child.GetValue(Class1.BazProperty));
	}

	[PresentationTestMethod]
	public void SettingInheritanceParentRaisesPropertyChangedWhenParentHasValueSet()
	{
		var raised = false;

		var parent = new Class1();
		parent.SetValue(Class1.BazProperty, "changed");

		var child = new Class2();
		child.PropertyChanged += (s, e) =>
			raised = (s == child) &&
				(e.Property == Class1.BazProperty) &&
				((string) e.OldValue == "bazdefault") &&
				((string) e.NewValue == "changed") &&
				(e.Priority == BindingPriority.Inherited);

		child.Parent = parent;

		CornerstoneTest.IsTrue(raised);
		CornerstoneTest.AreEqual("changed", child.GetValue(Class1.BazProperty));
	}

	[PresentationTestMethod]
	public void SettingValueInInheritanceParentRaisesPropertyChanged()
	{
		var raised = false;

		var parent = new Class1();

		var child = new Class2();
		child.PropertyChanged += (s, e) =>
			raised = (s == child) &&
				(e.Property == Class1.BazProperty) &&
				((string) e.OldValue == "bazdefault") &&
				((string) e.NewValue == "changed");
		child.Parent = parent;

		parent.SetValue(Class1.BazProperty, "changed");

		CornerstoneTest.IsTrue(raised);
		CornerstoneTest.AreEqual("changed", child.GetValue(Class1.BazProperty));
	}

	[PresentationTestMethod]
	public void SettingValueOfAttachedPropertyInInheritanceParentRaisesPropertyChanged()
	{
		var raised = false;

		var parent = new Class1();

		var child = new Class2();
		child.PropertyChanged += (s, e) =>
			raised = (s == child) &&
				(e.Property == AttachedOwner.AttachedProperty) &&
				((string) e.OldValue == null) &&
				((string) e.NewValue == "changed");
		child.Parent = parent;

		parent.SetValue(AttachedOwner.AttachedProperty, "changed");

		CornerstoneTest.IsTrue(raised);
		CornerstoneTest.AreEqual("changed", child.GetValue(AttachedOwner.AttachedProperty));
	}

	#endregion

	#region Classes

	private class AttachedOwner : PresentationObject
	{
		#region Fields

		public static readonly AttachedProperty<string> AttachedProperty =
			PresentationProperty.RegisterAttached<AttachedOwner, Class1, string>("Attached", inherits: true);

		#endregion
	}

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

	#endregion
}