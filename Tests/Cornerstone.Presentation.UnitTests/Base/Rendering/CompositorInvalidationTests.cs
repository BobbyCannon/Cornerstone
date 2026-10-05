#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Rendering;

[TestClass]
public class CompositorInvalidationTests : CompositorTestsBase
{
	#region Methods

	[PresentationTestMethod]
	public void ControlShouldInvalidateBothOwnRectsWhenMoved()
	{
		using (var s = new CompositorCanvas())
		{
			var control = new Border
			{
				Background = Brushes.Red, Width = 20, Height = 10,
				[Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50
			};
			s.Canvas.Children.Add(control);
			s.RunJobs();
			s.Events.Rects.Clear();
			control[Canvas.LeftProperty] = 55;
			s.AssertRects(new Rect(30, 50, 20, 10),
				new Rect(55, 50, 20, 10)
			);
		}
	}

	[PresentationTestMethod]
	public void ControlShouldInvalidateChildRectsWhenBecomesInvisible()
	{
		using (var s = new CompositorCanvas())
		{
			var control = new Decorator
			{
				[Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50,
				Padding = new Thickness(10),
				Child = new Border
				{
					Width = 20, Height = 10,
					Background = Brushes.Red
				}
			};
			s.Canvas.Children.Add(control);
			s.RunJobs();
			s.Events.Rects.Clear();
			control.IsVisible = false;
			s.AssertRects(new Rect(40, 60, 20, 10));
		}
	}

	[PresentationTestMethod]
	public void ControlShouldInvalidateChildRectsWhenMoved()
	{
		using (var s = new CompositorCanvas())
		{
			var control = new Decorator
			{
				[Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50,
				Padding = new Thickness(10),
				Child = new Border
				{
					Width = 20, Height = 10,
					Background = Brushes.Red
				}
			};
			s.Canvas.Children.Add(control);
			s.RunJobs();
			s.Events.Rects.Clear();
			control[Canvas.LeftProperty] = 55;
			s.AssertRects(new Rect(40, 60, 20, 10),
				new Rect(65, 60, 20, 10)
			);
		}
	}

	[PresentationTestMethod]
	public void ControlShouldInvalidateOwnRectWhenAdded()
	{
		using (var s = new CompositorCanvas())
		{
			var control = new Border
			{
				Background = Brushes.Red, Width = 20, Height = 10,
				[Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50
			};
			s.Canvas.Children.Add(control);
			s.AssertRects(new Rect(30, 50, 20, 10));
		}
	}

	[PresentationTestMethod]
	public void ControlShouldInvalidateOwnRectWhenRemoved()
	{
		using (var s = new CompositorCanvas())
		{
			var control = new Border
			{
				Background = Brushes.Red, Width = 20, Height = 10,
				[Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50
			};
			s.Canvas.Children.Add(control);
			s.RunJobs();
			s.Events.Rects.Clear();
			s.Canvas.Children.Remove(control);
			s.AssertRects(new Rect(30, 50, 20, 10));
		}
	}

	[PresentationTestMethod]
	public void SiblingControlsShouldInvalidateUnionRectWhenRemoved()
	{
		using (var s = new CompositorCanvas())
		{
			var control = new Border
			{
				Background = Brushes.Red, Width = 20, Height = 10,
				[Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 10
			};
			var control2 = new Border
			{
				Background = Brushes.Blue, Width = 20, Height = 10,
				[Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50
			};
			s.Canvas.Children.Add(control);
			s.Canvas.Children.Add(control2);
			s.RunJobs();
			s.Events.Rects.Clear();
			s.Canvas.Children.Remove(control);
			s.Canvas.Children.Remove(control2);
			s.AssertRects(new Rect(30, 10, 20, 50));
		}
	}

	#endregion
}