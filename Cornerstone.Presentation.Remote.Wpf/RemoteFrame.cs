#region References

using Cornerstone.Presentation.Remote.Protocol.Viewport;

#endregion

namespace Cornerstone.Presentation.Remote.Wpf;

/// <summary>
/// One framebuffer from a Cornerstone remote host.
/// </summary>
public sealed class RemoteFrame
{
	#region Properties

	public byte[] Data { get; set; }

	public PixelFormat Format { get; set; }

	public int Height { get; set; }

	public long SequenceId { get; set; }

	public int Stride { get; set; }

	public int Width { get; set; }

	#endregion
}