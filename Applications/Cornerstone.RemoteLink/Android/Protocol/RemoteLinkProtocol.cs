#region References

#endregion

namespace Cornerstone.RemoteLink.Android.Protocol;

/// <summary>
/// Framing shared with remotelink-server.jar. Keep in lockstep with
/// cornerstone.remotelink.server.Protocol on the device.
/// </summary>
public static class RemoteLinkProtocol
{
	#region Constants

	public const int ControlPort = 27183;
	public const ushort FlagVideo = 1;
	public const ushort Version = 1;
	public const int VideoPort = 27182;

	#endregion

	#region Fields

	public static readonly byte[] Magic = [0x52, 0x4C, 0x4E, 0x4B];

	#endregion

	#region Enums

	public enum ControlType : byte
	{
		TouchDown = 1,
		TouchMove = 2,
		TouchUp = 3,
		Key = 4,
		Back = 5,
		Home = 6,
		AppSwitch = 7,
		Text = 8
	}

	public enum MessageType : byte
	{
		Hello = 1,
		VideoHeader = 2,
		VideoPacket = 3,
		Control = 4
	}

	#endregion
}
