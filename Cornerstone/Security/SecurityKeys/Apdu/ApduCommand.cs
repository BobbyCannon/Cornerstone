#region References

using System;
using System.IO;

#endregion

namespace Cornerstone.Security.SecurityKeys.Apdu;

/// <summary>
/// This class represents a command APDU (Application Protocol Data Unit)
/// </summary>
public class ApduCommand
{
	#region Constants

	public const int MinLength = 4;

	#endregion

	#region Constructors

	public ApduCommand(byte cla, byte ins, byte p1, byte p2, byte[] data = null, int? le = null)
	{
		Cla = cla;
		Ins = ins;
		P1 = p1;
		P2 = p2;
		Data = data;
		Le = le;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The class of instruction.
	/// </summary>
	public byte Cla { get; set; }

	/// <summary>
	/// The data for the command.
	/// LC (data length) will be set if data is provided.
	/// </summary>
	public byte[] Data { get; set; }

	/// <summary>
	/// The instruction code.
	/// </summary>
	public byte Ins { get; set; }

	/// <summary>
	/// Maximum number of bytes expected in the response to this command.
	/// </summary>
	public int? Le { get; set; }

	/// <summary>
	/// The instruction parameter 1.
	/// </summary>
	public byte P1 { get; set; }

	/// <summary>
	/// The instruction parameter 2.
	/// </summary>
	public byte P2 { get; set; }

	#endregion

	#region Methods

	public byte[] ToByteArray()
	{
		using (var ms = new MemoryStream())
		{
			ms.WriteByte(Cla);
			ms.WriteByte(Ins);
			ms.WriteByte(P1);
			ms.WriteByte(P2);

			if (Data is { Length: > 0 })
			{
				ms.WriteByte((byte) Data.Length);
				ms.Write(Data, 0, Data.Length);
			}

			if (Le != null)
			{
				ms.WriteByte((byte) Le);
			}

			return ms.ToArray();
		}
	}

	public override string ToString()
	{
		return BitConverter.ToString(ToByteArray());
	}

	#endregion
}