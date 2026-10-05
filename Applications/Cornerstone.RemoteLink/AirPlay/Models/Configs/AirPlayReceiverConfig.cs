namespace Cornerstone.RemoteLink.AirPlay.Models.Configs;

public class AirPlayReceiverConfig
{
	#region Properties

	public ushort AirPlayPort { get; set; }
	public ushort AirTunesPort { get; set; }
	public string DeviceMacAddress { get; set; }
	public string Instance { get; set; }

	#endregion
}