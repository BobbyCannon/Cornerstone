#region References

using Cornerstone.Avalonia.TextEditor.Utils;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.IntegrationTests.Avalonia.TextEditor.Utils;

[TestClass]
public class ExtensionMethodsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void InfinityIsCloseToInfinity()
	{
		IsTrue(double.PositiveInfinity.IsClose(double.PositiveInfinity));
	}

	[TestMethod]
	public void NaNIsNotCloseToNaN()
	{
		IsFalse(double.NaN.IsClose(double.NaN));
	}

	[TestMethod]
	public void ZeroIsCloseToZero()
	{
		IsTrue(0.0.IsClose(0));
	}

	[TestMethod]
	public void ZeroIsNotCloseToOne()
	{
		IsFalse(0.0.IsClose(1));
	}

	#endregion
}