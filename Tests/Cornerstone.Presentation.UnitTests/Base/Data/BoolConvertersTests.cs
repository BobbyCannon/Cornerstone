#region References

using System.Globalization;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data;

[TestClass]
public class BoolConvertersTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(false, false, false)]
	[DataRow(false, true, false)]
	[DataRow(true, false, false)]
	[DataRow(true, true, true)]
	public void BoolConvertersAndWorks(bool a, bool b, bool y)
	{
		var converter = BoolConverters.And;
		var result = converter.Convert([a, b], typeof(bool), null, CultureInfo.CurrentCulture);
		CornerstoneTest.AreEqual(y, CornerstoneTest.IsType<bool>(result));
	}

	[PresentationTestMethod]
	public void BoolConvertersNotReturnsUnsetOnInvalidInput()
	{
		var converter = BoolConverters.Not;
		var result = converter.Convert(1234, typeof(bool), null, CultureInfo.CurrentCulture);
		CornerstoneTest.AreEqual(PresentationProperty.UnsetValue, result);
	}

	[PresentationTestMethod]
	public void BoolConvertersNotWorksTwoWay()
	{
		var converter = BoolConverters.Not;
		var result = converter.Convert(true, typeof(bool), null, CultureInfo.CurrentCulture);
		CornerstoneTest.IsFalse(CornerstoneTest.IsType<bool>(result));

		result = converter.ConvertBack(false, typeof(bool), null, CultureInfo.CurrentCulture);
		CornerstoneTest.IsTrue(CornerstoneTest.IsType<bool>(result));
	}

	[PresentationTestMethod]
	[DataRow(false, false, false)]
	[DataRow(false, true, true)]
	[DataRow(true, false, true)]
	[DataRow(true, true, true)]
	public void BoolConvertersOrWorks(bool a, bool b, bool y)
	{
		var converter = BoolConverters.Or;
		var result = converter.Convert([a, b], typeof(bool), null, CultureInfo.CurrentCulture);
		CornerstoneTest.AreEqual(y, CornerstoneTest.IsType<bool>(result));
	}

	#endregion
}