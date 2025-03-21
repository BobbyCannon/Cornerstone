#region References

using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests;

[TestClass]
public class DependencyProviderTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ShouldChainDependency()
	{
		var provider = new DependencyProvider("Test");
		provider.AddSingleton<RuntimeInformation>();
		provider.AddSingleton<IRuntimeInformation, RuntimeInformation>();
		var actual2 = provider.GetInstance<RuntimeInformation>();
		var actual1 = provider.GetInstance<IRuntimeInformation>();
		IsTrue(ReferenceEquals(actual1, actual2));
	}

	[TestMethod]
	public void ShouldChainDependencyReverseGetInstance()
	{
		var provider = new DependencyProvider("Test");
		provider.AddSingleton<RuntimeInformation>();
		provider.AddSingleton<IRuntimeInformation, RuntimeInformation>();
		var actual1 = provider.GetInstance<IRuntimeInformation>();
		var actual2 = provider.GetInstance<RuntimeInformation>();
		IsTrue(ReferenceEquals(actual1, actual2));
	}

	#endregion
}