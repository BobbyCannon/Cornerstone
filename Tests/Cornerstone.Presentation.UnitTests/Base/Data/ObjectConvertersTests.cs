#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data;

[TestClass]
public class ObjectConvertersTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(null, null, null, true)]
	[DataRow(null, null, "value", false)]
	[DataRow(null, "value", null, false)]
	[DataRow("value", null, null, false)]
	[DataRow("value", "value", "value", false)]
	public void ObjectConvertersTestsAreAllNullWorks(object value1, object value2, object value3, bool valid)
	{
		var converter = ObjectConverters.AreAllNull;
		var result = converter.Convert([value1, value2, value3], typeof(bool), null, CultureInfo.CurrentCulture);
		CornerstoneTest.AreEqual(valid, CornerstoneTest.IsType<bool>(result));
	}

	[PresentationTestMethod]
	[DataRow(null, null, null, true)]
	[DataRow(null, null, "value", true)]
	[DataRow(null, "value", null, true)]
	[DataRow("value", null, null, true)]
	[DataRow("value", "value", "value", false)]
	public void ObjectConvertersTestsAreAnyNullWorks(object value1, object value2, object value3, bool valid)
	{
		var converter = ObjectConverters.AreAnyNull;
		var result = converter.Convert([value1, value2, value3], typeof(bool), null, CultureInfo.CurrentCulture);
		CornerstoneTest.AreEqual(valid, CornerstoneTest.IsType<bool>(result));
	}

	[PresentationTestMethod]
	[DataRow(true, true, true)]
	[DataRow(false, true, false)]
	[DataRow(false, false, true)]
	public void ObjectConvertersTestsAreEqualEdgeWorks(bool empty, bool unique, bool valid)
	{
		ICollection<object> values;
		if (empty)
		{
			values = Array.Empty<object>();
		}
		else if (unique)
		{
			values = ["1", "2"];
		}
		else
		{
			values = ["1", "1"];
		}

		var converter = ObjectConverters.AreAllEqual;
		var result = converter.Convert(values.ToList(), typeof(bool), null, CultureInfo.CurrentCulture);
		CornerstoneTest.AreEqual(valid, CornerstoneTest.IsType<bool>(result));
	}

	[PresentationTestMethod]
	[DataRow("value", "value", "value", true)]
	[DataRow(null, "value", null, false)]
	[DataRow("value", null, "value", false)]
	[DataRow("value", "value", "value1", false)]
	[DataRow("value1", "value", "value1", false)]
	[DataRow("value", "value", 1, false)]
	public void ObjectConvertersTestsAreEqualWorks(object value1, object value2, object value3, bool valid)
	{
		var converter = ObjectConverters.AreAllEqual;
		var result = converter.Convert([value1, value2, value3], typeof(bool), null, CultureInfo.CurrentCulture);
		CornerstoneTest.AreEqual(valid, CornerstoneTest.IsType<bool>(result));
	}

	#endregion
}