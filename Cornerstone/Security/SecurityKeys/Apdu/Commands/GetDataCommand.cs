namespace Cornerstone.Security.SecurityKeys.Apdu.Commands;

public class GetDataCommand : ApduCommand
{
	#region Constructors

	public GetDataCommand(GetDataDataType type)
		: base(
			(byte) Apdu.Cla.ProtocolTypeSelection,
			(byte) Apdu.Ins.GetData,
			(byte) type, 0, null, 0)
	{
	}

	#endregion

	#region Properties

	public GetDataDataType Type
	{
		set => P1 = (byte) value;
		get => (GetDataDataType) P1;
	}

	#endregion

	#region Enumerations

	public enum GetDataDataType : byte
	{
		Uid = 0x00,
		HistoricalBytes = 0x01
	}

	#endregion
}