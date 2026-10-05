#region References

using System;
using System.Collections.Immutable;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Media;
using Cornerstone.RemoteLink.Vnc.Client.Rendering;
using Screen = Cornerstone.RemoteLink.Vnc.Client.Screen;
using Size = Cornerstone.RemoteLink.Vnc.Client.Size;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.RemoteLink.Vnc.Controls;

/// <summary>
/// A control that provides access to a target framebuffer for rendering frames onto it.
/// </summary>
public class RfbRenderTarget : Control, IRenderTarget, IDisposable
{
	#region Fields

	private VncFramebuffer _attachedFramebuffer;
	private volatile bool _disposed;
	private readonly VncFramebuffer _ownedFramebuffer;

	#endregion

	#region Constructors

	public RfbRenderTarget()
	{
		_ownedFramebuffer = new VncFramebuffer();
		_ownedFramebuffer.Updated += OnFramebufferUpdated;
	}

	#endregion

	#region Properties

	protected VncFramebuffer ActiveFramebuffer => _attachedFramebuffer ?? _ownedFramebuffer;

	#endregion

	#region Methods

	public void AttachFramebuffer(VncFramebuffer framebuffer)
	{
		if (ReferenceEquals(_attachedFramebuffer, framebuffer))
		{
			return;
		}

		if (_attachedFramebuffer != null)
		{
			_attachedFramebuffer.Updated -= OnFramebufferUpdated;
		}

		_attachedFramebuffer = framebuffer;
		if (_attachedFramebuffer != null)
		{
			_attachedFramebuffer.Updated += OnFramebufferUpdated;
		}

		InvalidateMeasure();
		InvalidateVisual();
	}

	/// <inheritdoc />
	public void Dispose()
	{
		Dispose(true);
	}

	/// <inheritdoc />
	public virtual IFramebufferReference GrabFramebufferReference(Size size, IImmutableSet<Screen> layout)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(nameof(RfbRenderTarget));
		}

		return ActiveFramebuffer.GrabFramebufferReference(size, layout);
	}

	/// <inheritdoc />
	public override void Render(DrawingContext context)
	{
		ActiveFramebuffer.Draw(context, Bounds.Size);
	}

	/// <summary>
	/// Maps a point in view coordinates to a framebuffer pixel, or returns false when the point is in the letterbox.
	/// </summary>
	protected bool TryMapViewPointToFramebuffer(Point viewPoint, out Client.Position framebufferPosition)
	{
		return ActiveFramebuffer.TryMapViewPoint(viewPoint, Bounds.Size, out framebufferPosition);
	}

	protected void ClearFramebuffer()
	{
		ActiveFramebuffer.Clear();
		InvalidateMeasure();
		InvalidateVisual();
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}

		if (disposing)
		{
			AttachFramebuffer(null);
			_ownedFramebuffer.Updated -= OnFramebufferUpdated;
			_ownedFramebuffer.Dispose();
		}

		_disposed = true;
	}

	/// <inheritdoc />
	protected override global::Cornerstone.Presentation.Size MeasureOverride(global::Cornerstone.Presentation.Size availableSize)
	{
		var bitmapSize = ActiveFramebuffer.BitmapSize;
		var widthIsFinite = !double.IsInfinity(availableSize.Width) && !double.IsNaN(availableSize.Width);
		var heightIsFinite = !double.IsInfinity(availableSize.Height) && !double.IsNaN(availableSize.Height);
		if (widthIsFinite && heightIsFinite)
		{
			return availableSize;
		}

		if (!bitmapSize.HasValue)
		{
			return new global::Cornerstone.Presentation.Size();
		}

		return new global::Cornerstone.Presentation.Size(
			widthIsFinite ? availableSize.Width : bitmapSize.Value.Width,
			heightIsFinite ? availableSize.Height : bitmapSize.Value.Height);
	}

	private void OnFramebufferUpdated(object sender, bool sizeChanged)
	{
		if (sizeChanged)
		{
			InvalidateMeasure();
		}

		InvalidateVisual();
	}

	#endregion
}
