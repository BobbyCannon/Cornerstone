#region References

using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class PixelRectTests
{
	#region Methods

	[PresentationTestMethod]
	public void FromRectSnapsToDevicePixels()
	{
		var rect = new Rect(189, 189, 26, 164);
		var result = PixelRect.FromRect(rect, 1.5);

		CornerstoneTest.AreEqual(new PixelRect(283, 283, 40, 247), result);
	}

	[PresentationTestMethod]
	public void FromRectVectorSnapsToDevicePixels()
	{
		var rect = new Rect(189, 189, 26, 164);
		var result = PixelRect.FromRect(rect, new Vector(1.5, 1.5));

		CornerstoneTest.AreEqual(new PixelRect(283, 283, 40, 247), result);
	}

	[PresentationTestMethod]
	public void FromRectWithDpiSnapsToDevicePixels()
	{
		var rect = new Rect(189, 189, 26, 164);
		var result = PixelRect.FromRectWithDpi(rect, 144);

		CornerstoneTest.AreEqual(new PixelRect(283, 283, 40, 247), result);
	}

	[PresentationTestMethod]
	public void FromRectWithDpiVectorSnapsToDevicePixels()
	{
		var rect = new Rect(189, 189, 26, 164);
		var result = PixelRect.FromRectWithDpi(rect, new Vector(144, 144));

		CornerstoneTest.AreEqual(new PixelRect(283, 283, 40, 247), result);
	}

	#endregion
}