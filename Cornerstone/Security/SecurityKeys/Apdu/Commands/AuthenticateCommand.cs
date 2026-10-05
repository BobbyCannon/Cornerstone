namespace Cornerstone.Security.SecurityKeys.Apdu.Commands;

public class AuthenticateCommand : ApduCommand
{
	#region Constructors

	public AuthenticateCommand(ushort address)
		: base((byte) Apdu.Cla.ProtocolTypeSelection,
			(byte) Apdu.Ins.Authenticate,
			0, 0,
			[1, (byte) (address >> 8), (byte) (address & 0x00FF), 0x60, 0]
		)
	{
		// FF-86-00-00-05-01-00-00-60-00
	}

	#endregion
}