#region References

using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.Imaging;

[TestClass]
public class PixelFormatTranscoderTests
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldTranscode()
	{
		var sourceMemory = CreateBitmapMemory();

		var destMemory = new BitmapMemory(PixelFormat.Bgra8888, AlphaFormat.Opaque, sourceMemory.Size);

		PixelFormatTranscoder.Transcode(
			sourceMemory.Address,
			sourceMemory.Size,
			sourceMemory.RowBytes,
			sourceMemory.Format,
			sourceMemory.AlphaFormat,
			destMemory.Address,
			destMemory.RowBytes,
			destMemory.Format,
			destMemory.AlphaFormat);

		var reader = new PixelFormatReader.Bgra8888PixelFormatReader();

		reader.Reset(destMemory.Address);

		CornerstoneTest.AreEqual(new Rgba8888Pixel(255, 0, 0, 0), reader.ReadNext());
		CornerstoneTest.AreEqual(new Rgba8888Pixel(0, 255, 0, 0), reader.ReadNext());
		CornerstoneTest.AreEqual(new Rgba8888Pixel(0, 0, 255, 0), reader.ReadNext());
	}

	private BitmapMemory CreateBitmapMemory()
	{
		var bitmapMemory = new BitmapMemory(PixelFormat.Rgba8888, AlphaFormat.Opaque, new PixelSize(3, 1));

		var sourceWriter = new PixelFormatWriter.Rgba8888PixelFormatWriter();

		sourceWriter.Reset(bitmapMemory.Address);

		sourceWriter.WriteNext(new Rgba8888Pixel { R = 255 });
		sourceWriter.WriteNext(new Rgba8888Pixel { G = 255 });
		sourceWriter.WriteNext(new Rgba8888Pixel { B = 255 });

		return bitmapMemory;
	}

	#endregion
}