#region References

using System.Globalization;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data;

[TestClass]
public class StringConvertersTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow("hello", true)]
	[DataRow("", false)]
	[DataRow(null, false)]
	public void StringConvertersIsNotNullOrEmptyWorks(string input, bool expected)
	{
		var converter = StringConverters.IsNotNullOrEmpty;
		var result = converter.Convert(input, typeof(bool), null, CultureInfo.CurrentCulture);
		CornerstoneTest.AreEqual(expected, CornerstoneTest.IsType<bool>(result));
	}

	[PresentationTestMethod]
	[DataRow("hello", false)]
	[DataRow("", true)]
	[DataRow(null, true)]
	public void StringConvertersIsNullOrEmptyWorks(string input, bool expected)
	{
		var converter = StringConverters.IsNullOrEmpty;
		var result = converter.Convert(input, typeof(bool), null, CultureInfo.CurrentCulture);
		CornerstoneTest.AreEqual(expected, CornerstoneTest.IsType<bool>(result));
	}

	#endregion
}