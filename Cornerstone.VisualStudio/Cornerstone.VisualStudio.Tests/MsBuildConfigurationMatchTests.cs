#region References

using Cornerstone.VisualStudio.Core.Completion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests;

[TestClass]
public class MsBuildConfigurationMatchTests
{
	#region Methods

	[TestMethod]
	public void EmptyActiveConfigurationMatchesAnyOutput()
	{
		Assert.IsTrue(MsBuildConfigurationMatch.Matches(null, "Release", @"bin\Release\net10.0\App.dll"));
		Assert.IsTrue(MsBuildConfigurationMatch.Matches("", "Debug", @"bin\Debug\net10.0\App.dll"));
	}

	[TestMethod]
	public void MatchesOutputConfigurationName()
	{
		Assert.IsTrue(MsBuildConfigurationMatch.Matches("Debug", "Debug", @"bin\Release\net10.0\App.dll"));
		Assert.IsFalse(MsBuildConfigurationMatch.Matches("Debug", "Release", @"bin\Release\net10.0\App.dll"));
	}

	[TestMethod]
	public void InfersConfigurationFromTargetPathWhenDimensionMissing()
	{
		Assert.IsTrue(MsBuildConfigurationMatch.Matches(
			"Debug", null, @"C:\src\App\bin\Debug\net10.0\App.dll"));
		Assert.IsFalse(MsBuildConfigurationMatch.Matches(
			"Debug", null, @"C:\src\App\bin\Release\net10.0\App.dll"));
	}

	[TestMethod]
	public void DoesNotFallBackToTheOtherConfiguration()
	{
		Assert.IsFalse(MsBuildConfigurationMatch.Matches(
			"Debug", "Release", @"C:\src\App\bin\Release\net10.0\App.dll"));
	}

	#endregion
}
