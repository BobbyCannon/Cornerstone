namespace Cornerstone.Security.SecurityKeys.Apdu.Commands;

public class ReadBytesCommand : ApduCommand
{
	#region Constructors

	public ReadBytesCommand(ushort address)
		: base(
			(byte) Apdu.Cla.ProtocolTypeSelection,
			(byte) Apdu.Ins.ReadBytes,
			0, 0, null, 16)
	{
		Address = address;
	}

	#endregion

	#region Properties

	public ushort Address
	{
		get => (ushort) ((P1 << 8) | P2);
		set
		{
			P1 = (byte) (value >> 8);
			P2 = (byte) (value & 0x00FF);
		}
	}

	#endregion
}