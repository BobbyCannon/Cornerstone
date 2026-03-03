#region References

using Cornerstone.Generators.UnitTests.Sample;
using Cornerstone.Reflection;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Sample;

public class SampleEnumTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void Source()
	{
		var t = SourceReflector.GetRequiredSourceType(typeof(SampleEnum));
		IsTrue(t.IsEnum);
		AreEqual(typeof(SampleEnum), t.Type);
		AreEqual(4, t.DeclaredFields.Length);
		AreEqual("Unknown", t.DeclaredFields[0].Name);
		AreEqual(SampleEnum.Unknown, t.DeclaredFields[0].GetValue(null));
		AreEqual("One", t.DeclaredFields[1].Name);
		AreEqual(SampleEnum.One, t.DeclaredFields[1].GetValue(null));
		AreEqual("Two", t.DeclaredFields[2].Name);
		AreEqual(SampleEnum.Two, t.DeclaredFields[2].GetValue(null));
		AreEqual("Ten", t.DeclaredFields[3].Name);
		AreEqual(SampleEnum.Ten, t.DeclaredFields[3].GetValue(null));
	}

	#endregion
}