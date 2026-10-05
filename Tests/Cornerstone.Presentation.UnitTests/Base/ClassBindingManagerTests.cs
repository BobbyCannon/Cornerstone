#region References

using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class ClassBindingManagerTests
{
	#region Methods

	[PresentationTestMethod]
	public void GetClassPropertyShouldReturnDifferentInstancesForDifferentClasses()
	{
		var property1 = ClassBindingManager.GetClassProperty("Foo");
		var property2 = ClassBindingManager.GetClassProperty("Bar");
		CornerstoneTest.NotSame(property1, property2);
	}

	[PresentationTestMethod]
	public void GetClassPropertyShouldReturnSameInstanceForSameClass()
	{
		var property1 = ClassBindingManager.GetClassProperty("Foo");
		var property2 = ClassBindingManager.GetClassProperty("Foo");
		CornerstoneTest.Same(property1, property2);
	}

	#endregion
}