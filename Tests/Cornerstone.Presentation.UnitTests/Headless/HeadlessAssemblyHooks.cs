#region References

using Cornerstone.Presentation.Headless;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Headless;

[TestClass]
public class HeadlessAssemblyHooks
{
	#region Methods

	[AssemblyInitialize]
	public static void AssemblyInitialize(TestContext context)
	{
		HeadlessUnitTestSession.GetOrStartForAssembly(typeof(TestApplication).Assembly);
	}

	#endregion
}