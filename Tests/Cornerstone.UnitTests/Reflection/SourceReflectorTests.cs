#region References

using System.Collections.Frozen;
using System.Linq;
using Cornerstone.Data.Times;
using Cornerstone.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Reflection;

[TestClass]
public class SourceReflectorTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CreateSourceTypeInfoUsingReflection()
	{
		var actual = SourceReflector.CreateSourceTypeInfoUsingReflection(typeof(FrozenSet<string>));
		IsNotNull(actual);
	}

	[TestMethod]
	public void GetEnumDetailsDictionary()
	{
		var actual = SourceReflector.GetEnumDetailsDictionary<TimeUnit>();
		AreEqual(11, actual.Count);

		var actualNames = string.Join(",", actual.Select(x => x.Value.Name));
		AreEqual("Ticks,Nanosecond,Microsecond,Millisecond,Second,Minute,Hour,Day,Week,Month,Year", actualNames);
	}

	#endregion
}