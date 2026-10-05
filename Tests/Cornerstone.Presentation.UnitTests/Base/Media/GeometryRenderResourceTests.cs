#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class GeometryRenderResourceTests
{
	#region Methods

	[PresentationTestMethod]
	public void AddingChildToGeometryGroupRaisesInvalidated()
	{
		var target = new GeometryGroup();

		RenderResourceTestHelper.AssertResourceInvalidation(
			target,
			() => target.Children.Add(new EllipseGeometry(new Rect(0, 0, 10, 10))));
	}

	[PresentationTestMethod]
	public void ChangingChildOfGeometryGroupRaisesInvalidated()
	{
		var child = new EllipseGeometry(new Rect(0, 0, 10, 10));
		var target = new GeometryGroup();
		target.Children.Add(child);

		RenderResourceTestHelper.AssertResourceInvalidation(
			target,
			() => child.Rect = new Rect(0, 0, 20, 20));
	}

	[PresentationTestMethod]
	public void ChangingCombineModeOfCombinedGeometryRaisesInvalidated()
	{
		var geometry1 = new EllipseGeometry(new Rect(0, 0, 10, 10));
		var geometry2 = new RectangleGeometry(new Rect(5, 5, 10, 10));
		var target = new CombinedGeometry(GeometryCombineMode.Union, geometry1, geometry2);

		RenderResourceTestHelper.AssertResourceInvalidation(
			target,
			() => target.GeometryCombineMode = GeometryCombineMode.Intersect);
	}

	[PresentationTestMethod]
	public void ChangingGeometry1OfCombinedGeometryRaisesInvalidated()
	{
		var geometry1 = new EllipseGeometry(new Rect(0, 0, 10, 10));
		var geometry2 = new RectangleGeometry(new Rect(5, 5, 10, 10));
		var target = new CombinedGeometry(GeometryCombineMode.Union, geometry1, geometry2);

		RenderResourceTestHelper.AssertResourceInvalidation(
			target,
			() => geometry1.Rect = new Rect(0, 0, 20, 20));
	}

	[PresentationTestMethod]
	public void ChangingGeometryPropertyRaisesInvalidated()
	{
		var target = new EllipseGeometry(new Rect(0, 0, 10, 10));

		RenderResourceTestHelper.AssertResourceInvalidation(
			target,
			() => target.Rect = new Rect(0, 0, 20, 20));
	}

	[PresentationTestMethod]
	public void ChangingTransformRaisesInvalidated()
	{
		var target = new EllipseGeometry(new Rect(0, 0, 10, 10));

		RenderResourceTestHelper.AssertResourceInvalidation(
			target,
			() => target.Transform = new TranslateTransform(5, 5));
	}

	[PresentationTestMethod]
	public void ChangingTransformValueRaisesInvalidated()
	{
		var transform = new TranslateTransform(5, 5);
		var target = new EllipseGeometry(new Rect(0, 0, 10, 10)) { Transform = transform };

		RenderResourceTestHelper.AssertResourceInvalidation(
			target,
			() => transform.X = 10);
	}

	#endregion
}