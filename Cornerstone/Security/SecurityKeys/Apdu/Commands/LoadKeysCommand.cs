namespace Cornerstone.Security.SecurityKeys.Apdu.Commands;

public class LoadKeysCommand : ApduCommand
{
	#region Constructors

	public LoadKeysCommand(byte[] keys)
		: base(
			(byte) Apdu.Cla.ProtocolTypeSelection,
			(byte) Apdu.Ins.LoadKeys,
			0, 0, keys)
	{
		// FF-82-00-00-06-FF-FF-FF-FF-FF-FF
		// 90-00
	}

	#endregion
}