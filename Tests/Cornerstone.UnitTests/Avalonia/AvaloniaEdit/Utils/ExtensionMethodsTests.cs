#region References

using Cornerstone.Avalonia.AvaloniaEdit.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.AvaloniaEdit.Utils;

[TestClass]
public class ExtensionMethodsTests
{
	#region Methods

	[TestMethod]
	public void InfinityIsCloseToInfinity()
	{
		Assert.IsTrue(double.PositiveInfinity.IsClose(double.PositiveInfinity));
	}

	[TestMethod]
	public void NaNIsNotCloseToNaN()
	{
		Assert.IsFalse(double.NaN.IsClose(double.NaN));
	}

	[TestMethod]
	public void ZeroIsCloseToZero()
	{
		Assert.IsTrue(0.0.IsClose(0));
	}

	[TestMethod]
	public void ZeroIsNotCloseToOne()
	{
		Assert.IsFalse(0.0.IsClose(1));
	}

	#endregion
}