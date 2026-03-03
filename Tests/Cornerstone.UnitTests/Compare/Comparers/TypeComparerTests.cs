#region References

using Cornerstone.Compare;
using Cornerstone.Compare.Comparers;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Compare.Comparers;

public class TypeComparerTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void Compare()
	{
		var session = new CompareSession();
		var comparer = new TypeComparer();
		AreEqual(CompareResult.AreEqual, comparer.Compare(session, typeof(bool), typeof(bool)));
		AreEqual(CompareResult.NotEqual, comparer.Compare(session, typeof(bool), typeof(int)));
	}

	#endregion
}