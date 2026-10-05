#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ReversibleStackPanelTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ArrangesInReverseOrder()
	{
		var target = new ReversibleStackPanel
		{
			ReverseOrder = true,
			Children =
			{
				new Border { Height = 30, Width = 10 },
				new Border { Height = 50 }
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Rect(0, 50, 10, 30), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 0, 10, 50), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	public void InvalidatesArrangeOnReverseOrderChange()
	{
		var target = new ReversibleStackPanel
		{
			Children =
			{
				new Border(),
				new Border()
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));
		target.ReverseOrder = true;

		CornerstoneTest.IsTrue(target.IsMeasureValid);
		CornerstoneTest.IsFalse(target.IsArrangeValid);
	}

	#endregion
}