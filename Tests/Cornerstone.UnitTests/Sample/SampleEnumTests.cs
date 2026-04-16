#region References

using System.ComponentModel.DataAnnotations;
using Cornerstone.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sample;

[TestClass]
public class SampleEnumTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
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

	#region Enumerations

	[SourceReflection]
	public enum SampleEnum
	{
		[Display(Name = "Unknown", ShortName = "-1", Order = 0)]
		Unknown = 0,

		[Display(Name = "One", ShortName = "1", Order = 1)]
		One = 1,

		[Display(Name = "One", ShortName = "2", Order = 2)]
		Two = 2,

		[Display(Name = "Ten", ShortName = "10", Order = 3)]
		Ten = 10
	}

	#endregion
}