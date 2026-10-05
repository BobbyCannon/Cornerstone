#region References

using System;

#endregion

namespace Cornerstone.RemoteLink.AirPlay.Models.Mirroring;

public class MirroringHeader
{
	#region Constructors

	public MirroringHeader(byte[] header)
	{
		if ((header == null) || (header.Length < 16))
		{
			return;
		}

		// 128-byte mirror header is little-endian. A 2048-byte NAL is 00 08 00 00;
		// reading that as big-endian yields 524288 and desyncs the TCP stream.
		PayloadSize = header[0] | (header[1] << 8) | (header[2] << 16) | (header[3] << 24);
		PayloadType = (short) (header[4] & 0xFF);
		PayloadOption = (short) (header[6] | (header[7] << 8));

		if (PayloadType == 0)
		{
			PayloadNtp = ReadUInt64LittleEndian(header, 8);
			PayloadPts = NtpToPts(PayloadNtp);
		}

		// WidthSource/HeightSource are the device panel. Encoded Width/Height
		// follow the current orientation and swap on rotation.
		if (header.Length >= 64)
		{
			WidthSource = (int) BitConverter.ToSingle(header, 40);
			HeightSource = (int) BitConverter.ToSingle(header, 44);
			Width = (int) BitConverter.ToSingle(header, 56);
			Height = (int) BitConverter.ToSingle(header, 60);
			if (WidthSource <= 0)
			{
				WidthSource = (int) BitConverter.ToSingle(header, 16);
			}

			if (HeightSource <= 0)
			{
				HeightSource = (int) BitConverter.ToSingle(header, 20);
			}
		}
	}

	#endregion

	#region Properties

	public int Height { get; }
	public int HeightSource { get; }

	public bool IsValid
	{
		get
		{
			// Types: 0 video, 1 codec (SPS/PPS), 2 heartbeat, 5 streaming report.
			if ((PayloadType < 0) || (PayloadType > 5) || (PayloadSize < 0) || (PayloadSize > (2 * 1024 * 1024)))
			{
				return false;
			}

			if ((PayloadType == 0) || (PayloadType == 1))
			{
				return PayloadSize > 0;
			}

			return true;
		}
	}

	public long PayloadNtp { get; }
	public short PayloadOption { get; }
	public long PayloadPts { get; }
	public int PayloadSize { get; }
	public short PayloadType { get; }
	public int Width { get; }
	public int WidthSource { get; }

	#endregion

	#region Methods

	private long NtpToPts(long ntp)
	{
		var seconds = (ntp >> 32) & 0xffffffff;
		var fraction = ntp & 0xffffffff;
		return (seconds * 1000000) + ((fraction * 1000000) >> 32);
	}

	private static long ReadUInt64LittleEndian(byte[] data, int offset)
	{
		return data[offset]
			| ((long) data[offset + 1] << 8)
			| ((long) data[offset + 2] << 16)
			| ((long) data[offset + 3] << 24)
			| ((long) data[offset + 4] << 32)
			| ((long) data[offset + 5] << 40)
			| ((long) data[offset + 6] << 48)
			| ((long) data[offset + 7] << 56);
	}

	#endregion
}