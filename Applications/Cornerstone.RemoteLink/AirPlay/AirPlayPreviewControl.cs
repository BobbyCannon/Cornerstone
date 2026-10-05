#region References

using System;
using System.Runtime.InteropServices;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Threading;
using Dispatcher = Cornerstone.Presentation.Threading.Dispatcher;
using DispatcherPriority = Cornerstone.Presentation.DispatcherPriority;
using Cornerstone.Presentation.Theme;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.RemoteLink.AirPlay;

/// <summary>
/// Draws mirrored frames in Render. Cornerstone Image caches WriteableBitmap and will not
/// show in-place pixel updates.
/// </summary>
public sealed class AirPlayPreviewControl : Control
{
	#region Fields

	private WriteableBitmap _bitmap;
	private bool _needsUpdate;
	private int _pixelHeight;
	private int _pixelWidth;
	private byte[] _pixels;

	#endregion

	#region Constructors

	public AirPlayPreviewControl()
	{
		ClipToBounds = true;
		this.GetObservable(BoundsProperty).Subscribe(_ => InvalidateVisual());
	}

	#endregion

	#region Methods

	public void Clear()
	{
		_pixels = null;
		_pixelWidth = 0;
		_pixelHeight = 0;
		_needsUpdate = false;
		_bitmap?.Dispose();
		_bitmap = null;
		Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
	}

	public override void Render(DrawingContext context)
	{
		if ((_pixels == null) || (_pixelWidth <= 0) || (_pixelHeight <= 0))
		{
			return;
		}

		if ((_bitmap == null)
			|| (_bitmap.PixelSize.Width != _pixelWidth)
			|| (_bitmap.PixelSize.Height != _pixelHeight))
		{
			_bitmap?.Dispose();
			_bitmap = new WriteableBitmap(
				new PixelSize(_pixelWidth, _pixelHeight),
				new Vector(96, 96),
				PixelFormat.Bgra8888,
				AlphaFormat.Opaque);
			_needsUpdate = true;
		}

		if (_needsUpdate)
		{
			using (var framebuffer = _bitmap.Lock())
			{
				var sourceStride = _pixelWidth * 4;
				var destinationStride = framebuffer.RowBytes;
				if (destinationStride == sourceStride)
				{
					Marshal.Copy(_pixels, 0, framebuffer.Address, sourceStride * _pixelHeight);
				}
				else
				{
					for (var y = 0; y < _pixelHeight; y++)
					{
						Marshal.Copy(_pixels, y * sourceStride, framebuffer.Address + (y * destinationStride), sourceStride);
					}
				}
			}

			_needsUpdate = false;
		}

		var bounds = Bounds;
		if ((bounds.Width <= 0) || (bounds.Height <= 0))
		{
			return;
		}

		var scale = Math.Min(bounds.Width / _pixelWidth, bounds.Height / _pixelHeight);
		var scaledWidth = _pixelWidth * scale;
		var scaledHeight = _pixelHeight * scale;
		var destination = new Rect(
			(bounds.Width - scaledWidth) / 2,
			(bounds.Height - scaledHeight) / 2,
			scaledWidth,
			scaledHeight);
		context.DrawImage(_bitmap, new Rect(0, 0, _pixelWidth, _pixelHeight), destination);
	}

	public bool TryMapToNormalized(Point point, out double x, out double y)
	{
		x = 0;
		y = 0;
		if ((_pixelWidth <= 0) || (_pixelHeight <= 0))
		{
			return false;
		}

		var bounds = Bounds;
		if ((bounds.Width <= 0) || (bounds.Height <= 0))
		{
			return false;
		}

		var scale = Math.Min(bounds.Width / _pixelWidth, bounds.Height / _pixelHeight);
		var scaledWidth = _pixelWidth * scale;
		var scaledHeight = _pixelHeight * scale;
		if ((scaledWidth <= 0) || (scaledHeight <= 0))
		{
			return false;
		}

		var left = (bounds.Width - scaledWidth) / 2;
		var top = (bounds.Height - scaledHeight) / 2;
		x = (point.X - left) / scaledWidth;
		y = (point.Y - top) / scaledHeight;
		return (x >= 0) && (x <= 1) && (y >= 0) && (y <= 1);
	}

	public void SetFrame(byte[] bgra, int width, int height)
	{
		if ((bgra == null) || (width <= 0) || (height <= 0))
		{
			Clear();
			return;
		}

		var needed = width * height * 4;
		if (bgra.Length < needed)
		{
			return;
		}

		_pixels = bgra;
		_pixelWidth = width;
		_pixelHeight = height;
		_needsUpdate = true;
		Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
	}

	#endregion
}