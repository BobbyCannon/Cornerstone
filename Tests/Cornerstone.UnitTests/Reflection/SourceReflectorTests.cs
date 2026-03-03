#region References

using System.Collections.Frozen;
using System.Linq;
using Cornerstone.Data.Times;
using Cornerstone.Reflection;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Reflection;

public class SourceReflectorTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void CreateSourceTypeInfoUsingReflection()
	{
		var actual = SourceReflector.CreateSourceTypeInfoUsingReflection(typeof(FrozenSet<string>));
		IsNotNull(actual);
	}

	[Test]
	public void GetEnumDetailsDictionary()
	{
		var actual = SourceReflector.GetEnumDetailsDictionary<TimeUnit>();
		AreEqual(11, actual.Count);

		var actualNames = string.Join(",", actual.Select(x => x.Value.Name));
		AreEqual("Ticks,Nanosecond,Microsecond,Millisecond,Second,Minute,Hour,Day,Week,Month,Year", actualNames);
	}

	#endregion
}