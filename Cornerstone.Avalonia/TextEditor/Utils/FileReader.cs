﻿#region References

using System;
using System.IO;
using System.Text;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Utils;

/// <summary>
/// Class that can open text files with auto-detection of the encoding.
/// </summary>
public static class FileReader
{
	#region Fields

	// ReSharper disable once InconsistentNaming
	private static readonly Encoding UTF8NoBOM = new UTF8Encoding(false);

	#endregion

	#region Methods

	/// <summary>
	/// Gets if the given encoding is a Unicode encoding (UTF).
	/// </summary>
	/// <remarks>
	/// Returns true for UTF-7, UTF-8, UTF-16 LE, UTF-16 BE, UTF-32 LE and UTF-32 BE.
	/// Returns false for all other encodings.
	/// </remarks>
	public static bool IsUnicode(Encoding encoding)
	{
		if (encoding == null)
		{
			throw new ArgumentNullException(nameof(encoding));
		}
		switch (encoding)
		{
			case UnicodeEncoding _:
			case UTF8Encoding _:
				return true;
			default:
				return false;
		}
		//switch (encoding) {
		//	case 65000: // UTF-7
		//	case 65001: // UTF-8
		//	case 1200: // UTF-16 LE
		//	case 1201: // UTF-16 BE
		//	case 12000: // UTF-32 LE
		//	case 12001: // UTF-32 BE
		//		return true;
		//	default:
		//		return false;
		//}
	}

	/// <summary>
	/// Opens the specified file for reading.
	/// </summary>
	/// <param name="fileName"> The file to open. </param>
	/// <param name="defaultEncoding"> The encoding to use if the encoding cannot be auto-detected. </param>
	/// <returns>
	/// Returns a StreamReader that reads from the stream. Use
	/// <see cref="StreamReader.CurrentEncoding" /> to get the encoding that was used.
	/// </returns>
	public static StreamReader OpenFile(string fileName, Encoding defaultEncoding)
	{
		throw new NotImplementedException();
		//if (fileName == null)
		//	throw new ArgumentNullException("fileName");
		//FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
		//try {
		//	return OpenStream(fs, defaultEncoding);
		//	// don't use finally: the stream must be kept open until the StreamReader closes it
		//} catch {
		//	fs.Dispose();
		//	throw;
		//}
	}

	/// <summary>
	/// Opens the specified stream for reading.
	/// </summary>
	/// <param name="stream"> The stream to open. </param>
	/// <param name="defaultEncoding"> The encoding to use if the encoding cannot be auto-detected. </param>
	/// <returns>
	/// Returns a StreamReader that reads from the stream. Use
	/// <see cref="StreamReader.CurrentEncoding" /> to get the encoding that was used.
	/// </returns>
	public static StreamReader OpenStream(Stream stream, Encoding defaultEncoding)
	{
		if (stream == null)
		{
			throw new ArgumentNullException(nameof(stream));
		}
		if (stream.Position != 0)
		{
			throw new ArgumentException("stream is not positioned at beginning.", nameof(stream));
		}
		if (defaultEncoding == null)
		{
			throw new ArgumentNullException(nameof(defaultEncoding));
		}

		if (stream.Length >= 2)
		{
			// the autodetection of StreamReader is not capable of detecting the difference
			// between ISO-8859-1 and UTF-8 without BOM.
			var firstByte = stream.ReadByte();
			var secondByte = stream.ReadByte();
			switch ((firstByte << 8) | secondByte)
			{
				case 0x0000: // either UTF-32 Big Endian or a binary file; use StreamReader
				case 0xfffe: // Unicode BOM (UTF-16 LE or UTF-32 LE)
				case 0xfeff: // UTF-16 BE BOM
				case 0xefbb: // start of UTF-8 BOM
					// StreamReader autodetection works
					stream.Position = 0;
					return new StreamReader(stream);
				default:
					return AutoDetect(stream, (byte) firstByte, (byte) secondByte, defaultEncoding);
			}
		}
		return new StreamReader(stream, defaultEncoding);
	}

	/// <summary>
	/// Reads the content of the given stream.
	/// </summary>
	/// <param name="stream">
	/// The stream to read.
	/// The stream must support seeking and must be positioned at its beginning.
	/// </param>
	/// <param name="defaultEncoding"> The encoding to use if the encoding cannot be auto-detected. </param>
	/// <returns> The file content as string. </returns>
	public static string ReadFileContent(Stream stream, Encoding defaultEncoding)
	{
		using (var reader = OpenStream(stream, defaultEncoding))
		{
			return reader.ReadToEnd();
		}
	}

	/// <summary>
	/// Reads the content of the file.
	/// </summary>
	/// <param name="fileName"> The file name. </param>
	/// <param name="defaultEncoding"> The encoding to use if the encoding cannot be auto-detected. </param>
	/// <returns> The file content as string. </returns>
	public static string ReadFileContent(string fileName, Encoding defaultEncoding)
	{
		throw new NotImplementedException();
		//using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
		//	return ReadFileContent(fs, defaultEncoding);
		//}
	}

	private static StreamReader AutoDetect(Stream fs, byte firstByte, byte secondByte, Encoding defaultEncoding)
	{
		var max = (int) Math.Min(fs.Length, 500000); // look at max. 500 KB
		// ReSharper disable InconsistentNaming
		const int ASCII = 0;
		const int Error = 1;
		const int UTF8 = 2;
		const int UTF8Sequence = 3;
		// ReSharper restore InconsistentNaming
		var state = ASCII;
		var sequenceLength = 0;
		for (var i = 0; i < max; i++)
		{
			byte b;
			if (i == 0)
			{
				b = firstByte;
			}
			else if (i == 1)
			{
				b = secondByte;
			}
			else
			{
				b = (byte) fs.ReadByte();
			}
			if (b < 0x80)
			{
				// normal ASCII character
				if (state == UTF8Sequence)
				{
					state = Error;
					break;
				}
			}
			else if (b < 0xc0)
			{
				// 10xxxxxx : continues UTF8 byte sequence
				if (state == UTF8Sequence)
				{
					--sequenceLength;
					if (sequenceLength < 0)
					{
						state = Error;
						break;
					}
					if (sequenceLength == 0)
					{
						state = UTF8;
					}
				}
				else
				{
					state = Error;
					break;
				}
			}
			else if ((b >= 0xc2) && (b < 0xf5))
			{
				// beginning of byte sequence
				if ((state == UTF8) || (state == ASCII))
				{
					state = UTF8Sequence;
					if (b < 0xe0)
					{
						sequenceLength = 1; // one more byte following
					}
					else if (b < 0xf0)
					{
						sequenceLength = 2; // two more bytes following
					}
					else
					{
						sequenceLength = 3; // three more bytes following
					}
				}
				else
				{
					state = Error;
					break;
				}
			}
			else
			{
				// 0xc0, 0xc1, 0xf5 to 0xff are invalid in UTF-8 (see RFC 3629)
				state = Error;
				break;
			}
		}
		fs.Position = 0;
		switch (state)
		{
			case ASCII:
				// TODO: Encoding.ASCII
				return new StreamReader(fs, IsAsciiCompatible(defaultEncoding) ? RemoveBom(defaultEncoding) : Encoding.UTF8);
			case Error:
				// When the file seems to be non-UTF8,
				// we read it using the user-specified encoding so it is saved again
				// using that encoding.
				if (IsUnicode(defaultEncoding))
				{
					// the file is not Unicode, so don't read it using Unicode even if the
					// user has choosen Unicode as the default encoding.

					defaultEncoding = Encoding.UTF8; // use system encoding instead
				}
				return new StreamReader(fs, RemoveBom(defaultEncoding));
			default:
				return new StreamReader(fs, UTF8NoBOM);
		}
	}

	private static bool IsAsciiCompatible(Encoding encoding)
	{
		var bytes = encoding.GetBytes("Az");
		return (bytes.Length == 2) && (bytes[0] == 'A') && (bytes[1] == 'z');
	}

	private static Encoding RemoveBom(Encoding encoding)
	{
		if (encoding is UTF8Encoding)
		{
			return UTF8NoBOM;
		}
		return encoding;

		//switch (encoding.CodePage) {
		//	case 65001: // UTF-8
		//		return UTF8NoBOM;
		//	default:
		//		return encoding;
		//}
	}

	#endregion
}