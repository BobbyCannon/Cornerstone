#region References

using System.Globalization;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data;

[TestClass]
public class ObjectConvertersTestsIsNull
{
	#region Methods

	[PresentationTestMethod]
	public void ReturnsFalseIfValueIsNotNull()
	{
		var result = ObjectConverters.IsNull.Convert(new object(), typeof(object), null, CultureInfo.InvariantCulture);

		CornerstoneTest.IsType<bool>(result);
		CornerstoneTest.IsTrue(result is false);
	}

	[PresentationTestMethod]
	public void ReturnsTrueIfValueIsNull()
	{
		var result = ObjectConverters.IsNull.Convert(null, typeof(object), null, CultureInfo.InvariantCulture);

		CornerstoneTest.IsType<bool>(result);
		CornerstoneTest.IsTrue(result is true);
	}

	#endregion
}