#region References

using System.Globalization;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data;

[TestClass]
public class ObjectConvertersTestsEqual
{
	#region Methods

	[PresentationTestMethod]
	public void ReturnsFalseIfValueAndParameterAreDifferentObjects()
	{
		var result = ObjectConverters.Equal.Convert(new object(), typeof(object), new object(), CultureInfo.InvariantCulture);

		CornerstoneTest.IsType<bool>(result);
		CornerstoneTest.IsTrue(result is false);
	}

	[PresentationTestMethod]
	public void ReturnsFalseIfValueIsNullAndParameterIsNotNull()
	{
		var result = ObjectConverters.Equal.Convert(null, typeof(object), new object(), CultureInfo.InvariantCulture);

		CornerstoneTest.IsType<bool>(result);
		CornerstoneTest.IsTrue(result is false);
	}

	[PresentationTestMethod]
	public void ReturnsTrueIfValueAndParameterAreNull()
	{
		var result = ObjectConverters.Equal.Convert(null, typeof(object), null, CultureInfo.InvariantCulture);

		CornerstoneTest.IsType<bool>(result);
		CornerstoneTest.IsTrue(result is true);
	}

	[PresentationTestMethod]
	public void ReturnsTrueIfValueAndParameterAreSameObject()
	{
		var target = new object();
		var result = ObjectConverters.Equal.Convert(target, typeof(object), target, CultureInfo.InvariantCulture);

		CornerstoneTest.IsType<bool>(result);
		CornerstoneTest.IsTrue(result is true);
	}

	#endregion
}