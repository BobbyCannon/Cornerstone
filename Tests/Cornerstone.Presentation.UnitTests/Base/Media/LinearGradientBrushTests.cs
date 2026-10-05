#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class LinearGradientBrushTests
{
	#region Methods

	[PresentationTestMethod]
	public void AddingGradientStopRaisesInvalidated()
	{
		var target = new LinearGradientBrush();

		target.GradientStops = new GradientStops { new GradientStop(Colors.Red, 0) };
		RenderResourceTestHelper.AssertResourceInvalidation(target, () => { target.GradientStops.Add(new GradientStop(Colors.Green, 1)); });
	}

	[PresentationTestMethod]
	public void ChangingEndPointRaisesInvalidated()
	{
		var target = new LinearGradientBrush();

		target.EndPoint = new RelativePoint();
		RenderResourceTestHelper.AssertResourceInvalidation(target, () => { target.EndPoint = new RelativePoint(10, 10, RelativeUnit.Absolute); });
	}

	[PresentationTestMethod]
	public void ChangingGradientStopOffsetRaisesInvalidated()
	{
		var target = new LinearGradientBrush();

		target.GradientStops = new GradientStops { new GradientStop(Colors.Red, 0) };
		RenderResourceTestHelper.AssertResourceInvalidation(target, () => { target.GradientStops[0].Offset = 0.5; });
	}

	[PresentationTestMethod]
	public void ChangingGradientStopsRaisesInvalidated()
	{
		var target = new LinearGradientBrush();

		target.GradientStops = new GradientStops { new GradientStop(Colors.Red, 0) };
		RenderResourceTestHelper.AssertResourceInvalidation(target, () => { target.GradientStops = new GradientStops { new GradientStop(Colors.Green, 0) }; });
	}

	[PresentationTestMethod]
	public void ChangingStartPointRaisesInvalidated()
	{
		var target = new LinearGradientBrush();

		target.StartPoint = new RelativePoint();

		RenderResourceTestHelper.AssertResourceInvalidation(target, () => { target.StartPoint = new RelativePoint(10, 10, RelativeUnit.Absolute); });
	}

	#endregion
}