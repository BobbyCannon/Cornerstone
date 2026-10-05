#region References

using System;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Headless;

[TestClass]
public class SetupTests : IDisposable
{
	#region Fields

	private static int sinstanceCount;

	#endregion

	#region Constructors

	public SetupTests()
	{
		++sinstanceCount;
	}

	#endregion

	#region Methods

	public void Dispose()
	{
		--sinstanceCount;
	}

	[HeadlessTestMethod]
	[DataRow(1)]
	[DataRow(2)]
	public void SetupTearDownShouldWork(int index)
	{
		AssertHelper.Equal(1, sinstanceCount);
	}

	#endregion
}