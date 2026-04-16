#region References

using Cornerstone.Compare;
using Cornerstone.Compare.Comparers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Compare.Comparers;

[TestClass]
public class TypeComparerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Compare()
	{
		var session = new CompareSession();
		var comparer = new TypeComparer();
		AreEqual(CompareResult.AreEqual, comparer.Compare(session, typeof(bool), typeof(bool)));
		AreEqual(CompareResult.NotEqual, comparer.Compare(session, typeof(bool), typeof(int)));
	}

	#endregion
}