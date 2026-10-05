#region References

using System.Globalization;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data;

[TestClass]
public class ObjectConvertersTestsNotEqual
{
	#region Methods

	[PresentationTestMethod]
	public void ReturnsFalseIfValueAndParameterAreNull()
	{
		var result = ObjectConverters.NotEqual.Convert(null, typeof(object), null, CultureInfo.InvariantCulture);

		CornerstoneTest.IsType<bool>(result);
		CornerstoneTest.IsTrue(result is false);
	}

	[PresentationTestMethod]
	public void ReturnsFalseIfValueAndParameterAreSameObject()
	{
		var target = new object();
		var result = ObjectConverters.NotEqual.Convert(target, typeof(object), target, CultureInfo.InvariantCulture);

		CornerstoneTest.IsType<bool>(result);
		CornerstoneTest.IsTrue(result is false);
	}

	[PresentationTestMethod]
	public void ReturnsTrueIfValueAndParameterAreDifferentObjects()
	{
		var result = ObjectConverters.NotEqual.Convert(new object(), typeof(object), new object(), CultureInfo.InvariantCulture);

		CornerstoneTest.IsType<bool>(result);
		CornerstoneTest.IsTrue(result is true);
	}

	[PresentationTestMethod]
	public void ReturnsTrueIfValueIsNullAndParameterIsNotNull()
	{
		var result = ObjectConverters.NotEqual.Convert(null, typeof(object), new object(), CultureInfo.InvariantCulture);

		CornerstoneTest.IsType<bool>(result);
		CornerstoneTest.IsTrue(result is true);
	}

	#endregion
}