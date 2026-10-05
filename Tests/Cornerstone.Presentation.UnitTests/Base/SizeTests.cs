#region References

using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class SizeTests
{
	#region Methods

	[PresentationTestMethod]
	public void DividingShouldProduceScalingFactor()
	{
		var result = new Size(15, 10) / new Size(5, 5);

		CornerstoneTest.AreEqual(new Vector(3, 2), result);
	}

	[PresentationTestMethod]
	public void ShouldProduceCorrectAspectRatio()
	{
		var result = new Size(3, 2).AspectRatio;

		CornerstoneTest.AreEqual(1.5, result);
	}

	#endregion
}