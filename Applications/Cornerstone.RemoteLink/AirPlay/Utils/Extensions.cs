#region References

using System;
using System.Globalization;
using System.IO;
using System.Linq;

#endregion

namespace Cornerstone.RemoteLink.AirPlay.Utils;

public static class Extensions
{
	#region Methods

	public static string BytesToHex(this byte[] data)
	{
		return string.Join(string.Empty, data.Select(s => s.ToString("X2")));
	}

	public static byte[] HexToBytes(this string hex)
	{
		var data = new byte[hex.Length / 2];
		for (var index = 0; index < data.Length; index++)
		{
			var byteValue = hex.Substring(index * 2, 2);
			data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		}

		return data;
	}

	public static byte[] ReadBytesRequired(this BinaryReader binRdr, int byteCount)
	{
		var result = binRdr.ReadBytes(byteCount);

		if (result.Length != byteCount)
		{
			throw new EndOfStreamException(string.Format("{0} bytes required from stream, but only {1} returned.", byteCount, result.Length));
		}

		return result;
	}

	public static short ReadInt16BE(this BinaryReader binRdr)
	{
		return BitConverter.ToInt16(binRdr.ReadBytesRequired(sizeof(short)).Reverse(), 0);
	}

	public static int ReadInt32BE(this BinaryReader binRdr)
	{
		return BitConverter.ToInt32(binRdr.ReadBytesRequired(sizeof(int)).Reverse(), 0);
	}

	public static long ReadInt64BE(this BinaryReader binRdr)
	{
		return BitConverter.ToInt64(binRdr.ReadBytesRequired(sizeof(long)).Reverse(), 0);
	}

	public static ushort ReadUInt16BE(this BinaryReader binRdr)
	{
		return BitConverter.ToUInt16(binRdr.ReadBytesRequired(sizeof(ushort)).Reverse(), 0);
	}

	public static uint ReadUInt32BE(this BinaryReader binRdr)
	{
		return BitConverter.ToUInt32(binRdr.ReadBytesRequired(sizeof(uint)).Reverse(), 0);
	}

	public static ulong ReadUInt64BE(this BinaryReader binRdr)
	{
		return BitConverter.ToUInt64(binRdr.ReadBytesRequired(sizeof(ulong)).Reverse(), 0);
	}

	public static byte[] Reverse(this byte[] b)
	{
		Array.Reverse(b);
		return b;
	}

	#endregion
}