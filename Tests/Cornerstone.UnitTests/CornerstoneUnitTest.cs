#region References

using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests;

public abstract class CornerstoneUnitTest : CornerstoneTest
{
	#region Methods

	[TearDown]
	[TestCleanup]
	public override void TestCleanup()
	{
		base.TestCleanup();
	}

	[SetUp]
	[TestInitialize]
	public override void TestInitialize()
	{
		base.TestInitialize();
	}

	#endregion
}