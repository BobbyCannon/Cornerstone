#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

#endregion

namespace Cornerstone.RemoteLink.AirPlay.Plist;

/// <summary>
/// Performs de-serialization of binary plists.
/// </summary>
[SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "The spelling is correct.")]
public sealed class BinaryPlistReader
{
	#region Fields

	private List<BinaryPlistItem> objectTable;
	private int offsetIntSize, objectRefSize, objectCount, topLevelObjectOffset, offsetTableOffset;
	private List<int> offsetTable;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the BinaryPlistReader class.
	/// </summary>
	public BinaryPlistReader()
	{
	}

	#endregion

	#region Methods

	/// <summary>
	/// Reads a binary plist from the given file path into an <see cref="IDictionary" />.
	/// </summary>
	/// <param name="path"> The path of the file to read. </param>
	/// <returns> The result plist <see cref="IDictionary" />. </returns>
	public IDictionary ReadObject(string path)
	{
		using (var stream = File.OpenRead(path))
		{
			return ReadObject(stream);
		}
	}

	/// <summary>
	/// Reads a binary plist from the given stream into an <see cref="IDictionary" />.
	/// </summary>
	/// <param name="stream"> The <see cref="Stream" /> to read. </param>
	/// <returns> The result plist <see cref="IDictionary" />. </returns>
	public IDictionary ReadObject(Stream stream)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream", "stream cannot be null.");
		}

		if (!stream.CanRead)
		{
			throw new ArgumentException("The stream must be readable.", "stream");
		}

		var concreteStream = stream;
		var disposeConcreteStream = false;

		if (!stream.CanSeek)
		{
			concreteStream = new MemoryStream();
			var buffer = new byte[4096];
			var count = 0;

			while (0 < (count = stream.Read(buffer, 0, buffer.Length)))
			{
				concreteStream.Write(buffer, 0, count);
			}

			concreteStream.Position = 0;
			disposeConcreteStream = true;
		}

		try
		{
			Dictionary<object, object> dictionary = null;
			Reset();

			// Header + trailer = 40.
			if (stream.Length > 40)
			{
				using (var reader = new BinaryReader(concreteStream))
				{
					// Read the header.
					stream.Position = 0;
					var bpli = reader.ReadInt32().ToBigEndianConditional();
					var version = reader.ReadInt32().ToBigEndianConditional();

					if ((bpli != BinaryPlistWriter.HeaderMagicNumber) || (version != BinaryPlistWriter.HeaderVersionNumber))
					{
						throw new ArgumentException("The stream data does not start with required 'bplist00' header.", "stream");
					}

					// Read the trailer.
					// The first six bytes of the first eight-byte block are unused, so offset by 26 instead of 32.
					stream.Position = stream.Length - 26;
					offsetIntSize = reader.ReadByte();
					objectRefSize = reader.ReadByte();
					objectCount = (int) reader.ReadInt64().ToBigEndianConditional();
					topLevelObjectOffset = (int) reader.ReadInt64().ToBigEndianConditional();
					offsetTableOffset = (int) reader.ReadInt64().ToBigEndianConditional();
					var offsetTableSize = offsetIntSize * objectCount;

					// Ensure our sanity.
					if ((offsetIntSize < 1)
						|| (offsetIntSize > 8)
						|| (objectRefSize < 1)
						|| (objectRefSize > 8)
						|| (offsetTableOffset < 8)
						|| (topLevelObjectOffset >= objectCount)
						|| ((offsetTableSize + offsetTableOffset + 32) > stream.Length))
					{
						throw new ArgumentException("The stream data contains an invalid trailer.", "stream");
					}

					// Read the offset table and then the object table.
					ReadOffsetTable(reader);
					ReadObjectTable(reader);
				}
			}
			else
			{
				throw new ArgumentException("The stream is too short to be a valid binary plist.", "stream");
			}

			var root = objectTable[topLevelObjectOffset].Value as BinaryPlistDictionary;

			if (root != null)
			{
				dictionary = root.ToDictionary();
			}
			else
			{
				throw new InvalidOperationException("Unsupported root plist object: " + objectTable[topLevelObjectOffset].GetType() + ". A dictionary must be the root plist object.");
			}

			return dictionary ?? new Dictionary<object, object>();
		}
		finally
		{
			if (disposeConcreteStream && (concreteStream != null))
			{
				concreteStream.Dispose();
			}
		}
	}

	/// <summary>
	/// Reads a binary plist from the given file path into a new <see cref="IPlistSerializable" /> object instance.
	/// </summary>
	/// <typeparam name="T"> The concrete <see cref="IPlistSerializable" /> type to create. </typeparam>
	/// <param name="path"> The path of the file to read. </param>
	/// <returns> The result <see cref="IPlistSerializable" /> object instance. </returns>
	public T ReadObject<T>(string path) where T : IPlistSerializable, new()
	{
		using (Stream stream = File.OpenRead(path))
		{
			return ReadObject<T>(stream);
		}
	}

	/// <summary>
	/// Reads a binary plist from the given stream into a new <see cref="IPlistSerializable" /> object instance.
	/// </summary>
	/// <typeparam name="T"> The concrete <see cref="IPlistSerializable" /> type to create. </typeparam>
	/// <param name="stream"> The <see cref="Stream" /> to read. </param>
	/// <returns> The result <see cref="IPlistSerializable" /> object instance. </returns>
	public T ReadObject<T>(Stream stream) where T : IPlistSerializable, new()
	{
		var obj = new T();
		obj.FromPlistDictionary(ReadObject(stream));
		return obj;
	}

	/// <summary>
	/// Reads an array value from the given reader, starting at the given index and of the given size.
	/// </summary>
	/// <param name="reader"> The <see cref="BinaryReader" /> to read the array value from. </param>
	/// <param name="index"> The index in the stream the array value starts at. </param>
	/// <param name="size"> The number of items in the array. </param>
	/// <returns> An array value. </returns>
	private BinaryPlistArray ReadArray(BinaryReader reader, long index, int size)
	{
		var array = new BinaryPlistArray(objectTable, size);

		for (var i = 0; i < size; i++)
		{
			array.ObjectReference.Add((int) ReadInteger(reader, index + (i * objectRefSize), objectRefSize));
		}

		return array;
	}

	/// <summary>
	/// Reads an ASCII string value from the given reader, starting at the given index and of the given size.
	/// </summary>
	/// <param name="reader"> The <see cref="BinaryReader" /> to read the ASCII string value from. </param>
	/// <param name="index"> The index in the stream the string value starts at. </param>
	/// <param name="size"> The number of bytes that make up the string value. </param>
	/// <returns> A string value. </returns>
	private static string ReadAsciiString(BinaryReader reader, long index, int size)
	{
		var buffer = ReadData(reader, index, size);
		return buffer.Length > 0 ? Encoding.ASCII.GetString(buffer) : string.Empty;
	}

	/// <summary>
	/// Reads a data value from the given reader, starting at the given index and of the given size.
	/// </summary>
	/// <param name="reader"> The <see cref="BinaryReader" /> to read the data value from. </param>
	/// <param name="index"> The index in the stream the data value starts at. </param>
	/// <param name="size"> The number of bytes that make up the data value. </param>
	/// <returns> A data value. </returns>
	private static byte[] ReadData(BinaryReader reader, long index, int size)
	{
		reader.BaseStream.Position = index;

		var buffer = new byte[size];
		int bufferIndex = 0, count;

		while (0 < (count = reader.Read(buffer, bufferIndex, buffer.Length - bufferIndex)))
		{
			bufferIndex += count;
		}

		return buffer;
	}

	/// <summary>
	/// Reads a date value from the given reader, starting at the given index and of the given size.
	/// </summary>
	/// <param name="reader"> The <see cref="BinaryReader" /> to read the date value from. </param>
	/// <param name="index"> The index in the stream the date value starts at. </param>
	/// <param name="size"> The number of bytes that make up the date value. </param>
	/// <returns> A date value. </returns>
	private static DateTime ReadDate(BinaryReader reader, long index, int size)
	{
		return BinaryPlistWriter.ReferenceDate.AddSeconds(ReadReal(reader, index, size)).ToLocalTime();
	}

	/// <summary>
	/// Reads a dictionary value from the given reader, starting at the given index and of the given size.
	/// </summary>
	/// <param name="reader"> The <see cref="BinaryReader" /> to read the dictionary value from. </param>
	/// <param name="index"> The index in the stream the dictionary value starts at. </param>
	/// <param name="size"> The number of items in the dictionary. </param>
	/// <returns> A dictionary value. </returns>
	private BinaryPlistDictionary ReadDictionary(BinaryReader reader, long index, int size)
	{
		var dictionary = new BinaryPlistDictionary(objectTable, size);
		var skip = size * objectRefSize;

		for (var i = 0; i < size; i++)
		{
			dictionary.KeyReference.Add((int) ReadInteger(reader, index + (i * objectRefSize), objectRefSize));
			dictionary.ObjectReference.Add((int) ReadInteger(reader, skip + index + (i * objectRefSize), objectRefSize));
		}

		return dictionary;
	}

	/// <summary>
	/// Reads an integer value from the given reader, starting at the given index and of the given size.
	/// </summary>
	/// <param name="reader"> The <see cref="BinaryReader" /> to read the integer value from. </param>
	/// <param name="index"> The index in the stream the integer value starts at. </param>
	/// <param name="size"> The number of bytes that make up the integer value. </param>
	/// <returns> An integer value. </returns>
	private static long ReadInteger(BinaryReader reader, long index, int size)
	{
		var buffer = ReadData(reader, index, size);

		if ((buffer.Length > 1) && BitConverter.IsLittleEndian)
		{
			Array.Reverse(buffer);
		}

		switch (size)
		{
			case 1:
				return buffer[0];
			case 2:
				return BitConverter.ToUInt16(buffer, 0);
			case 4:
				return BitConverter.ToUInt32(buffer, 0);
			case 8:
				return (long) BitConverter.ToUInt64(buffer, 0);
			default:
				throw new InvalidOperationException("Unsupported variable-length integer size: " + size);
		}
	}

	/// <summary>
	/// Reads the object table from the given reader.
	/// </summary>
	/// <param name="reader"> The reader to read the object table from. </param>
	private void ReadObjectTable(BinaryReader reader)
	{
		byte marker;
		bool? primitive;
		int size, intSize;
		long parsedInt;
		BinaryPlistItem item;

		for (var i = 0; i < objectCount; i++)
		{
			reader.BaseStream.Position = offsetTable[i];
			marker = reader.ReadByte();
			var hexMarker = marker.ToString("X2");
			var firstHalf = marker >> 4;
			var bottomHalf = (marker & 0xf0) >> 4;
			var type = marker & 0xF0;
			var caseHex = type.ToString("X2");
			var hexFirstHalf = firstHalf.ToString("X2");
			var hexBottomHalf = bottomHalf.ToString("X2");

			// The first half of the byte is the base marker.
			switch (type)
			{
				case 0x00:
					if (ReadPrimitive(reader, reader.BaseStream.Position - 1, out primitive))
					{
						objectTable.Add(new BinaryPlistItem(primitive));
					}

					break;
				case 0x10:
					size = 1 << (marker & 0xf);
					parsedInt = ReadInteger(reader, reader.BaseStream.Position, size);

					if (size < 4)
					{
						objectTable.Add(new BinaryPlistItem((short) parsedInt));
					}
					else if (size < 8)
					{
						objectTable.Add(new BinaryPlistItem((int) parsedInt));
					}
					else
					{
						objectTable.Add(new BinaryPlistItem(parsedInt));
					}

					break;
				case 0x20:
					size = 1 << (marker & 0xf);
					objectTable.Add(new BinaryPlistItem(ReadReal(reader, reader.BaseStream.Position, size)));
					break;
				case 0x30:
					size = marker & 0xf;

					objectTable.Add(new BinaryPlistItem(ReadDate(reader, reader.BaseStream.Position, 8)));

					break;
				case 0x40:
					size = marker & 0xf;

					if (size == 15)
					{
						intSize = 1 << (reader.ReadByte() & 0xf);
						size = (int) ReadInteger(reader, reader.BaseStream.Position, intSize);
					}

					objectTable.Add(new BinaryPlistItem(ReadData(reader, reader.BaseStream.Position, size)));
					break;
				case 0x50:
					size = marker & 0xf;

					if (size == 15)
					{
						intSize = 1 << (reader.ReadByte() & 0xf);
						size = (int) ReadInteger(reader, reader.BaseStream.Position, intSize);
					}

					objectTable.Add(new BinaryPlistItem(ReadAsciiString(reader, reader.BaseStream.Position, size)));
					break;
				case 0x60:
					size = marker & 0xf;

					if (size == 15)
					{
						intSize = 1 << (reader.ReadByte() & 0xf);
						size = (int) ReadInteger(reader, reader.BaseStream.Position, intSize);
					}

					objectTable.Add(new BinaryPlistItem(ReadUnicodeString(reader, reader.BaseStream.Position, size)));
					break;
				case 0x80:
					size = (marker & 0xf) + 1;
					objectTable.Add(new BinaryPlistItem(ReadUniqueId(reader, reader.BaseStream.Position, size)));
					break;
				case 0xA0:
				case 0xC0:
					size = marker & 0xf;

					if (size == 15)
					{
						intSize = 1 << (reader.ReadByte() & 0xf);
						size = (int) ReadInteger(reader, reader.BaseStream.Position, intSize);
					}

					item = new BinaryPlistItem(ReadArray(reader, reader.BaseStream.Position, size));
					item.IsArray = true;
					objectTable.Add(item);
					break;
				case 0xD0:
					size = marker & 0xf;

					if (size == 15)
					{
						intSize = 1 << (reader.ReadByte() & 0xf);
						size = (int) ReadInteger(reader, reader.BaseStream.Position, intSize);
					}

					item = new BinaryPlistItem(ReadDictionary(reader, reader.BaseStream.Position, size));
					item.IsDictionary = true;
					objectTable.Add(item);
					break;
				default:
					throw new InvalidOperationException("An invalid marker was found while reading the object table: " + marker.ToBinaryString());
			}
		}
	}

	/// <summary>
	/// Reads the offset table from the given reader.
	/// </summary>
	/// <param name="reader"> The reader to read the offset table from. </param>
	private void ReadOffsetTable(BinaryReader reader)
	{
		for (var i = 0; i < objectCount; i++)
		{
			var offset = (int) ReadInteger(reader, offsetTableOffset + (i * offsetIntSize), offsetIntSize);
			offsetTable.Add(offset);
		}
	}

	/// <summary>
	/// Reads a primitive (true, false or null) value from the given reader, starting at the given index.
	/// </summary>
	/// <param name="reader"> The <see cref="BinaryReader" /> to read the primitive value from. </param>
	/// <param name="index"> The index in the stream the value starts at. </param>
	/// <param name="primitive"> Contains the read primitive value upon completion. </param>
	/// <returns> True if a value was read, false if the value was a fill byte. </returns>
	private static bool ReadPrimitive(BinaryReader reader, long index, out bool? primitive)
	{
		reader.BaseStream.Position = index;
		var value = reader.ReadByte();

		switch (value & 0xf)
		{
			case 0:
				primitive = null;
				return true;
			case 8:
				primitive = false;
				return true;
			case 9:
				primitive = true;
				return true;
			case 15:
				// This is a fill byte.
				primitive = null;
				return false;
			default:
				throw new InvalidOperationException("Illegal primitive: " + value.ToBinaryString());
		}
	}

	/// <summary>
	/// Reads a floating-point value from the given reader, starting at the given index and of the given size.
	/// </summary>
	/// <param name="reader"> The <see cref="BinaryReader" /> to read the floating-point value from. </param>
	/// <param name="index"> The index in the stream the floating-point value starts at. </param>
	/// <param name="size"> The number of bytes that make up the floating-point value. </param>
	/// <returns> A floating-point value. </returns>
	private static double ReadReal(BinaryReader reader, long index, int size)
	{
		var buffer = ReadData(reader, index, size);

		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(buffer);
		}

		switch (size)
		{
			case 4:
				return BitConverter.ToSingle(buffer, 0);
			case 8:
				return BitConverter.ToDouble(buffer, 0);
			default:
				throw new InvalidOperationException("Unsupported floating point number size: " + size);
		}
	}

	/// <summary>
	/// Reads a Unicode string value from the given reader, starting at the given index and of the given size.
	/// </summary>
	/// <param name="reader"> The <see cref="BinaryReader" /> to read the Unicode string value from. </param>
	/// <param name="index"> The index in the stream the string value starts at. </param>
	/// <param name="size"> The number of characters that make up the string value. </param>
	/// <returns> A string value. </returns>
	private static string ReadUnicodeString(BinaryReader reader, long index, int size)
	{
		reader.BaseStream.Position = index;
		size = size * 2;

		var buffer = new byte[size];
		byte one, two;

		for (var i = 0; i < size; i++)
		{
			one = reader.ReadByte();
			two = reader.ReadByte();

			if (BitConverter.IsLittleEndian)
			{
				buffer[i++] = two;
				buffer[i] = one;
			}
			else
			{
				buffer[i++] = one;
				buffer[i] = two;
			}
		}

		return Encoding.Unicode.GetString(buffer);
	}

	/// <summary>
	/// Reads a unique ID value from the given reader, starting at the given index and of the given size.
	/// </summary>
	/// <param name="reader"> The <see cref="BinaryReader" /> to read the unique ID value from. </param>
	/// <param name="index"> The index in the stream the unique ID value starts at. </param>
	/// <param name="size"> The number of bytes that make up the unique ID value. </param>
	/// <returns> A unique ID value. </returns>
	private static IDictionary ReadUniqueId(BinaryReader reader, long index, int size)
	{
		// Unique IDs in XML plists are <dict><key>CF$UID</key><integer>value</integer></dict>.
		// They're used by Cocoa's key-value coder. 
		var dict = new Dictionary<string, ulong>();
		dict["CF$UID"] = (ulong) ReadInteger(reader, index, size);
		return dict;
	}

	/// <summary>
	/// Resets this instance's state.
	/// </summary>
	private void Reset()
	{
		objectRefSize =
			objectCount =
				offsetIntSize =
					offsetTableOffset =
						topLevelObjectOffset = 0;

		objectTable = new List<BinaryPlistItem>();
		offsetTable = new List<int>();
	}

	#endregion
}