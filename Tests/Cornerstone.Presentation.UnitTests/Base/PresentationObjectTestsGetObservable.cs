#region References

using System;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class PresentationObjectTestsGetObservable
{
	#region Methods

	[PresentationTestMethod]
	public void GetObservableDisposeStopsPropertyChanges()
	{
		var target = new Class1();
		var raised = false;

		target.GetObservable(Class1.FooProperty)
			.Subscribe(x => raised = true)
			.Dispose();
		raised = false;
		target.SetValue(Class1.FooProperty, "newvalue");

		CornerstoneTest.IsFalse(raised);
	}

	[PresentationTestMethod]
	public void GetObservableReturnsInitialValue()
	{
		var target = new Class1();
		var raised = 0;

		target.GetObservable(Class1.FooProperty).Subscribe(x =>
		{
			if (x == "foodefault")
			{
				++raised;
			}
		});

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void GetObservableReturnsPropertyChange()
	{
		var target = new Class1();
		var raised = false;

		target.GetObservable(Class1.FooProperty).Subscribe(x => raised = x == "newvalue");
		raised = false;
		target.SetValue(Class1.FooProperty, "newvalue");

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void GetObservableReturnsPropertyChangeOnlyForCorrectProperty()
	{
		var target = new Class2();
		var raised = false;

		target.GetObservable(Class1.FooProperty).Subscribe(x => raised = true);
		raised = false;
		target.SetValue(Class2.BarProperty, "newvalue");

		CornerstoneTest.IsFalse(raised);
	}

	#endregion

	#region Classes

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly StyledProperty<string> FooProperty =
			PresentationProperty.Register<Class1, string>("Foo", "foodefault");

		#endregion
	}

	private class Class2 : Class1
	{
		#region Fields

		public static readonly StyledProperty<string> BarProperty =
			PresentationProperty.Register<Class2, string>("Bar", "bardefault");

		#endregion
	}

	#endregion
}