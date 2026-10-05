#region References

using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class PresentationObjectTestsAddOwner
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

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly StyledProperty<string> FooProperty =
			PresentationProperty.Register<Class1, string>(
				"Foo",
				"foodefault");

		#endregion
	}

	private class Class2 : PresentationObject
	{
		#region Fields

		public static readonly StyledProperty<string> FooProperty =
			Class1.FooProperty.AddOwner<Class2>();

		#endregion
	}

	#endregion
}