namespace Cornerstone.Security.SecurityKeys.Apdu.Commands;

public class LoadKeysCommand : ApduCommand
{
	#region Constructors

	public LoadKeysCommand(byte[] keys)
		: base(
			(byte) Apdu.Cla.ProtocolTypeSelection,
			(byte) Apdu.Ins.LoadKeys,
			0, (byte) keys.Length, keys)
	{
	}

	#endregion
}