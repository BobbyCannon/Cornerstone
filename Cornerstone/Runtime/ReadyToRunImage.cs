#if !BROWSER
#region References

using System;
using System.IO;
using System.IO.Compression;
using System.Text;

#endregion

namespace Cornerstone.Runtime;

/// <summary>
/// Detects ReadyToRun native code in a managed PE or a .NET single-file bundle.
/// Native AOT is not ReadyToRun. There is no API for whether a given method ran from R2R code.
/// </summary>
internal static class ReadyToRunImage
{
	#region Constants

	/// <summary>
	/// SHA-256 of ".net core bundle". 8-byte header offset sits immediately before this in the apphost.
	/// </summary>
	private static readonly byte[] BundleSignature =
	[
		0x8B, 0x12, 0x02, 0xB9, 0x6A, 0x61, 0x20, 0x38,
		0x72, 0x7B, 0x93, 0x02, 0x14, 0xD7, 0xA0, 0x32,
		0x13, 0xF5, 0xB9, 0xE6, 0xEF, 0xAE, 0x33, 0x18,
		0xEE, 0x3B, 0x2D, 0xCE, 0x24, 0xB3, 0x6A, 0xAE
	];

	private const uint ReadyToRunSignature = 0x00525452;
	private const int BundleSearchChunkSize = 64 * 1024;
	private const int MaxBundleFiles = 8192;

	#endregion

	#region Methods

	/// <summary>
	/// True when the PE at path has a ReadyToRun managed native header.
	/// </summary>
	public static bool HasReadyToRunHeader(string path)
	{
		using var stream = File.OpenRead(path);
		return HasReadyToRunHeader(stream);
	}

	/// <summary>
	/// True when the PE stream (position 0 = DOS header) has a ReadyToRun managed native header.
	/// </summary>
	public static bool HasReadyToRunHeader(Stream stream)
	{
		using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

		if (stream.Length < 64)
		{
			return false;
		}

		if (reader.ReadUInt16() != 0x5A4D)
		{
			return false;
		}

		stream.Seek(0x3C, SeekOrigin.Begin);
		var peOffset = reader.ReadInt32();
		if ((peOffset < 0) || ((peOffset + 24) > stream.Length))
		{
			return false;
		}

		stream.Seek(peOffset, SeekOrigin.Begin);
		if (reader.ReadUInt32() != 0x00004550)
		{
			return false;
		}

		stream.Seek(peOffset + 4 + 2, SeekOrigin.Begin);
		var numberOfSections = reader.ReadUInt16();
		stream.Seek(peOffset + 4 + 16, SeekOrigin.Begin);
		var optionalHeaderSize = reader.ReadUInt16();
		var optionalHeaderOffset = peOffset + 24;
		if ((optionalHeaderOffset + optionalHeaderSize) > stream.Length)
		{
			return false;
		}

		stream.Seek(optionalHeaderOffset, SeekOrigin.Begin);
		var magic = reader.ReadUInt16();
		int dataDirectoryOffset;
		if (magic == 0x10B)
		{
			dataDirectoryOffset = optionalHeaderOffset + 96;
		}
		else if (magic == 0x20B)
		{
			dataDirectoryOffset = optionalHeaderOffset + 112;
		}
		else
		{
			return false;
		}

		const int comDescriptorIndex = 14;
		var comDirectoryEntry = dataDirectoryOffset + (comDescriptorIndex * 8);
		if ((comDirectoryEntry + 8) > stream.Length)
		{
			return false;
		}

		stream.Seek(comDirectoryEntry, SeekOrigin.Begin);
		var corHeaderRva = reader.ReadUInt32();
		var corHeaderSize = reader.ReadUInt32();
		if ((corHeaderRva == 0) || (corHeaderSize < 72))
		{
			return false;
		}

		var sectionTableOffset = optionalHeaderOffset + optionalHeaderSize;
		if (!TryMapRvaToOffset(stream, reader, sectionTableOffset, numberOfSections, corHeaderRva, out var corHeaderOffset))
		{
			return false;
		}

		const int managedNativeHeaderFieldOffset = 64;
		stream.Seek(corHeaderOffset + managedNativeHeaderFieldOffset, SeekOrigin.Begin);
		var managedNativeRva = reader.ReadUInt32();
		var managedNativeSize = reader.ReadUInt32();
		if ((managedNativeRva == 0) || (managedNativeSize < 4))
		{
			return false;
		}

		if (!TryMapRvaToOffset(stream, reader, sectionTableOffset, numberOfSections, managedNativeRva, out var nativeHeaderOffset))
		{
			// Non-empty ManagedNativeHeader is the R2R/NGen marker (jkotas). Signature confirms R2R when mapped.
			return true;
		}

		stream.Seek(nativeHeaderOffset, SeekOrigin.Begin);
		return reader.ReadUInt32() == ReadyToRunSignature;
	}

	/// <summary>
	/// True when the single-file host embeds ReadyToRun for the application assembly (or its composite .r2r.dll).
	/// Does not inspect framework assemblies (self-contained packs are already R2R).
	/// </summary>
	public static bool HasReadyToRunInSingleFileBundle(string hostPath, string assemblyName)
	{
		if (string.IsNullOrEmpty(hostPath) || !File.Exists(hostPath) || string.IsNullOrEmpty(assemblyName))
		{
			return false;
		}

		using var stream = File.OpenRead(hostPath);
		if (!TryFindBundleHeaderOffset(stream, out var headerOffset))
		{
			return false;
		}

		stream.Seek(headerOffset, SeekOrigin.Begin);
		using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

		var major = reader.ReadUInt32();
		var minor = reader.ReadUInt32();
		var fileCount = reader.ReadInt32();
		if ((fileCount <= 0) || (fileCount > MaxBundleFiles) || (major < 1) || (major > 100))
		{
			return false;
		}

		_ = minor;
		_ = reader.ReadString();

		if (major >= 2)
		{
			stream.Seek(sizeof(long) * 4 + sizeof(ulong), SeekOrigin.Current);
		}

		var appDll = assemblyName + ".dll";
		var compositeDll = assemblyName + ".r2r.dll";

		for (var i = 0; i < fileCount; i++)
		{
			var offset = reader.ReadInt64();
			var size = reader.ReadInt64();
			var compressedSize = 0L;
			if (major >= 6)
			{
				compressedSize = reader.ReadInt64();
			}

			var fileType = reader.ReadByte();
			var relativePath = reader.ReadString();
			var fileName = Path.GetFileName(relativePath);

			if ((offset <= 0) || (size <= 0))
			{
				continue;
			}

			var isAppAssembly = fileName.Equals(appDll, StringComparison.OrdinalIgnoreCase);
			var isComposite = fileName.Equals(compositeDll, StringComparison.OrdinalIgnoreCase);
			if (!isAppAssembly && !isComposite)
			{
				continue;
			}

			if ((fileType != 1) && !isComposite)
			{
				continue;
			}

			if (TryAssemblyHasReadyToRun(stream, offset, size, compressedSize))
			{
				return true;
			}
		}

		return false;
	}

	private static bool TryAssemblyHasReadyToRun(Stream host, long offset, long size, long compressedSize)
	{
		var restore = host.Position;
		try
		{
			if (compressedSize > 0)
			{
				host.Seek(offset, SeekOrigin.Begin);
				using var compressed = new MemoryStream((int) Math.Min(compressedSize, int.MaxValue));
				CopyExactly(host, compressed, compressedSize);
				compressed.Position = 0;
				using var deflate = new DeflateStream(compressed, CompressionMode.Decompress, leaveOpen: true);
				using var inflated = new MemoryStream((int) Math.Min(size, int.MaxValue));
				deflate.CopyTo(inflated);
				inflated.Position = 0;
				return HasReadyToRunHeader(inflated);
			}

			using var slice = new SliceStream(host, offset, size);
			return HasReadyToRunHeader(slice);
		}
		catch
		{
			return false;
		}
		finally
		{
			host.Seek(restore, SeekOrigin.Begin);
		}
	}

	private static void CopyExactly(Stream source, Stream destination, long count)
	{
		var buffer = new byte[8192];
		var remaining = count;
		while (remaining > 0)
		{
			var toRead = (int) Math.Min(buffer.Length, remaining);
			var read = source.Read(buffer, 0, toRead);
			if (read <= 0)
			{
				break;
			}

			destination.Write(buffer, 0, read);
			remaining -= read;
		}
	}

	private static bool TryFindBundleHeaderOffset(Stream stream, out long headerOffset)
	{
		headerOffset = 0;
		var signature = BundleSignature;
		var chunk = new byte[BundleSearchChunkSize];
		var overlap = signature.Length - 1;
		long position = 0;
		var carry = Array.Empty<byte>();

		while (position < stream.Length)
		{
			stream.Seek(position, SeekOrigin.Begin);
			var read = stream.Read(chunk, 0, chunk.Length);
			if (read <= 0)
			{
				break;
			}

			byte[] window;
			int windowOffset;
			if (carry.Length == 0)
			{
				window = chunk;
				windowOffset = 0;
			}
			else
			{
				window = new byte[carry.Length + read];
				Buffer.BlockCopy(carry, 0, window, 0, carry.Length);
				Buffer.BlockCopy(chunk, 0, window, carry.Length, read);
				windowOffset = 0;
			}

			var limit = (carry.Length == 0 ? read : window.Length) - signature.Length;
			for (var i = windowOffset; i <= limit; i++)
			{
				if (!BytesEqual(window, i, signature))
				{
					continue;
				}

				var signatureFileOffset = position - carry.Length + i;
				if (signatureFileOffset < 8)
				{
					continue;
				}

				stream.Seek(signatureFileOffset - 8, SeekOrigin.Begin);
				using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
				var offset = reader.ReadInt64();
				if ((offset > 0) && (offset < stream.Length))
				{
					headerOffset = offset;
					return true;
				}
			}

			if (read < signature.Length)
			{
				break;
			}

			carry = new byte[overlap];
			Buffer.BlockCopy(chunk, read - overlap, carry, 0, overlap);
			position += read;
		}

		return false;
	}

	private static bool BytesEqual(byte[] buffer, int offset, byte[] signature)
	{
		for (var i = 0; i < signature.Length; i++)
		{
			if (buffer[offset + i] != signature[i])
			{
				return false;
			}
		}

		return true;
	}

	private static bool TryMapRvaToOffset(Stream stream, BinaryReader reader, int sectionTableOffset, ushort numberOfSections, uint rva, out long fileOffset)
	{
		fileOffset = 0;
		const int sectionHeaderSize = 40;
		for (var i = 0; i < numberOfSections; i++)
		{
			var sectionOffset = sectionTableOffset + (i * sectionHeaderSize);
			if ((sectionOffset + sectionHeaderSize) > stream.Length)
			{
				return false;
			}

			stream.Seek(sectionOffset + 8, SeekOrigin.Begin);
			var virtualSize = reader.ReadUInt32();
			var virtualAddress = reader.ReadUInt32();
			reader.ReadUInt32();
			var rawDataPointer = reader.ReadUInt32();
			var size = virtualSize == 0 ? 0 : virtualSize;
			if ((rva >= virtualAddress) && (rva < (virtualAddress + Math.Max(size, 1))))
			{
				fileOffset = rawDataPointer + (rva - virtualAddress);
				return fileOffset < stream.Length;
			}
		}

		return false;
	}

	#endregion

	#region Classes

	/// <summary>
	/// Seekable window over an inner stream. Does not own the inner stream.
	/// </summary>
	private sealed class SliceStream : Stream
	{
		private readonly Stream _inner;
		private readonly long _origin;
		private readonly long _length;
		private long _position;

		public SliceStream(Stream inner, long origin, long length)
		{
			_inner = inner;
			_origin = origin;
			_length = length;
			_position = 0;
		}

		public override bool CanRead => true;
		public override bool CanSeek => true;
		public override bool CanWrite => false;
		public override long Length => _length;

		public override long Position
		{
			get => _position;
			set => Seek(value, SeekOrigin.Begin);
		}

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (_position >= _length)
			{
				return 0;
			}

			var remaining = _length - _position;
			if (count > remaining)
			{
				count = (int) remaining;
			}

			_inner.Seek(_origin + _position, SeekOrigin.Begin);
			var read = _inner.Read(buffer, offset, count);
			_position += read;
			return read;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			var next = origin switch
			{
				SeekOrigin.Begin => offset,
				SeekOrigin.Current => _position + offset,
				SeekOrigin.End => _length + offset,
				_ => _position
			};
			if (next < 0)
			{
				next = 0;
			}

			_position = next;
			return _position;
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}
	}

	#endregion
}
#endif
