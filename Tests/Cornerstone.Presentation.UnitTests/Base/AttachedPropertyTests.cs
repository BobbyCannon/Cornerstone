#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class AttachedPropertyTests
{
	#region Methods

	[PresentationTestMethod]
	public void IsAttachedReturnsTrue()
	{
		var property = new AttachedProperty<string>(
			"Foo",
			typeof(Class1),
			typeof(Control),
			new StyledPropertyMetadata<string>());

		CornerstoneTest.IsTrue(property.IsAttached);
	}

	#endregion

	#region Classes

	private class Class1 : PresentationObject
	{
	}

	#endregion
}