#region References

using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class AssemblyExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetAssemblyDirectory()
	{
		// Update this once we have RuntimeInformation

		//var actual = typeof(AssemblyExtensionsTests)
		//	.Assembly
		//	.GetAssemblyDirectory();

		//var expected = $"{SolutionDirectory}\\Cornerstone.UnitTests\\bin\\Debug\\net8.0-windows10.0.19041.0";
		//AreEqual(UnitTestsDirectory, actual.FullName);
	}

	#endregion
}