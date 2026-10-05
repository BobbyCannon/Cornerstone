#region References

using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media;

[TestClass]
public class BitmapSaveTests
{
	#region Methods

	[PresentationTestMethod]
	public void SaveWithInvalidJpegQualityThrows()
	{
		using var app = Start();

		using var bitmap = CreateBitmap(SKColors.Red, 16, 16);
		using var stream = new MemoryStream();
		var options = new JpegBitmapEncoderOptions { Quality = -1 };

		Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Save(stream, options));
	}

	[PresentationTestMethod]
	public void SaveWithInvalidPngCompressionLevelThrows()
	{
		using var app = Start();

		using var bitmap = CreateBitmap(SKColors.Red, 16, 16);
		using var stream = new MemoryStream();
		var options = new PngBitmapEncoderOptions { CompressionLevel = (CompressionLevel) 42 };

		Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Save(stream, options));
	}

	[PresentationTestMethod]
	public void SaveWithJpegOptionsProducesJpeg()
	{
		using var app = Start();

		using var bitmap = CreateBitmap(SKColors.Red, 16, 16);
		using var stream = new MemoryStream();

		bitmap.Save(stream, JpegBitmapEncoderOptions.Default);

		stream.Position = 0;
		using var codec = SKCodec.Create(stream);

		CornerstoneTest.AreEqual(SKEncodedImageFormat.Jpeg, codec.EncodedFormat);
	}

	[PresentationTestMethod]
	public void SaveWithNullOptionsThrows()
	{
		using var app = Start();

		using var bitmap = CreateBitmap(SKColors.Red, 16, 16);
		using var stream = new MemoryStream();

		Assert.Throws<ArgumentNullException>(() => bitmap.Save(stream, (BitmapEncoderOptions) null!));
	}

	[PresentationTestMethod]
	public void SaveWithPngOptionsProducesPng()
	{
		using var app = Start();

		using var bitmap = CreateBitmap(SKColors.Red, 16, 16);
		using var stream = new MemoryStream();

		bitmap.Save(stream, PngBitmapEncoderOptions.Default);

		stream.Position = 0;
		using var codec = SKCodec.Create(stream);

		CornerstoneTest.AreEqual(SKEncodedImageFormat.Png, codec.EncodedFormat);
	}

	private static WriteableBitmap CreateBitmap(SKColor color, int width, int height)
	{
		var pixel = (color.Alpha << 24) | (color.Red << 16) | (color.Green << 8) | color.Blue;

		var data = new int[width * height];
		data.AsSpan().Fill(pixel);

		return CreateBitmap(width, height, data);
	}

	private static WriteableBitmap CreateBitmap(int width, int height, int[] data)
	{
		var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

		using var fb = bitmap.Lock();

		for (var y = 0; y < height; y++)
		{
			Marshal.Copy(data, y * width, fb.Address + (y * fb.RowBytes), width);
		}

		return bitmap;
	}

	private static IDisposable Start()
	{
		return UnitTestApplication.Start(
			TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface()));
	}

	#endregion
}