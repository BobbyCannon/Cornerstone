#region References

using System;
using Cornerstone.RemoteLink.AirPlay.Models.Mirroring;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.RemoteLink;

[TestClass]
public class MirroringHeaderTests : RemoteLinkUnitTest
{
	#region Methods

	[TestMethod]
	public void CodecPacketSizeIsLittleEndian()
	{
		var header = new byte[128];
		header[0] = 0x1F;
		header[4] = 0x01;
		header[6] = 0x06;
		WriteSingleLittleEndian(header, 16, 864f);
		WriteSingleLittleEndian(header, 20, 648f);
		WriteSingleLittleEndian(header, 40, 864f);
		WriteSingleLittleEndian(header, 44, 648f);
		WriteSingleLittleEndian(header, 56, 863f);
		WriteSingleLittleEndian(header, 60, 647f);

		var parsed = new MirroringHeader(header);
		AreEqual(31, parsed.PayloadSize);
		AreEqual(1, parsed.PayloadType);
		AreEqual(864, parsed.WidthSource);
		AreEqual(648, parsed.HeightSource);
		AreEqual(863, parsed.Width);
		AreEqual(647, parsed.Height);
		IsTrue(parsed.IsValid);
	}

	[TestMethod]
	public void EncodedSizeFollowsLandscapeOrientation()
	{
		var header = new byte[128];
		header[4] = 0x01;
		WriteSingleLittleEndian(header, 16, 1170f);
		WriteSingleLittleEndian(header, 20, 2532f);
		WriteSingleLittleEndian(header, 40, 1170f);
		WriteSingleLittleEndian(header, 44, 2532f);
		WriteSingleLittleEndian(header, 56, 2532f);
		WriteSingleLittleEndian(header, 60, 1170f);

		var parsed = new MirroringHeader(header);
		AreEqual(1170, parsed.WidthSource);
		AreEqual(2532, parsed.HeightSource);
		AreEqual(2532, parsed.Width);
		AreEqual(1170, parsed.Height);
		IsTrue(parsed.Width > parsed.Height);
	}

	[TestMethod]
	public void VideoPacketDoesNotInflate1280To320Kilobytes()
	{
		var header = new byte[128];
		header[0] = 0x00;
		header[1] = 0x05;

		var parsed = new MirroringHeader(header);
		AreEqual(1280, parsed.PayloadSize);
		AreEqual(0, parsed.PayloadType);
		IsTrue(parsed.IsValid);
	}

	[TestMethod]
	public void VideoPacketSizeIsLittleEndianNotBigEndian()
	{
		var header = new byte[128];
		header[0] = 0x00;
		header[1] = 0x08;

		var parsed = new MirroringHeader(header);
		AreEqual(2048, parsed.PayloadSize);
		AreEqual(0, parsed.PayloadType);
		IsTrue(parsed.IsValid);
	}

	private static void WriteSingleLittleEndian(byte[] buffer, int offset, float value)
	{
		var bytes = BitConverter.GetBytes(value);
		if (!BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}

		Buffer.BlockCopy(bytes, 0, buffer, offset, 4);
	}

	#endregion
}