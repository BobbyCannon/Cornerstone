#region References

using System;
using System.Collections.Immutable;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Threading;
using Dispatcher = Cornerstone.Presentation.Threading.Dispatcher;
using Cornerstone.RemoteLink.Vnc.Client.Rendering;
using Cornerstone.RemoteLink.Vnc.Controls.Adapters.Rendering;
using IRenderTarget = Cornerstone.RemoteLink.Vnc.Client.Rendering.IRenderTarget;
using PixelFormat = Cornerstone.Presentation.Platform.PixelFormat;
using Screen = Cornerstone.RemoteLink.Vnc.Client.Screen;
using Size = Cornerstone.RemoteLink.Vnc.Client.Size;

#endregion

namespace Cornerstone.RemoteLink.Vnc.Controls;

/// <summary>
/// Connection-owned framebuffer so the last frame survives view detach/reattach.
/// </summary>
public sealed class VncFramebuffer : IRenderTarget, IDisposable
{
	#region Fields

	private volatile WriteableBitmap _bitmap;
	private readonly object _bitmapReplacementLock = new();
	private volatile bool _disposed;

	#endregion

	#region Events

	public event EventHandler<bool> Updated;

	#endregion

	#region Properties

	public global::Cornerstone.Presentation.Size? BitmapSize
	{
		get
		{
			lock (_bitmapReplacementLock)
			{
				return _bitmap?.Size;
			}
		}
	}

	#endregion

	#region Methods

	public void Clear()
	{
		lock (_bitmapReplacementLock)
		{
			_bitmap?.Dispose();
			_bitmap = null;
		}

		Updated?.Invoke(this, true);
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		Clear();
		_disposed = true;
	}

	public void Draw(DrawingContext context, global::Cornerstone.Presentation.Size boundsSize)
	{
		if (context == null)
		{
			throw new ArgumentNullException(nameof(context));
		}

		lock (_bitmapReplacementLock)
		{
			if (_bitmap == null)
			{
				return;
			}

			var source = new Rect(_bitmap.Size);
			var destination = GetScaledImageRect(_bitmap.Size, boundsSize);
			if ((destination.Width <= 0) || (destination.Height <= 0))
			{
				return;
			}

			context.DrawImage(_bitmap, source, destination);
		}
	}

	public IFramebufferReference GrabFramebufferReference(Size size, IImmutableSet<Screen> layout)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(nameof(VncFramebuffer));
		}

		var requiredPixelSize = Conversions.GetPixelSize(size);
		var sizeChanged = (_bitmap == null) || (_bitmap.PixelSize != requiredPixelSize);

		WriteableBitmap bitmap;
		if (sizeChanged)
		{
			bitmap = new WriteableBitmap(requiredPixelSize, new Vector(96.0f, 96.0f), PixelFormat.Bgra8888, AlphaFormat.Opaque);
			lock (_bitmapReplacementLock)
			{
				_bitmap?.Dispose();
				_bitmap = bitmap;
			}
		}
		else
		{
			bitmap = _bitmap;
		}

		var lockedFramebuffer = bitmap.Lock();
		return new PresentationFramebufferReference(lockedFramebuffer, () => Dispatcher.UIThread.Post(() =>
		{
			Updated?.Invoke(this, sizeChanged);
		}));
	}

	public bool TryMapViewPoint(Point viewPoint, global::Cornerstone.Presentation.Size boundsSize, out Client.Position framebufferPosition)
	{
		framebufferPosition = default;
		PixelSize pixelSize;
		global::Cornerstone.Presentation.Size bitmapSize;
		lock (_bitmapReplacementLock)
		{
			if (_bitmap == null)
			{
				return false;
			}

			pixelSize = _bitmap.PixelSize;
			bitmapSize = _bitmap.Size;
		}

		var destination = GetScaledImageRect(bitmapSize, boundsSize);
		if ((destination.Width <= 0) || (destination.Height <= 0) || !destination.Contains(viewPoint))
		{
			return false;
		}

		var x = (viewPoint.X - destination.X) / destination.Width * pixelSize.Width;
		var y = (viewPoint.Y - destination.Y) / destination.Height * pixelSize.Height;
		framebufferPosition = new Client.Position((int)x, (int)y);
		return true;
	}

	internal static Rect GetScaledImageRect(global::Cornerstone.Presentation.Size sourceSize, global::Cornerstone.Presentation.Size boundsSize)
	{
		if ((sourceSize.Width <= 0) || (sourceSize.Height <= 0) || (boundsSize.Width <= 0) || (boundsSize.Height <= 0))
		{
			return default;
		}

		var scale = Math.Min(boundsSize.Width / sourceSize.Width, boundsSize.Height / sourceSize.Height);
		var width = sourceSize.Width * scale;
		var height = sourceSize.Height * scale;
		var x = (boundsSize.Width - width) / 2;
		var y = (boundsSize.Height - height) / 2;
		return new Rect(x, y, width, height);
	}

	#endregion
}
