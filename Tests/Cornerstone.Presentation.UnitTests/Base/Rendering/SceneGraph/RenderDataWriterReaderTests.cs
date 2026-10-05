#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Rendering.Composition.Drawing;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Rendering.SceneGraph;

[TestClass]
public class RenderDataWriterReaderTests
{
	#region Methods

	[PresentationTestMethod]
	public void ColorAndBoxShadowRoundTrip()
	{
		var color = Color.FromArgb(10, 20, 30, 40);
		var shadow = new BoxShadow
		{
			OffsetX = 1.5,
			OffsetY = -2.5,
			Blur = 3,
			Spread = 4,
			Color = Color.FromArgb(255, 1, 2, 3),
			IsInset = true
		};

		var writer = new RenderDataWriter();
		try
		{
			writer.Write(color);
			writer.Write(shadow);

			var reader = new RenderDataReader(writer.Written);
			CornerstoneTest.AreEqual(color, reader.Read<Color>());

			var readShadow = reader.Read<BoxShadow>();
			CornerstoneTest.AreEqual(shadow.OffsetX, readShadow.OffsetX);
			CornerstoneTest.AreEqual(shadow.OffsetY, readShadow.OffsetY);
			CornerstoneTest.AreEqual(shadow.Blur, readShadow.Blur);
			CornerstoneTest.AreEqual(shadow.Spread, readShadow.Spread);
			CornerstoneTest.AreEqual(shadow.Color, readShadow.Color);
			CornerstoneTest.AreEqual(shadow.IsInset, readShadow.IsInset);
			CornerstoneTest.IsTrue(reader.IsAtEnd);
		}
		finally
		{
			writer.Dispose();
		}
	}

	[PresentationTestMethod]
	public void GeometricStructsRoundTrip()
	{
		var point = new Point(1, 2);
		var vector = new Vector(3, 4);
		var rect = new Rect(5, 6, 7, 8);
		var roundedRect = new RoundedRect(new Rect(1, 2, 30, 40),
			new Vector(1, 1), new Vector(2, 2), new Vector(3, 3), new Vector(4, 4));
		var matrix = new Matrix(1, 2, 3, 4, 5, 6, 7, 8, 9);

		var writer = new RenderDataWriter();
		try
		{
			writer.Write(point);
			writer.Write(vector);
			writer.Write(rect);
			writer.Write(roundedRect);
			writer.Write(matrix);

			var reader = new RenderDataReader(writer.Written);
			CornerstoneTest.AreEqual(point, reader.Read<Point>());
			CornerstoneTest.AreEqual(vector, reader.Read<Vector>());
			CornerstoneTest.AreEqual(rect, reader.Read<Rect>());

			var readRounded = reader.Read<RoundedRect>();
			CornerstoneTest.AreEqual(roundedRect.Rect, readRounded.Rect);
			CornerstoneTest.AreEqual(roundedRect.RadiiTopLeft, readRounded.RadiiTopLeft);
			CornerstoneTest.AreEqual(roundedRect.RadiiTopRight, readRounded.RadiiTopRight);
			CornerstoneTest.AreEqual(roundedRect.RadiiBottomRight, readRounded.RadiiBottomRight);
			CornerstoneTest.AreEqual(roundedRect.RadiiBottomLeft, readRounded.RadiiBottomLeft);

			CornerstoneTest.AreEqual(matrix, reader.Read<Matrix>());
			CornerstoneTest.IsTrue(reader.IsAtEnd);
		}
		finally
		{
			writer.Dispose();
		}
	}

	[PresentationTestMethod]
	public void LengthTracksWrittenBytes()
	{
		var writer = new RenderDataWriter();
		try
		{
			CornerstoneTest.AreEqual(0, writer.Length);
			writer.Write<byte>(1);
			CornerstoneTest.AreEqual(1, writer.Length);
			writer.Write(2);
			CornerstoneTest.AreEqual(5, writer.Length);
			writer.Write(3d);
			CornerstoneTest.AreEqual(13, writer.Length);
		}
		finally
		{
			writer.Dispose();
		}
	}

	[PresentationTestMethod]
	[DataRow(null)]
	[DataRow(true)]
	[DataRow(false)]
	public void NullableBooleanRoundTripsAllThreeStates(bool? value)
	{
		var writer = new RenderDataWriter();
		try
		{
			writer.Write(new RenderOptions { RequiresFullOpacityHandling = value });

			var reader = new RenderDataReader(writer.Written);
			CornerstoneTest.AreEqual(value, reader.Read<RenderOptions>().RequiresFullOpacityHandling);
		}
		finally
		{
			writer.Dispose();
		}
	}

	[PresentationTestMethod]
	public void PayloadAutoPrependsOpcode()
	{
		var writer = new RenderDataWriter();
		try
		{
			writer.WritePayload(new PushOpacityPayload { Opacity = 0.5 });

			var reader = new RenderDataReader(writer.Written);
			CornerstoneTest.AreEqual(RenderDataOpcode.PushOpacity, reader.Read<RenderDataOpcode>());
			var payload = reader.Read<PushOpacityPayload>();
			CornerstoneTest.AreEqual(0.5, payload.Opacity);
			CornerstoneTest.IsTrue(reader.IsAtEnd);
		}
		finally
		{
			writer.Dispose();
		}
	}

	[PresentationTestMethod]
	public void PrimitivesRoundTrip()
	{
		var writer = new RenderDataWriter();
		try
		{
			writer.Write<byte>(200);
			writer.WriteOpcode(RenderDataOpcode.DrawGeometry);
			writer.Write(-123456);
			writer.Write(4000000000u);
			writer.Write(3.14159);
			writer.Write(true);
			writer.Write(false);

			var reader = new RenderDataReader(writer.Written);
			CornerstoneTest.AreEqual(200, reader.Read<byte>());
			CornerstoneTest.AreEqual(RenderDataOpcode.DrawGeometry, reader.Read<RenderDataOpcode>());
			CornerstoneTest.AreEqual(-123456, reader.Read<int>());
			CornerstoneTest.AreEqual(4000000000u, reader.Read<uint>());
			CornerstoneTest.AreEqual(3.14159, reader.Read<double>());
			CornerstoneTest.IsTrue(reader.Read<bool>());
			CornerstoneTest.IsFalse(reader.Read<bool>());
			CornerstoneTest.IsTrue(reader.IsAtEnd);
		}
		finally
		{
			writer.Dispose();
		}
	}

	[PresentationTestMethod]
	public void RenderOptionsAndTextOptionsRoundTrip()
	{
		var renderOptions = new RenderOptions
		{
			#pragma warning disable CS0618
			TextRenderingMode = TextRenderingMode.Antialias,
			#pragma warning restore CS0618
			BitmapInterpolationMode = BitmapInterpolationMode.HighQuality,
			EdgeMode = EdgeMode.Aliased,
			BitmapBlendingMode = BitmapBlendingMode.Plus,
			RequiresFullOpacityHandling = true
		};
		var textOptions = new TextOptions
		{
			TextRenderingMode = TextRenderingMode.SubpixelAntialias,
			TextHintingMode = TextHintingMode.Light,
			BaselinePixelAlignment = BaselinePixelAlignment.Aligned
		};

		var writer = new RenderDataWriter();
		try
		{
			writer.Write(renderOptions);
			writer.Write(textOptions);

			var reader = new RenderDataReader(writer.Written);
			CornerstoneTest.AreEqual(renderOptions, reader.Read<RenderOptions>());
			CornerstoneTest.AreEqual(textOptions, reader.Read<TextOptions>());
			CornerstoneTest.IsTrue(reader.IsAtEnd);
		}
		finally
		{
			writer.Dispose();
		}
	}

	[PresentationTestMethod]
	public void WriterGrowsBufferToFitLargePayloads()
	{
		var writer = new RenderDataWriter();
		try
		{
			for (var i = 0; i < 1000; i++)
			{
				writer.Write(i);
			}

			CornerstoneTest.AreEqual(4000, writer.Length);

			var reader = new RenderDataReader(writer.Written);
			for (var i = 0; i < 1000; i++)
			{
				CornerstoneTest.AreEqual(i, reader.Read<int>());
			}
			CornerstoneTest.IsTrue(reader.IsAtEnd);
		}
		finally
		{
			writer.Dispose();
		}
	}

	#endregion
}