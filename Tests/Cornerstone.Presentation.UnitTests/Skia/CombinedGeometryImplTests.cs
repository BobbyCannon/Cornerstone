#region References

using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia;

[TestClass]
public class CombinedGeometryImplTests
{
	#region Methods

	[PresentationTestMethod]
	public void CombiningFillWithEmptyStrokeReturnsFillBounds()
	{
		var fill = new SKPath();
		fill.LineTo(100, 0);
		fill.LineTo(100, 100);
		fill.LineTo(0, 100);
		fill.Close();

		var stroke = new SKPath();

		var result = new CombinedGeometryImpl(stroke, fill);

		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), result.Bounds);
	}

	#endregion
}