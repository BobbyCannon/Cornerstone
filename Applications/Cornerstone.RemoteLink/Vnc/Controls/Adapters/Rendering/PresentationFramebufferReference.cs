using System;
using Cornerstone.Presentation.Platform;
using Cornerstone.RemoteLink.Vnc.Client;
using Cornerstone.RemoteLink.Vnc.Client.Rendering;
using PixelFormat = Cornerstone.RemoteLink.Vnc.Client.PixelFormat;

namespace Cornerstone.RemoteLink.Vnc.Controls.Adapters.Rendering;

/// <inheritdoc />
public sealed class PresentationFramebufferReference : IFramebufferReference
{
	private ILockedFramebuffer _lockedFramebuffer;
	private readonly Action _invalidateVisual;

	/// <inheritdoc />
	public IntPtr Address => _lockedFramebuffer?.Address ?? throw new ObjectDisposedException(nameof(PresentationFramebufferReference));

	/// <inheritdoc />
	public Size Size => Conversions.GetSize(_lockedFramebuffer?.Size ?? throw new ObjectDisposedException(nameof(PresentationFramebufferReference)));

	/// <inheritdoc />
	public PixelFormat Format => Conversions.GetPixelFormat(_lockedFramebuffer?.Format ?? throw new ObjectDisposedException(nameof(PresentationFramebufferReference)));

	/// <inheritdoc />
	public double HorizontalDpi => _lockedFramebuffer?.Dpi.X ?? throw new ObjectDisposedException(nameof(PresentationFramebufferReference));

	/// <inheritdoc />
	public double VerticalDpi => _lockedFramebuffer?.Dpi.Y ?? throw new ObjectDisposedException(nameof(PresentationFramebufferReference));

	internal PresentationFramebufferReference(ILockedFramebuffer lockedFramebuffer, Action invalidateVisual)
	{
		_lockedFramebuffer = lockedFramebuffer;
		_invalidateVisual = invalidateVisual;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		var lockedFramebuffer = _lockedFramebuffer;
		_lockedFramebuffer = null;
		if (lockedFramebuffer == null)
		{
			return;
		}

		lockedFramebuffer.Dispose();
		_invalidateVisual();
	}
}
