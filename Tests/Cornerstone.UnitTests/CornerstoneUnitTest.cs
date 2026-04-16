#region References

using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests;

public abstract class CornerstoneUnitTest : CornerstoneTest
{
	#region Methods

	[TestCleanup]
	public override void TestCleanup()
	{
		base.TestCleanup();
	}

	[TestInitialize]
	public override void TestInitialize()
	{
		base.TestInitialize();
	}

	#endregion
}