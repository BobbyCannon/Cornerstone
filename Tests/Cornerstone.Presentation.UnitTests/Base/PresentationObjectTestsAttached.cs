#region References

using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class PresentationObjectTestsAttached
{
	#region Methods

	[PresentationTestMethod]
	public void AddOwneredPropertyRetainsDefaultValue()
	{
		var target = new Class2();

		CornerstoneTest.AreEqual("foodefault", target.GetValue(Class2.FooProperty));
	}

	#endregion

	#region Classes

	private class Base : PresentationObject
	{
	}

	private class Class1 : Base
	{
		#region Fields

		public static readonly AttachedProperty<string> FooProperty =
			PresentationProperty.RegisterAttached<Class1, Base, string>(
				"Foo",
				"foodefault");

		#endregion
	}

	private class Class2 : Base
	{
		#region Fields

		public static readonly AttachedProperty<string> FooProperty =
			Class1.FooProperty.AddOwner<Class2>();

		#endregion
	}

	#endregion
}