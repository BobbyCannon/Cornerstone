#region References

using System;
using System.Runtime.InteropServices;
using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media;

[TestClass]
public class ImmutableBitmapTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(1, 1, false)]
	[DataRow(3, 5, false)]
	[DataRow(64, 64, false)]
	[DataRow(1, 1, true)]
	[DataRow(3, 5, true)]
	[DataRow(64, 64, true)]
	public unsafe void ConstructorFromPixelsCopiesSourceData(int width, int height, bool negativeStride)
	{
		var size = new PixelSize(width, height);
		var rowBytes = width * 4;
		var absStride = rowBytes;
		var byteSize = absStride * height;

		// Logical pixel byte: deterministic function of (row, byteIndexWithinRow).
		byte Expected(int row, int x)
		{
			return (byte) ((((row * rowBytes) + x) * 7) + 1);
		}

		var source = Marshal.AllocHGlobal(byteSize);
		try
		{
			// Lay the logical rows out in physical memory. For a negative stride the rows are stored
			// bottom-up and the data pointer addresses the first (top) logical row, which sits at the
			// highest address.
			var buffer = new Span<byte>((void*) source, byteSize);
			for (var row = 0; row < height; row++)
			{
				var physicalRow = negativeStride ? height - 1 - row : row;
				for (var x = 0; x < rowBytes; x++)
				{
					buffer[(physicalRow * absStride) + x] = Expected(row, x);
				}
			}

			var stride = negativeStride ? -absStride : absStride;
			var data = negativeStride ? source + (absStride * (height - 1)) : source;

			using var bitmap = new ImmutableBitmap(
				size, new Vector(96, 96), stride,
				PixelFormat.Bgra8888, AlphaFormat.Premul, data);

			// The constructor must take its own copy: corrupting (and freeing) the source
			// afterwards must not affect the bitmap's pixels.
			buffer.Fill(0xCD);

			CornerstoneTest.AreEqual(size, bitmap.PixelSize);

			using var locked = bitmap.Lock();
			CornerstoneTest.AreEqual(size, locked.Size);

			var dst = new ReadOnlySpan<byte>((void*) locked.Address, locked.RowBytes * height);
			for (var row = 0; row < height; row++)
			for (var x = 0; x < rowBytes; x++)
			{
				CornerstoneTest.AreEqual(Expected(row, x), dst[(row * locked.RowBytes) + x]);
			}
		}
		finally
		{
			Marshal.FreeHGlobal(source);
		}
	}

	#endregion
}