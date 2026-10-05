namespace Cornerstone.Security.SecurityKeys.Apdu.Commands;

public class WriteBytesCommand : ApduCommand
{
	#region Constructors

	public WriteBytesCommand(ushort address, byte[] data)
		: base(
			(byte) Apdu.Cla.ProtocolTypeSelection,
			(byte) Apdu.Ins.WriteBytes,
			0, 0, data)
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