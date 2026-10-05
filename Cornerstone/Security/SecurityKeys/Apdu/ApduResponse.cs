#region References

using System;

#endregion

namespace Cornerstone.Security.SecurityKeys.Apdu;

/// <summary>
/// This class represents the APDU response sent by the card.
/// Application Protocol Data Unit
/// </summary>
public class ApduResponse
{
	#region Constants

	/// <summary>
	/// Status bytes length
	/// </summary>
	public const int SwLength = 2;

	#endregion

	#region Constructors

	/// <summary>
	/// Constructor from the byte data sent back by the card
	/// </summary>
	/// <param name="baData"> Buffer of data from the card </param>
	public ApduResponse(byte[] baData)
	{
		if ((baData == null)
			|| (baData.Length == 0))
		{
			Data = [];
			Sw1 = 0;
			Sw2 = 0;
			return;
		}

		if (baData.Length > SwLength)
		{
			Data = new byte[baData.Length - SwLength];

			for (var nI = 0; nI < (baData.Length - SwLength); nI++)
			{
				Data[nI] = baData[nI];
			}
		}
		else
		{
			Data = [];
		}

		Sw1 = baData[baData.Length - 2];
		Sw2 = baData[baData.Length - 1];
	}

	#endregion

	#region Properties

	/// <summary>
	/// Response data get property. Contains the data sent by the card minus the 2 status bytes (SW1, SW2)
	/// null if no data were sent by the card
	/// </summary>
	public byte[] Data { get; }

	/// <summary>
	/// Status get property
	/// </summary>
	public ushort Status => (ushort) ((Sw1 << 8) + Sw2);

	/// <summary>
	/// SW1 byte get property
	/// </summary>
	public byte Sw1 { get; }

	/// <summary>
	/// SW2 byte get property
	/// </summary>
	public byte Sw2 { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Overrides the ToString method to format to a string the APDUResponse object
	/// </summary>
	/// <returns> </returns>
	public override string ToString()
	{
		var sRet = $"SW={Status:X04}";

		if (Data is { Length: > 0 })
		{
			sRet += " Data=" + BitConverter.ToString(Data);
		}

		return sRet;
	}

	#endregion
}