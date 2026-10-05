#region References

using System;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.Imaging;

[TestClass]
public class PixelFormatWriterTests
{
	#region Fields

	private static readonly Rgba8888Pixel sblack = new()
	{
		A = 255,
		B = 0,
		G = 0,
		R = 0
	};

	private static readonly Rgba8888Pixel swhite = new()
	{
		A = 255,
		B = 255,
		G = 255,
		R = 255
	};

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void ShouldWriteBgr555()
	{
		var bitmapMemory = new BitmapMemory(
			new PixelFormat(PixelFormatEnum.Bgr555),
			AlphaFormat.Unpremul,
			new PixelSize(10, 10));

		var pixelWriter = new PixelFormatWriter.Bgr555PixelFormatWriter();
		var pixelReader = new PixelFormatReader.Bgr555PixelFormatReader();

		pixelWriter.Reset(bitmapMemory.Address);
		pixelReader.Reset(bitmapMemory.Address);

		pixelWriter.WriteNext(new Rgba8888Pixel { R = 255 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { R = 255, A = 255 }, pixelReader.ReadNext());

		pixelWriter.WriteNext(new Rgba8888Pixel { G = 255 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { G = 255, A = 255 }, pixelReader.ReadNext());

		pixelWriter.WriteNext(new Rgba8888Pixel { B = 255 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { B = 255, A = 255 }, pixelReader.ReadNext());
	}

	[PresentationTestMethod]
	public void ShouldWriteBgr565()
	{
		var bitmapMemory = new BitmapMemory(
			new PixelFormat(PixelFormatEnum.Bgr565),
			AlphaFormat.Unpremul,
			new PixelSize(10, 10));

		var pixelWriter = new PixelFormatWriter.Bgr565PixelFormatWriter();
		var pixelReader = new PixelFormatReader.Bgr565PixelFormatReader();

		pixelWriter.Reset(bitmapMemory.Address);
		pixelReader.Reset(bitmapMemory.Address);

		pixelWriter.WriteNext(new Rgba8888Pixel { R = 255 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { R = 255, A = 255 }, pixelReader.ReadNext());

		pixelWriter.WriteNext(new Rgba8888Pixel { G = 255 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { G = 255, A = 255 }, pixelReader.ReadNext());

		pixelWriter.WriteNext(new Rgba8888Pixel { B = 255 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { B = 255, A = 255 }, pixelReader.ReadNext());
	}

	[PresentationTestMethod]
	public void ShouldWriteBgra8888()
	{
		var sourceMemory = new BitmapMemory(
			new PixelFormat(PixelFormatEnum.Bgra8888),
			AlphaFormat.Unpremul,
			new PixelSize(3, 1));

		var sourceWriter = new PixelFormatWriter.Bgra8888PixelFormatWriter();
		var sourceReader = new PixelFormatReader.Bgra8888PixelFormatReader();

		sourceWriter.Reset(sourceMemory.Address);
		sourceReader.Reset(sourceMemory.Address);

		sourceWriter.WriteNext(new Rgba8888Pixel { R = 255 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { R = 255 }, sourceReader.ReadNext());

		sourceWriter.WriteNext(new Rgba8888Pixel { G = 255 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { G = 255 }, sourceReader.ReadNext());

		sourceWriter.WriteNext(new Rgba8888Pixel { B = 255 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { B = 255 }, sourceReader.ReadNext());
	}

	[PresentationTestMethod]
	public void ShouldWriteBlackWhite()
	{
		var bitmapMemory = new BitmapMemory(
			new PixelFormat(PixelFormatEnum.BlackWhite),
			AlphaFormat.Unpremul,
			new PixelSize(10, 10));

		var pixelWriter = new PixelFormatWriter.BlackWhitePixelFormatWriter();
		var pixelReader = new PixelFormatReader.BlackWhitePixelFormatReader();

		pixelWriter.Reset(bitmapMemory.Address);
		pixelReader.Reset(bitmapMemory.Address);

		pixelWriter.WriteNext(swhite);
		CornerstoneTest.AreEqual(swhite, pixelReader.ReadNext());

		pixelWriter.WriteNext(sblack);
		CornerstoneTest.AreEqual(sblack, pixelReader.ReadNext());
	}

	[PresentationTestMethod]
	public void ShouldWriteGray16()
	{
		var bitmapMemory = new BitmapMemory(
			new PixelFormat(PixelFormatEnum.Gray16),
			AlphaFormat.Unpremul,
			new PixelSize(10, 10));

		var pixelWriter = new PixelFormatWriter.Gray16PixelFormatWriter();
		var pixelReader = new PixelFormatReader.Gray16PixelFormatReader();

		pixelWriter.Reset(bitmapMemory.Address);
		pixelReader.Reset(bitmapMemory.Address);

		pixelWriter.WriteNext(new Rgba8888Pixel { R = 255 });
		CornerstoneTest.AreEqual(GetGray16(new Rgba8888Pixel { R = 255 }), pixelReader.ReadNext());

		pixelWriter.WriteNext(new Rgba8888Pixel { R = 120 });
		CornerstoneTest.AreEqual(GetGray16(new Rgba8888Pixel { R = 120 }), pixelReader.ReadNext());

		pixelWriter.WriteNext(new Rgba8888Pixel());
		CornerstoneTest.AreEqual(GetGray16(new Rgba8888Pixel { A = 255 }), pixelReader.ReadNext());
	}

	[PresentationTestMethod]
	public void ShouldWriteGray2()
	{
		var palette = new[]
		{
			sblack,
			new Rgba8888Pixel
			{
				A = 255, B = 0x55, G = 0x55, R = 0x55
			},
			new Rgba8888Pixel
			{
				A = 255, B = 0xAA, G = 0xAA, R = 0xAA
			},
			swhite
		};

		var bitmapMemory = new BitmapMemory(
			new PixelFormat(PixelFormatEnum.Gray2),
			AlphaFormat.Unpremul,
			new PixelSize(10, 10));

		var pixelWriter = new PixelFormatWriter.Gray2PixelFormatWriter();
		var pixelReader = new PixelFormatReader.Gray2PixelFormatReader();

		pixelWriter.Reset(bitmapMemory.Address);
		pixelReader.Reset(bitmapMemory.Address);

		pixelWriter.WriteNext(palette[0]);
		CornerstoneTest.AreEqual(palette[0], pixelReader.ReadNext());

		pixelWriter.WriteNext(palette[1]);
		CornerstoneTest.AreEqual(palette[1], pixelReader.ReadNext());

		pixelWriter.WriteNext(palette[2]);
		CornerstoneTest.AreEqual(palette[2], pixelReader.ReadNext());

		pixelWriter.WriteNext(palette[3]);
		CornerstoneTest.AreEqual(palette[3], pixelReader.ReadNext());
	}

	[PresentationTestMethod]
	public void ShouldWriteGray32Float()
	{
		var bitmapMemory = new BitmapMemory(
			new PixelFormat(PixelFormatEnum.Gray32Float),
			AlphaFormat.Unpremul,
			new PixelSize(10, 10));

		var pixelWriter = new PixelFormatWriter.Gray32FloatPixelFormatWriter();
		var pixelReader = new PixelFormatReader.Gray32FloatPixelFormatReader();

		pixelWriter.Reset(bitmapMemory.Address);
		pixelReader.Reset(bitmapMemory.Address);

		pixelWriter.WriteNext(new Rgba8888Pixel { R = 255 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { R = 255, G = 255, B = 255, A = 255 }, pixelReader.ReadNext());

		pixelWriter.WriteNext(new Rgba8888Pixel { R = 125 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { R = 125, G = 125, B = 125, A = 255 }, pixelReader.ReadNext());

		pixelWriter.WriteNext(new Rgba8888Pixel());
		CornerstoneTest.AreEqual(new Rgba8888Pixel { A = 255 }, pixelReader.ReadNext());
	}

	[PresentationTestMethod]
	public void ShouldWriteGray4()
	{
		var bitmapMemory = new BitmapMemory(
			new PixelFormat(PixelFormatEnum.Gray4),
			AlphaFormat.Unpremul,
			new PixelSize(10, 10));

		var pixelWriter = new PixelFormatWriter.Gray4PixelFormatWriter();
		var pixelReader = new PixelFormatReader.Gray4PixelFormatReader();

		pixelWriter.Reset(bitmapMemory.Address);
		pixelReader.Reset(bitmapMemory.Address);

		pixelWriter.WriteNext(new Rgba8888Pixel { R = 255 });
		CornerstoneTest.AreEqual(GetGray4(new Rgba8888Pixel { R = 255 }), pixelReader.ReadNext());

		pixelWriter.WriteNext(new Rgba8888Pixel { R = 17 });
		CornerstoneTest.AreEqual(GetGray4(new Rgba8888Pixel { R = 17 }), pixelReader.ReadNext());

		pixelWriter.WriteNext(new Rgba8888Pixel());
		CornerstoneTest.AreEqual(new Rgba8888Pixel { A = 255 }, pixelReader.ReadNext());
	}

	[PresentationTestMethod]
	public void ShouldWriteGray8()
	{
		var bitmapMemory = new BitmapMemory(
			new PixelFormat(PixelFormatEnum.Gray8),
			AlphaFormat.Unpremul,
			new PixelSize(10, 10));

		var pixelWriter = new PixelFormatWriter.Gray8PixelFormatWriter();
		var pixelReader = new PixelFormatReader.Gray8PixelFormatReader();

		pixelWriter.Reset(bitmapMemory.Address);
		pixelReader.Reset(bitmapMemory.Address);

		pixelWriter.WriteNext(new Rgba8888Pixel { R = 255 });
		CornerstoneTest.AreEqual(GetGray8(new Rgba8888Pixel { R = 255 }), pixelReader.ReadNext());

		pixelWriter.WriteNext(new Rgba8888Pixel { R = 120 });
		CornerstoneTest.AreEqual(GetGray8(new Rgba8888Pixel { R = 120 }), pixelReader.ReadNext());

		pixelWriter.WriteNext(new Rgba8888Pixel());
		CornerstoneTest.AreEqual(GetGray8(new Rgba8888Pixel { A = 255 }), pixelReader.ReadNext());
	}

	[PresentationTestMethod]
	public void ShouldWriteRgb24()
	{
		var sourceMemory = new BitmapMemory(
			new PixelFormat(PixelFormatEnum.Rgb24),
			AlphaFormat.Unpremul,
			new PixelSize(10, 10));

		var pixelWriter = new PixelFormatWriter.Rgb24PixelFormatWriter();
		var pixelReader = new PixelFormatReader.Rgb24PixelFormatReader();

		pixelWriter.Reset(sourceMemory.Address);
		pixelReader.Reset(sourceMemory.Address);

		pixelWriter.WriteNext(new Rgba8888Pixel { R = 255, G = 125, B = 125 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { R = 255, G = 125, B = 125, A = 255 }, pixelReader.ReadNext());

		pixelWriter.WriteNext(new Rgba8888Pixel { R = 125, G = 255, B = 125 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { R = 125, G = 255, B = 125, A = 255 }, pixelReader.ReadNext());

		pixelWriter.WriteNext(new Rgba8888Pixel { R = 125, G = 125, B = 255 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { R = 125, G = 125, B = 255, A = 255 }, pixelReader.ReadNext());
	}

	[PresentationTestMethod]
	public void ShouldWriteRgba64()
	{
		var sourceMemory = new BitmapMemory(
			new PixelFormat(PixelFormatEnum.Rgba64),
			AlphaFormat.Unpremul,
			new PixelSize(10, 10));

		var pixelWriter = new PixelFormatWriter.Rgba64PixelFormatWriter();
		var pixelReader = new PixelFormatReader.Rgba64PixelFormatReader();

		pixelWriter.Reset(sourceMemory.Address);
		pixelReader.Reset(sourceMemory.Address);

		pixelWriter.WriteNext(new Rgba8888Pixel { R = 255, G = 125, B = 125, A = 125 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { R = 255, G = 125, B = 125, A = 125 }, pixelReader.ReadNext());

		pixelWriter.WriteNext(new Rgba8888Pixel { R = 125, G = 255, B = 125, A = 125 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { R = 125, G = 255, B = 125, A = 125 }, pixelReader.ReadNext());

		pixelWriter.WriteNext(new Rgba8888Pixel { R = 125, G = 125, B = 255, A = 125 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { R = 125, G = 125, B = 255, A = 125 }, pixelReader.ReadNext());
	}

	[PresentationTestMethod]
	public void ShouldWriteRgba8888()
	{
		var sourceMemory = new BitmapMemory(
			new PixelFormat(PixelFormatEnum.Rgba8888),
			AlphaFormat.Unpremul,
			new PixelSize(10, 10));

		var pixelWriter = new PixelFormatWriter.Rgba8888PixelFormatWriter();
		var pixelReader = new PixelFormatReader.Rgba8888PixelFormatReader();

		pixelWriter.Reset(sourceMemory.Address);
		pixelReader.Reset(sourceMemory.Address);

		pixelWriter.WriteNext(new Rgba8888Pixel { R = 255, G = 125, B = 125, A = 125 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { R = 255, G = 125, B = 125, A = 125 }, pixelReader.ReadNext());

		pixelWriter.WriteNext(new Rgba8888Pixel { R = 125, G = 255, B = 125, A = 125 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { R = 125, G = 255, B = 125, A = 125 }, pixelReader.ReadNext());

		pixelWriter.WriteNext(new Rgba8888Pixel { R = 125, G = 125, B = 255, A = 125 });
		CornerstoneTest.AreEqual(new Rgba8888Pixel { R = 125, G = 125, B = 255, A = 125 }, pixelReader.ReadNext());
	}

	private static Rgba8888Pixel GetGray16(Rgba8888Pixel pixel)
	{
		var grayscale = (ushort) Math.Round(((0.299F * pixel.R) + (0.587F * pixel.G) + (0.114F * pixel.B)) * 0x0101);

		var value = (byte) (grayscale >> 8);

		return new Rgba8888Pixel(value, value, value, 255);
	}

	private static Rgba8888Pixel GetGray4(Rgba8888Pixel pixel)
	{
		var grayscale = (byte) Math.Round((0.299F * pixel.R) + (0.587F * pixel.G) + (0.114F * pixel.B));

		var value = (byte) ((grayscale / 255F) * 0xF);

		value = (byte) (value | (value << 4));

		return new Rgba8888Pixel(value, value, value, 255);
	}

	private static Rgba8888Pixel GetGray8(Rgba8888Pixel pixel)
	{
		var value = (byte) Math.Round((0.299F * pixel.R) + (0.587F * pixel.G) + (0.114F * pixel.B));

		return new Rgba8888Pixel(value, value, value, 255);
	}

	#endregion
}