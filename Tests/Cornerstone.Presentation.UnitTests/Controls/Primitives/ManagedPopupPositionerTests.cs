#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Controls.Primitives.PopupPositioning;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Primitives;

[TestClass]
public class ManagedPopupPositionerTests : ScopedTestBase
{
	#region Methods

	// The anchor rectangle overlaps the 4 screens, so each corner lands on a different screen.
	// With no gravity the popup is centered on the anchor point and is slid back into the screen containing that point.
	[PresentationTestMethod]
	[DataRow(PopupAnchor.TopLeft, 600, 600)]
	[DataRow(PopupAnchor.TopRight, 1000, 600)]
	[DataRow(PopupAnchor.BottomLeft, 600, 1000)]
	[DataRow(PopupAnchor.BottomRight, 1000, 1000)]
	public void UsesScreenContainingTheAnchorPoint(PopupAnchor anchor, double expectedX, double expectedY)
	{
		var popup = new MockManagedPopupPositionerPopup();
		var positioner = new ManagedPopupPositioner(popup);

		positioner.Update(new PopupPositionerParameters
		{
			Size = new Size(400, 400),
			AnchorRectangle = new Rect(900, 900, 200, 200),
			Anchor = anchor,
			Gravity = PopupGravity.None,
			ConstraintAdjustment = PopupPositionerConstraintAdjustment.All
		});

		CornerstoneTest.AreEqual(new Point(expectedX, expectedY), popup.LastPosition);
	}

	#endregion

	#region Classes

	private sealed class MockManagedPopupPositionerPopup : IManagedPopupPositionerPopup
	{
		#region Properties

		public Point LastPosition { get; private set; }

		public Size LastSize { get; private set; }

		public Rect ParentClientAreaScreenGeometry => new(0, 0, 1000, 1000);

		public double Scaling => 1.0;

		// Four screens arranged in a 2x2 grid, meeting at (1000, 1000).
		public IReadOnlyList<ManagedPopupPositionerScreenInfo> Screens { get; } =
		[
			new(new Rect(0, 0, 1000, 1000), new Rect(0, 0, 1000, 1000)),
			new(new Rect(1000, 0, 1000, 1000), new Rect(1000, 0, 1000, 1000)),
			new(new Rect(0, 1000, 1000, 1000), new Rect(0, 1000, 1000, 1000)),
			new(new Rect(1000, 1000, 1000, 1000), new Rect(1000, 1000, 1000, 1000))
		];

		#endregion

		#region Methods

		public void MoveAndResize(Point devicePoint, Size virtualSize)
		{
			LastPosition = devicePoint;
			LastSize = virtualSize;
		}

		#endregion
	}

	#endregion
}