#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class CanvasTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BottomPropertyShouldWork()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		Rectangle rect;
		var target = new Canvas
		{
			Width = 400,
			Height = 400,
			Children =
			{
				(rect = new Rectangle
				{
					MinWidth = 20,
					MinHeight = 25,
					[Canvas.BottomProperty] = 30
				})
			}
		};

		target.Measure(new Size(400, 400));
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Rect(0, 345, 20, 25), rect.Bounds);
	}

	[PresentationTestMethod]
	public void LeftPropertyShouldWork()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		Rectangle rect;
		var target = new Canvas
		{
			Width = 400,
			Height = 400,
			Children =
			{
				(rect = new Rectangle
				{
					MinWidth = 20,
					MinHeight = 25,
					[Canvas.LeftProperty] = 30
				})
			}
		};

		target.Measure(new Size(400, 400));
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Rect(30, 0, 20, 25), rect.Bounds);
	}

	[PresentationTestMethod]
	public void RightPropertyShouldWork()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		Rectangle rect;
		var target = new Canvas
		{
			Width = 400,
			Height = 400,
			Children =
			{
				(rect = new Rectangle
				{
					MinWidth = 20,
					MinHeight = 25,
					[Canvas.RightProperty] = 30
				})
			}
		};

		target.Measure(new Size(400, 400));
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Rect(350, 0, 20, 25), rect.Bounds);
	}

	[PresentationTestMethod]
	public void TopPropertyShouldWork()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		Rectangle rect;
		var target = new Canvas
		{
			Width = 400,
			Height = 400,
			Children =
			{
				(rect = new Rectangle
				{
					MinWidth = 20,
					MinHeight = 25,
					[Canvas.TopProperty] = 30
				})
			}
		};

		target.Measure(new Size(400, 400));
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Rect(0, 30, 20, 25), rect.Bounds);
	}

	#endregion
}