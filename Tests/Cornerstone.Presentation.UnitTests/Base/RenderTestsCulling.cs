#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class RenderTestsCulling
{
	#region Methods

	[PresentationTestMethod]
	public void InBoundsControlShouldBeRendered()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			TestControl target;

			var container = new Canvas
			{
				Width = 100,
				Height = 100,
				ClipToBounds = true,
				Children =
				{
					(target = new TestControl
					{
						Width = 10, Height = 10, [Canvas.LeftProperty] = 98, [Canvas.TopProperty] = 98
					})
				}
			};

			Render(container);

			CornerstoneTest.IsTrue(target.Rendered);
		}
	}

	[PresentationTestMethod]
	public void NegativeMarginShouldBeRespected()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			TestControl target;

			var container = new Canvas
			{
				Width = 100,
				Height = 100,
				ClipToBounds = true,
				Children =
				{
					new Border
					{
						Margin = new Thickness(100, 100, 0, 0),
						Child = target = new TestControl
						{
							Width = 10, Height = 10, Margin = new Thickness(-100, -100, 0, 0)
						}
					}
				}
			};

			Render(container);

			CornerstoneTest.IsTrue(target.Rendered);
		}
	}

	[PresentationTestMethod]
	public void OutOfBoundsChildControlShouldNotBeRendered()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			TestControl target;

			var container = new Canvas
			{
				Width = 100,
				Height = 100,
				ClipToBounds = true,
				Children =
				{
					new Canvas
					{
						Width = 100,
						Height = 100,
						[Canvas.LeftProperty] = 50,
						[Canvas.TopProperty] = 50,
						Children =
						{
							(target = new TestControl
							{
								Width = 10,
								Height = 10,
								ClipToBounds = true,
								[Canvas.LeftProperty] = 50,
								[Canvas.TopProperty] = 50
							})
						}
					}
				}
			};

			Render(container);

			CornerstoneTest.IsFalse(target.Rendered);
		}
	}

	[PresentationTestMethod]
	public void OutOfBoundsControlShouldNotBeRendered()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			TestControl target;

			var container = new Canvas
			{
				Width = 100,
				Height = 100,
				ClipToBounds = true,
				Children =
				{
					(target = new TestControl
					{
						Width = 10,
						Height = 10,
						ClipToBounds = true,
						[Canvas.LeftProperty] = 110,
						[Canvas.TopProperty] = 110
					})
				}
			};

			Render(container);

			CornerstoneTest.IsFalse(target.Rendered);
		}
	}

	[PresentationTestMethod]
	public void RenderTransformShouldBeRespected()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			TestControl target;

			var container = new Canvas
			{
				Width = 100,
				Height = 100,
				ClipToBounds = true,
				Children =
				{
					(target = new TestControl
					{
						Width = 10,
						Height = 10,
						[Canvas.LeftProperty] = 110,
						[Canvas.TopProperty] = 110,
						RenderTransform = new TranslateTransform(-100, -100)
					})
				}
			};

			Render(container);

			CornerstoneTest.IsTrue(target.Rendered);
		}
	}

	[PresentationTestMethod]
	public void TransformedChildControlWithClipToBoundsTrueShouldBeRendered()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			TestControl target;

			var container = new Panel
			{
				Width = 100,
				Height = 20,
				ClipToBounds = true,
				Children =
				{
					new Panel
					{
						Width = 100,
						Height = 20,
						RenderTransform = new TranslateTransform(0, 30),
						Children =
						{
							(target = new TestControl
							{
								Width = 100,
								Height = 20,
								ClipToBounds = true,
								RenderTransform = new TranslateTransform(0, -30)
							})
						}
					}
				}
			};

			Render(container);

			CornerstoneTest.IsTrue(target.Rendered);
		}
	}

	private DrawingContext CreateDrawingContext()
	{
		return new PlatformDrawingContext(new StubDrawingContextImpl());
	}

	private void Render(Control control)
	{
		var ctx = CreateDrawingContext();
		control.Measure(Size.Infinity);
		control.Arrange(new Rect(control.DesiredSize));
		ImmediateRenderer.Render(ctx, control);
	}

	#endregion

	#region Classes

	private class TestControl : Control
	{
		#region Properties

		public bool Rendered { get; private set; }

		#endregion

		#region Methods

		public override void Render(DrawingContext context)
		{
			Rendered = true;
		}

		#endregion
	}

	#endregion
}