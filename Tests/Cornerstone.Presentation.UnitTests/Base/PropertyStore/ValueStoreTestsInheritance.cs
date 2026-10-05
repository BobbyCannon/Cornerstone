#region References

using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.PropertyStore;

[TestClass]
public class ValueStoreTestsInheritance
{
	#region Methods

	[PresentationTestMethod]
	public void AddingChildSetsInheritanceAncestor()
	{
		var parent = new Class1();
		var child = new Class1();
		var grandchild = new Class1 { Parent = child };

		parent.Foo = "changed";
		child.Parent = parent;

		var parentStore = parent.GetValueStore();
		CornerstoneTest.IsNull(parentStore.InheritanceAncestor);
		CornerstoneTest.Same(parentStore, child.GetValueStore().InheritanceAncestor);
		CornerstoneTest.Same(parentStore, grandchild.GetValueStore().InheritanceAncestor);
	}

	[PresentationTestMethod]
	public void ChildNotifiesAboutSettingBackToDefaultValue()
	{
		var parent = new Class1();
		var child = new Class1();

		parent.Foo = "changed";
		child.Parent = parent;

		var raised = false;
		child.PropertyChanged += (_, args) => { raised = (args.Property == Class1.FooProperty) && (args.GetNewValue<string>() == "foodefault"); };

		CornerstoneTest.AreEqual("changed", child.Foo); // inherited from parent.

		child.Foo = "foodefault"; // reset back to default.
		CornerstoneTest.IsTrue(raised); // expect event to be raised, as actual value was changed.
	}

	[PresentationTestMethod]
	public void ClearValueInParentDoesntUpdateGrandchildInheritanceAncestorIfChildHasValueSet()
	{
		var parent = new Class1();
		var child = new Class1 { Parent = parent };
		var grandchild = new Class1 { Parent = child };

		child.Foo = "foochanged";
		parent.Foo = "changed";
		parent.ClearValue(Class1.FooProperty);

		CornerstoneTest.IsNull(parent.GetValueStore().InheritanceAncestor);
		CornerstoneTest.IsNull(child.GetValueStore().InheritanceAncestor);
		CornerstoneTest.Same(child.GetValueStore(), grandchild.GetValueStore().InheritanceAncestor);
	}

	[PresentationTestMethod]
	public void ClearingValueInChildUpdatesInheritanceAncestor()
	{
		var parent = new Class1();
		var child = new Class1 { Parent = parent };
		var grandchild = new Class1 { Parent = child };

		parent.Foo = "changed";
		child.Foo = "foochanged";
		child.ClearValue(Class1.FooProperty);

		var parentStore = parent.GetValueStore();
		CornerstoneTest.IsNull(parentStore.InheritanceAncestor);
		CornerstoneTest.Same(parentStore, child.GetValueStore().InheritanceAncestor);
		CornerstoneTest.Same(parentStore, grandchild.GetValueStore().InheritanceAncestor);
	}

	[PresentationTestMethod]
	public void ClearingValueInParentUpdatesInheritanceAncestor()
	{
		var parent = new Class1();
		var child = new Class1 { Parent = parent };
		var grandchild = new Class1 { Parent = child };

		parent.Foo = "changed";
		parent.ClearValue(Class1.FooProperty);

		CornerstoneTest.IsNull(parent.GetValueStore().InheritanceAncestor);
		CornerstoneTest.IsNull(child.GetValueStore().InheritanceAncestor);
		CornerstoneTest.IsNull(grandchild.GetValueStore().InheritanceAncestor);
	}

	[PresentationTestMethod]
	public void InheritanceAncestorIsInitiallyNull()
	{
		var parent = new Class1();
		var child = new Class1 { Parent = parent };
		var grandchild = new Class1 { Parent = child };

		CornerstoneTest.IsNull(parent.GetValueStore().InheritanceAncestor);
		CornerstoneTest.IsNull(child.GetValueStore().InheritanceAncestor);
		CornerstoneTest.IsNull(grandchild.GetValueStore().InheritanceAncestor);
	}

	[PresentationTestMethod]
	public void SettingValueInParentDoesntUpdateGrandchildInheritanceAncestorIfChildHasValueSet()
	{
		var parent = new Class1();
		var child = new Class1 { Parent = parent };
		var grandchild = new Class1 { Parent = child };

		child.Foo = "foochanged";
		parent.Foo = "changed";

		var parentStore = parent.GetValueStore();
		CornerstoneTest.IsNull(parentStore.InheritanceAncestor);
		CornerstoneTest.Same(parentStore, child.GetValueStore().InheritanceAncestor);
		CornerstoneTest.Same(child.GetValueStore(), grandchild.GetValueStore().InheritanceAncestor);
	}

	[PresentationTestMethod]
	public void SettingValueInParentUpdatesInheritanceAncestor()
	{
		var parent = new Class1();
		var child = new Class1 { Parent = parent };
		var grandchild = new Class1 { Parent = child };

		parent.Foo = "changed";

		var parentStore = parent.GetValueStore();
		CornerstoneTest.IsNull(parentStore.InheritanceAncestor);
		CornerstoneTest.Same(parentStore, child.GetValueStore().InheritanceAncestor);
		CornerstoneTest.Same(parentStore, grandchild.GetValueStore().InheritanceAncestor);
	}

	#endregion

	#region Classes

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly StyledProperty<string> FooProperty =
			PresentationProperty.Register<Class1, string>("Foo", "foodefault", true);

		#endregion

		#region Properties

		public string Foo
		{
			get => GetValue(FooProperty);
			set => SetValue(FooProperty, value);
		}

		public Class1 Parent
		{
			get => (Class1) InheritanceParent;
			set => InheritanceParent = value;
		}

		#endregion
	}

	#endregion
}