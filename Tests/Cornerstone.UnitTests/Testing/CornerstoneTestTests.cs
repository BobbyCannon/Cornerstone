#region References

using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Testing;

[TestClass]
public class CornerstoneTestTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CalculateTypePath()
	{
		var type = typeof(CornerstoneTest);
		var expected = $"{SolutionDirectory}\\Cornerstone\\Testing\\CornerstoneTest.cs";
		var actual = CalculateTypeFilePath(SolutionDirectory, type.FullName, ".cs");

		AreEqual(expected, actual);
		
		type = typeof(Babel);
		expected = $"{SolutionDirectory}\\Cornerstone\\Babel.cs";
		actual = CalculateTypeFilePath(SolutionDirectory, type.FullName, ".cs");

		AreEqual(expected, actual);
	}

	#endregion
}