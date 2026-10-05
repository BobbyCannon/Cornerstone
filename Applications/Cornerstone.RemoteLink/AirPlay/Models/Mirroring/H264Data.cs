namespace Cornerstone.RemoteLink.AirPlay.Models.Mirroring;

public struct H264Data
{
	#region Properties

	public byte[] Data { get; set; }
	public int FrameType { get; set; }
	public int Height { get; set; }
	public int Length { get; set; }
	public long Pts { get; set; }
	public int Width { get; set; }

	#endregion
}