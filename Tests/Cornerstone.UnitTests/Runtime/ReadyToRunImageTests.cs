#region References

using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Runtime;

[TestClass]
public class ReadyToRunImageTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void DetectsCompositeReadyToRunImage()
	{
		var path = WriteBundle(CreatePe(true), "App.r2r.dll", false, 0);
		try
		{
			IsTrue(ReadyToRunImage.HasReadyToRunInSingleFileBundle(path, "App"));
		}
		finally
		{
			File.Delete(path);
		}
	}

	[TestMethod]
	public void DetectsIlOnlyPe()
	{
		using var stream = new MemoryStream(CreatePe(false));
		IsFalse(ReadyToRunImage.HasReadyToRunHeader(stream));
	}

	[TestMethod]
	public void DetectsReadyToRunInCompressedSingleFileBundle()
	{
		var path = WriteBundle(CreatePe(true), "App.dll", true, 1);
		try
		{
			IsTrue(ReadyToRunImage.HasReadyToRunInSingleFileBundle(path, "App"));
		}
		finally
		{
			File.Delete(path);
		}
	}

	[TestMethod]
	public void DetectsReadyToRunInSingleFileBundle()
	{
		var path = WriteBundle(CreatePe(true), "App.dll", false, 1);
		try
		{
			IsTrue(ReadyToRunImage.HasReadyToRunInSingleFileBundle(path, "App"));
			IsFalse(ReadyToRunImage.HasReadyToRunInSingleFileBundle(path, "Other"));
		}
		finally
		{
			File.Delete(path);
		}
	}

	[TestMethod]
	public void DetectsReadyToRunPe()
	{
		using var stream = new MemoryStream(CreatePe(true));
		IsTrue(ReadyToRunImage.HasReadyToRunHeader(stream));
	}

	[TestMethod]
	public void IgnoresHostWithoutBundle()
	{
		var path = Path.Combine(Path.GetTempPath(), "r2r-host-" + Guid.NewGuid().ToString("N") + ".exe");
		File.WriteAllBytes(path, new byte[256]);
		try
		{
			IsFalse(ReadyToRunImage.HasReadyToRunInSingleFileBundle(path, "App"));
		}
		finally
		{
			File.Delete(path);
		}
	}

	[TestMethod]
	public void IgnoresIlOnlySingleFileBundle()
	{
		var path = WriteBundle(CreatePe(false), "App.dll", false, 1);
		try
		{
			IsFalse(ReadyToRunImage.HasReadyToRunInSingleFileBundle(path, "App"));
		}
		finally
		{
			File.Delete(path);
		}
	}

	private static byte[] CreatePe(bool readyToRun)
	{
		var pe = new byte[0x600];
		pe[0] = 0x4D;
		pe[1] = 0x5A;
		BitConverter.GetBytes(0x80).CopyTo(pe, 0x3C);

		var peOffset = 0x80;
		BitConverter.GetBytes(0x00004550).CopyTo(pe, peOffset);
		BitConverter.GetBytes((ushort) 0x8664).CopyTo(pe, peOffset + 4);
		BitConverter.GetBytes((ushort) 1).CopyTo(pe, peOffset + 6);
		BitConverter.GetBytes((ushort) 240).CopyTo(pe, peOffset + 20);

		var optionalHeaderOffset = peOffset + 24;
		BitConverter.GetBytes((ushort) 0x20B).CopyTo(pe, optionalHeaderOffset);
		BitConverter.GetBytes(16).CopyTo(pe, optionalHeaderOffset + 108);

		var comDirectory = optionalHeaderOffset + 112 + (14 * 8);
		BitConverter.GetBytes(0x2000u).CopyTo(pe, comDirectory);
		BitConverter.GetBytes(72u).CopyTo(pe, comDirectory + 4);

		var sectionTable = optionalHeaderOffset + 240;
		Encoding.ASCII.GetBytes(".text").CopyTo(pe, sectionTable);
		BitConverter.GetBytes(0x200u).CopyTo(pe, sectionTable + 8);
		BitConverter.GetBytes(0x2000u).CopyTo(pe, sectionTable + 12);
		BitConverter.GetBytes(0x200u).CopyTo(pe, sectionTable + 16);
		BitConverter.GetBytes(0x400u).CopyTo(pe, sectionTable + 20);

		if (readyToRun)
		{
			BitConverter.GetBytes(0x2050u).CopyTo(pe, 0x400 + 64);
			BitConverter.GetBytes(8u).CopyTo(pe, 0x400 + 68);
			BitConverter.GetBytes(0x00525452u).CopyTo(pe, 0x450);
		}

		return pe;
	}

	private static string WriteBundle(byte[] pe, string relativePath, bool compress, byte fileType)
	{
		var path = Path.Combine(Path.GetTempPath(), "r2r-bundle-" + Guid.NewGuid().ToString("N") + ".exe");
		using var stream = File.Create(path);
		using var writer = new BinaryWriter(stream, Encoding.UTF8, true);

		var host = new byte[512];
		var signature = new byte[]
		{
			0x8B, 0x12, 0x02, 0xB9, 0x6A, 0x61, 0x20, 0x38,
			0x72, 0x7B, 0x93, 0x02, 0x14, 0xD7, 0xA0, 0x32,
			0x13, 0xF5, 0xB9, 0xE6, 0xEF, 0xAE, 0x33, 0x18,
			0xEE, 0x3B, 0x2D, 0xCE, 0x24, 0xB3, 0x6A, 0xAE
		};
		Buffer.BlockCopy(signature, 0, host, 72, signature.Length);
		writer.Write(host);

		var payloadOffset = stream.Position;
		long uncompressedSize = pe.Length;
		long compressedSize = 0;
		if (compress)
		{
			using var compressed = new MemoryStream();
			using (var deflate = new DeflateStream(compressed, CompressionLevel.Optimal, true))
			{
				deflate.Write(pe, 0, pe.Length);
			}

			writer.Write(compressed.ToArray());
			compressedSize = compressed.Length;
		}
		else
		{
			writer.Write(pe);
		}

		var headerOffset = stream.Position;
		writer.Write(6u);
		writer.Write(0u);
		writer.Write(1);
		writer.Write("abcdefghijkl");
		writer.Write(0L);
		writer.Write(0L);
		writer.Write(0L);
		writer.Write(0L);
		writer.Write(0UL);
		writer.Write(payloadOffset);
		writer.Write(uncompressedSize);
		writer.Write(compressedSize);
		writer.Write(fileType);
		writer.Write(relativePath);

		stream.Seek(64, SeekOrigin.Begin);
		writer.Write(headerOffset);
		return path;
	}

	#endregion
}