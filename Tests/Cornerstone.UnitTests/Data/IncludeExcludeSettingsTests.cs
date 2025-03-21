#region References

using Cornerstone.Data;
using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Data;

[TestClass]
public class IncludeExcludeSettingsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Clone()
	{
		var expected = new IncludeExcludeSettings(["a", "b", "c"], ["x", "y", "z"]);
		var actual = expected.DeepClone();
		AreEqual(expected, actual);
		
		actual = expected.ShallowClone();
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void ToCode()
	{
		AreEqual("IncludeExcludeSettings.Empty", IncludeExcludeSettings.Empty.ToCSharp());
		AreEqual("""
				new IncludeExcludeSettings(
					["a", "b"],
					[]
				)
				""", new IncludeExcludeSettings(["a", "b"], null).ToCSharp());
		AreEqual("""
				new IncludeExcludeSettings(
					[],
					["c", "d"]
				)
				""", new IncludeExcludeSettings(null, ["c", "d"]).ToCSharp());
	}

	#endregion
}