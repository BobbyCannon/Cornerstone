#region References

using System;
using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TupleExtensions = Cornerstone.Extensions.TupleExtensions;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class TupleExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CreateTuple()
	{
		var values = new object[] { 123, "test" };
		var actual = TupleExtensions.CreateTuple(values);
		AreEqual(typeof(Tuple<,>), actual.GetType().GetGenericTypeDefinition());
		var actual1 = actual.GetMemberValue("Item1");
		AreEqual(typeof(int), actual1.GetType());
		var actual2 = actual.GetMemberValue("Item2");
		AreEqual(typeof(string), actual2.GetType());
	}

	[TestMethod]
	public void CreateValueTuple()
	{
		var values = new object[] { 123, "test" };
		var actual = TupleExtensions.CreateValueTuple(values);
		AreEqual(typeof(ValueTuple<,>), actual.GetType().GetGenericTypeDefinition());
		var actual1 = actual.GetMemberValue("Item1");
		AreEqual(typeof(int), actual1.GetType());
		var actual2 = actual.GetMemberValue("Item2");
		AreEqual(typeof(string), actual2.GetType());
	}

	#endregion
}