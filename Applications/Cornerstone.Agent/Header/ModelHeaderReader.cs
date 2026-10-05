#region References

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

#endregion

namespace Cornerstone.Agent.Header;

public class ModelHeaderReader
{
	#region Methods

	/// <summary>
	/// Reads a GGUF file and parses its header, metadata entries, tensor descriptors, and data section offset.
	/// </summary>
	/// <param name="filePath"> The path to the GGUF file. </param>
	/// <returns> A <see cref="ModelFile" /> describing the parsed file structure. </returns>
	public ModelFile Read(string filePath)
	{
		using var fs = MemoryMappedFile.CreateFromFile(filePath);
		using var s = fs.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
		var header = ReadHeader(s);
		var d = ReadMetaData(s, header.MetaKeyValueCount).ToList();

		var t = ReadTensorData(s, header.TensorCount).ToList();
		ulong alignment = 32; //TODO: read align from header

		var startOffset = (ulong) s.Position + ((alignment - ((ulong) s.Position % alignment)) % alignment);
		var sortedItems = t.OrderBy(x => x.Offset).ToList();
		for (var i = 0; i < (sortedItems.Count - 1); i++)
		{
			sortedItems[i].Size = sortedItems[i + 1].Offset - sortedItems[i].Offset;
		}
		var last = sortedItems.Last();
		last.Size = (ulong) new FileInfo(filePath).Length - last.Offset - startOffset;

		return new ModelFile
		{
			FilePath = filePath,
			MetaItems = d,
			TensorInfos = sortedItems,
			Version = header.Version,
			DataStartOffset = startOffset
		};
	}

	/// <summary>
	/// Reads the raw byte payload for a tensor from a parsed GGUF file.
	/// </summary>
	/// <param name="file"> The parsed GGUF file descriptor. </param>
	/// <param name="tensor"> The tensor descriptor to read. </param>
	/// <returns>
	/// An <see cref="IMemoryOwner{T}" /> containing the tensor bytes.
	/// The caller owns the returned buffer and must dispose it after use.
	/// </returns>
	/// <remarks>
	/// The returned buffer is rented from <see cref="MemoryPool{T}.Shared" />.
	/// If the caller does not dispose it explicitly, the rented memory can remain occupied longer than necessary.
	/// </remarks>
	public IMemoryOwner<byte> ReadTensorData(ModelFile file, ModelTensorInfo tensor)
	{
		using var fs = MemoryMappedFile.CreateFromFile(file.FilePath);
		using var s = fs.CreateViewStream((long) (file.DataStartOffset + tensor.Offset), (long) tensor.Size, MemoryMappedFileAccess.Read);
		if (tensor.Size > int.MaxValue)
		{
			throw new NotSupportedException("Not supported by now, tensor size should not larger than max value of int32");
		}
		var om = MemoryPool<byte>.Shared.Rent((int) tensor.Size);
		_ = s.Read(om.Memory.Span);
		return om;
	}

	private ReadOnlySpan<T> ReadArray<T>(BinaryReader reader, ulong elementCount = 0) where T : struct
	{
		if (elementCount == 0)
		{
			elementCount = reader.ReadUInt64();
		}
		var length = Marshal.SizeOf<T>() * (int) elementCount;
		var buffer = new byte[length];
		_ = reader.Read(buffer, 0, length);
		return MemoryMarshal.Cast<byte, T>(buffer);
	}

	private ModelHeader ReadHeader(Stream header)
	{
		using var br = new BinaryReader(header, Encoding.UTF8, true);
		var result = new ModelHeader();
		result.MagicCode = br.ReadUInt32();
		if (result.MagicCode != 0x46554747) // "GGUF" in little-endian bytes order
		{
			throw new InvalidOperationException("Invalid magic code");
		}
		result.Version = br.ReadUInt32();
		result.TensorCount = br.ReadUInt64();
		result.MetaKeyValueCount = br.ReadUInt64();
		return result;
	}

	private IEnumerable<ModelMetaItem> ReadMetaData(Stream meta, ulong metaCount)
	{
		using var br = new BinaryReader(meta, Encoding.UTF8, true);
		for (ulong i = 0; i < metaCount; i++)
		{
			var result = new ModelMetaItem();
			result.Name = ReadString(br);
			result.DataType = (ModelDataTypeEnum) br.ReadUInt32();
			int size;
			switch (result.DataType)
			{
				case ModelDataTypeEnum.GgufMetadataValueTypeString:
					size = (int) br.ReadUInt64();
					break;
				case ModelDataTypeEnum.GgufMetadataValueTypeArray:
					var elementType = (ModelDataTypeEnum) br.ReadUInt32();

					if (elementType == ModelDataTypeEnum.GgufMetadataValueTypeArray)
					{
						throw new NotSupportedException("Nested array is not supported");
					}
					var elementCount = br.ReadUInt64();
					if (elementType == ModelDataTypeEnum.GgufMetadataValueTypeString)
					{
						result.ArrayStrings = new string[elementCount];
						result.ArrayElementType = ModelDataTypeEnum.GgufMetadataValueTypeString;
						for (ulong j = 0; j < elementCount; j++)
						{
							result.ArrayStrings[j] = ReadString(br);
						}
						size = 0;
					}
					else
					{
						result.ArrayElementType = elementType;
						size = elementType.GetDataTypeSize() * (int) elementCount;
					}

					break;
				default:
					size = result.DataType.GetDataTypeSize();
					break;
			}
			if (size > 0)
			{
				result.RawData = br.ReadBytes(size);
			}

			yield return result;
		}
	}

	private string ReadString(BinaryReader reader)
	{
		var l = reader.ReadUInt64();
		var x = reader.ReadBytes((int) l);
		return Encoding.UTF8.GetString(x);
	}

	private IEnumerable<ModelTensorInfo> ReadTensorData(Stream stream, ulong tensorCount)
	{
		using var br = new BinaryReader(stream, Encoding.UTF8, true);
		for (ulong i = 0; i < tensorCount; i++)
		{
			var result = new ModelTensorInfo();
			result.Name = ReadString(br);
			result.DimensionCount = br.ReadUInt32();
			result.Dimensions = ReadArray<ulong>(br, result.DimensionCount).ToArray();
			result.TensorType = (ModelTensorType) br.ReadUInt32();
			result.Offset = br.ReadUInt64();
			yield return result;
		}
	}

	#endregion
}