namespace Cornerstone.RemoteLink.AirPlay.Models.Audio;

public struct PcmData
{
	#region Properties

	public byte[] Data { get; set; }
	public int Length { get; set; }
	public ulong Pts { get; set; }

	#endregion
}