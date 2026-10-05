#region References

using System;
using System.Buffers.Binary;
using System.Text;
using Cornerstone.Presentation.Media.Fonts.Tables;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.Fonts.Tables;

[TestClass]
public class MetaTableTests
{
	#region Methods

	[PresentationTestMethod]
	public void TryParseDuplicateDlngUsesFirstRecord()
	{
		var bytes = BuildMetaTable(
			("dlng", "ja"),
			("dlng", "en"),
			("slng", "en"));

		CornerstoneTest.IsTrue(MetaTable.TryParse(bytes, out var table));
		CornerstoneTest.AreEqual(new[] { "ja" }, table.DesignLanguages);
		CornerstoneTest.AreEqual(new[] { "en" }, table.SupportedLanguages);
	}

	[PresentationTestMethod]
	public void TryParseEmptySpanReturnsFalse()
	{
		CornerstoneTest.IsFalse(MetaTable.TryParse(ReadOnlySpan<byte>.Empty, out _));
	}

	[PresentationTestMethod]
	public void TryParseReadsDlngAndSlngTags()
	{
		const string dlng = "ja,zh-Hant";
		const string slng = "en";

		var bytes = BuildMetaTable(dlng, slng);

		CornerstoneTest.IsTrue(MetaTable.TryParse(bytes, out var table));
		CornerstoneTest.AreEqual(new[] { "ja", "zh-Hant" }, table.DesignLanguages);
		CornerstoneTest.AreEqual(new[] { "en" }, table.SupportedLanguages);
	}

	[PresentationTestMethod]
	public void TryParseReturnsEmptyArraysWhenOnlyUnknownMaps()
	{
		// Build a meta table with a single data map using an unknown tag.
		var unknownPayload = Encoding.UTF8.GetBytes("ignored");
		const int header = 16;
		const int mapSize = 12;
		var dataOffset = header + mapSize;
		var bytes = new byte[dataOffset + unknownPayload.Length];

		// Header
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(0, 4), 1); // version
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(4, 4), 0); // flags
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(8, 4), 0); // reserved
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(12, 4), 1); // dataMapsCount

		// Data map
		WriteTag(bytes.AsSpan(16, 4), "xxxx");
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20, 4), (uint) dataOffset);
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(24, 4), (uint) unknownPayload.Length);

		unknownPayload.CopyTo(bytes.AsSpan(dataOffset));

		CornerstoneTest.IsTrue(MetaTable.TryParse(bytes, out var table));
		CornerstoneTest.Empty(table.DesignLanguages);
		CornerstoneTest.Empty(table.SupportedLanguages);
	}

	[PresentationTestMethod]
	public void TryParseTrimsWhitespaceAndSkipsEmptyTags()
	{
		var bytes = BuildMetaTable(" ja , , zh-Hant ", "en-US");

		CornerstoneTest.IsTrue(MetaTable.TryParse(bytes, out var table));
		CornerstoneTest.AreEqual(new[] { "ja", "zh-Hant" }, table.DesignLanguages);
		CornerstoneTest.AreEqual(new[] { "en-US" }, table.SupportedLanguages);
	}

	[PresentationTestMethod]
	public void TryParseTruncatedDataMapArrayReturnsFalse()
	{
		var bytes = new byte[16];

		// Header claims one data map but no data map records follow.
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(0, 4), 1); // version
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(4, 4), 0); // flags
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(8, 4), 0); // reserved
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(12, 4), 1); // dataMapsCount

		CornerstoneTest.IsFalse(MetaTable.TryParse(bytes, out _));
	}

	[PresentationTestMethod]
	public void TryParseWrongVersionReturnsFalse()
	{
		var bytes = new byte[16];

		// version 2 is not supported
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(0, 4), 2);

		CornerstoneTest.IsFalse(MetaTable.TryParse(bytes, out _));
	}

	private static byte[] BuildMetaTable(string dlngValue, string slngValue)
	{
		return BuildMetaTable(("dlng", dlngValue), ("slng", slngValue));
	}

	private static byte[] BuildMetaTable(params (string Tag, string Value)[] maps)
	{
		const int header = 16;
		const int mapSize = 12;
		var mapCount = maps.Length;
		var payloadOffset = header + (mapSize * mapCount);
		var payloads = new byte[mapCount][];
		var totalLength = payloadOffset;

		for (var i = 0; i < mapCount; i++)
		{
			payloads[i] = Encoding.UTF8.GetBytes(maps[i].Value);
			totalLength += payloads[i].Length;
		}

		var bytes = new byte[totalLength];

		// Header
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(0, 4), 1); // version
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(4, 4), 0); // flags
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(8, 4), 0); // reserved
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(12, 4), (uint) mapCount); // dataMapsCount

		for (var i = 0; i < mapCount; i++)
		{
			var mapOffset = header + (i * mapSize);
			WriteTag(bytes.AsSpan(mapOffset, 4), maps[i].Tag);
			BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(mapOffset + 4, 4), (uint) payloadOffset);
			BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(mapOffset + 8, 4), (uint) payloads[i].Length);

			payloads[i].CopyTo(bytes.AsSpan(payloadOffset));
			payloadOffset += payloads[i].Length;
		}

		return bytes;
	}

	private static void WriteTag(Span<byte> destination, string tag)
	{
		Encoding.ASCII.GetBytes(tag, destination);
	}

	#endregion
}