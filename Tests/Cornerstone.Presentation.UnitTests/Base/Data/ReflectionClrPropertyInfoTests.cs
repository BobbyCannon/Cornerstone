#region References

using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data;

[TestClass]
public class ReflectionClrPropertyInfoTests
{
	#region Methods

	[PresentationTestMethod]
	public void CanCompile()
	{
		var propertyInfo = new ReflectionClrPropertyInfo(
			typeof(TestClass).GetProperty(nameof(TestClass.Test))!);
		var target = new TestClass();
		const string result = "qwerty";
		propertyInfo.Set(target, result);
		CornerstoneTest.AreEqual(result, target.Test);
		CornerstoneTest.AreEqual(result, (string) propertyInfo.Get(target));
	}

	#endregion

	#region Classes

	public class TestClass
	{
		#region Properties

		public string Test { get; set; }

		#endregion
	}

	#endregion
}