namespace Cornerstone.Presentation.Platform;

/// <summary>
/// Native host island that can take CPU BGRA frames (Wayland SHM subsurface).
/// </summary>
public interface INativePixelBufferHost
{
	void SetUsesExternalBuffers(bool value);

	void AttachBgra(byte[] pixels, int width, int height);
}
