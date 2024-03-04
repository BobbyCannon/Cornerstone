#region References

using System;
using Cornerstone.Compare;
using Cornerstone.Compare.Comparers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Compare.Comparers;

[TestClass]
public class DateComparerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Kind()
	{
		var session = new CompareSession();
		var comparer = new DateComparer();

		AreEqual(CompareResult.AreEqual, comparer.Compare(session,
			new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Unspecified),
			new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc))
		);

		AreEqual(CompareResult.NotEqual, comparer.Compare(session,
			new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Unspecified),
			new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Local))
		);

		AreEqual(CompareResult.NotEqual, comparer.Compare(session,
			new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Local),
			new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc))
		);
	}

	#endregion
}