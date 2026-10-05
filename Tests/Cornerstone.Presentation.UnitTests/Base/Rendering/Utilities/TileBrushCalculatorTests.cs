#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Rendering.Utilities;

[TestClass]
public class TileBrushCalculatorTests
{
	#region Methods

	[PresentationTestMethod]
	public void NoTileFill1x()
	{
		var result = new TileBrushCalculator(
			TileMode.None,
			Stretch.Fill,
			AlignmentX.Center,
			AlignmentY.Center,
			RelativeRect.Fill,
			RelativeRect.Fill,
			new Size(100, 100),
			new Size(100, 100));

		CornerstoneTest.IsFalse(result.NeedsIntermediate);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), result.SourceRect);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), result.DestinationRect);
	}

	[PresentationTestMethod]
	public void NoTileFill2x()
	{
		var result = new TileBrushCalculator(
			TileMode.None,
			Stretch.Fill,
			AlignmentX.Center,
			AlignmentY.Center,
			RelativeRect.Fill,
			RelativeRect.Fill,
			new Size(100, 100),
			new Size(200, 200));

		// TODO: This doesn't need an intermediate render target.
		CornerstoneTest.IsTrue(result.NeedsIntermediate);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), result.SourceRect);
		CornerstoneTest.AreEqual(new Rect(0, 0, 200, 200), result.DestinationRect);
	}

	[PresentationTestMethod]
	public void NoTileNoStretchBottomRightQuarterDest()
	{
		var result = new TileBrushCalculator(
			TileMode.None,
			Stretch.None,
			AlignmentX.Center,
			AlignmentY.Center,
			RelativeRect.Fill,
			new RelativeRect(0.5, 0.5, 0.5, 0.5, RelativeUnit.Relative),
			new Size(800, 800),
			new Size(400, 400));

		CornerstoneTest.IsTrue(result.NeedsIntermediate);
		CornerstoneTest.AreEqual(new Rect(0, 0, 800, 800), result.SourceRect);
		CornerstoneTest.AreEqual(new Rect(200, 200, 200, 200), result.DestinationRect);
		CornerstoneTest.AreEqual(new Size(400, 400), result.IntermediateSize);
		CornerstoneTest.AreEqual(new Rect(200, 200, 200, 200), result.IntermediateClip);
		CornerstoneTest.AreEqual(Matrix.CreateTranslation(-100, -100), result.IntermediateTransform);
	}

	[PresentationTestMethod]
	public void NoTileUniformCenterHoriz()
	{
		var result = new TileBrushCalculator(
			TileMode.None,
			Stretch.Uniform,
			AlignmentX.Center,
			AlignmentY.Center,
			RelativeRect.Fill,
			RelativeRect.Fill,
			new Size(100, 100),
			new Size(200, 100));

		CornerstoneTest.IsTrue(result.NeedsIntermediate);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), result.SourceRect);
		CornerstoneTest.AreEqual(new Rect(0, 0, 200, 100), result.DestinationRect);
		CornerstoneTest.AreEqual(new Size(200, 100), result.IntermediateSize);
		CornerstoneTest.AreEqual(Matrix.CreateTranslation(50, 0), result.IntermediateTransform);
	}

	[PresentationTestMethod]
	public void NoTileUniformCenterVert()
	{
		var result = new TileBrushCalculator(
			TileMode.None,
			Stretch.Uniform,
			AlignmentX.Center,
			AlignmentY.Center,
			RelativeRect.Fill,
			RelativeRect.Fill,
			new Size(100, 100),
			new Size(100, 200));

		CornerstoneTest.IsTrue(result.NeedsIntermediate);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), result.SourceRect);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 200), result.DestinationRect);
		CornerstoneTest.AreEqual(new Size(100, 200), result.IntermediateSize);
		CornerstoneTest.AreEqual(Matrix.CreateTranslation(0, 50), result.IntermediateTransform);
	}

	[PresentationTestMethod]
	public void TileNoStretchBottomRightQuarterSourceCenterQuarterDest()
	{
		var result = new TileBrushCalculator(
			TileMode.Tile,
			Stretch.None,
			AlignmentX.Center,
			AlignmentY.Center,
			new RelativeRect(0.5, 0.5, 0.5, 0.5, RelativeUnit.Relative),
			new RelativeRect(0.25, 0.25, 0.5, 0.5, RelativeUnit.Relative),
			new Size(800, 800),
			new Size(400, 400));

		var b = new VisualBrush
		{
			TileMode = TileMode.Tile,
			Stretch = Stretch.None,
			SourceRect = new RelativeRect(0.5, 0.5, 0.5, 0.5, RelativeUnit.Relative),
			DestinationRect = new RelativeRect(0.25, 0.25, 0.5, 0.5, RelativeUnit.Relative),
			Visual = new Border { Width = 400, Height = 400 }
		};

		CornerstoneTest.IsTrue(result.NeedsIntermediate);
		CornerstoneTest.AreEqual(new Rect(400, 400, 400, 400), result.SourceRect);
		CornerstoneTest.AreEqual(new Rect(100, 100, 200, 200), result.DestinationRect);
		CornerstoneTest.AreEqual(new Size(200, 200), result.IntermediateSize);
		CornerstoneTest.AreEqual(new Rect(0, 0, 200, 200), result.IntermediateClip);
		CornerstoneTest.AreEqual(Matrix.CreateTranslation(-500, -500), result.IntermediateTransform);
	}

	#endregion
}