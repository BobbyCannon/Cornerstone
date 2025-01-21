namespace Cornerstone.Security.SecurityKeys.Apdu.Commands;

public class ReadBinaryCommand : ApduCommand
{
	#region Constructors

	public ReadBinaryCommand(ushort address, byte? expectedReturnBytes)
		: base(
			(byte) Apdu.Cla.ProtocolTypeSelection,
			(byte) Apdu.Ins.ReadBinary,
			0, 0, null, expectedReturnBytes)
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