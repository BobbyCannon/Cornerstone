#region References

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.Platform;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers.Fonts;

/// <summary>
/// An editable, in-memory sfnt font for robustness / malformed-input tests.
/// </summary>
/// <remarks>
/// <para>
/// The font subsystem reads every table through
/// <see cref="IFontMemory.TryGetTable" />, and each OpenType table is self-contained
/// (its internal offsets are relative to the table start, never to the file). That
/// means a faithful test font is just a <c> tag → bytes </c> map: <see cref="SyntheticFont" />
/// parses a real font's sfnt table directory into one, lets a test mutate a single
/// table (truncate it, remove it, or patch a specific offset / count to a hostile
/// value), and hands the result back as an <see cref="IPlatformTypeface" /> that serves
/// the (possibly corrupted) tables verbatim.
/// </para>
/// <para>
/// Seeding from a real font (rather than hand-building every required table) keeps the
/// base font valid for free, so a test can corrupt exactly one thing and attribute any
/// behaviour change to that corruption. Use <see cref="ToPlatformTypeface" /> for the
/// common path (it drives <see cref="GlyphTypeface" /> and the table parsers directly);
/// use <see cref="ToBytes" /> when a test specifically needs the real
/// <c> UnmanagedFontMemory </c> sfnt-directory parser in the loop.
/// </para>
/// </remarks>
public sealed class SyntheticFont
{
	#region Fields

	private readonly uint _sfntVersion;

	// Tables in declaration order is irrelevant to the consumers (they look up by tag),
	// but a stable order keeps ToBytes() deterministic.
	private readonly Dictionary<OpenTypeTag, byte[]> _tables;

	#endregion

	#region Constructors

	private SyntheticFont(uint sfntVersion, Dictionary<OpenTypeTag, byte[]> tables)
	{
		_sfntVersion = sfntVersion;
		_tables = tables;
	}

	#endregion

	#region Properties

	/// <summary> The tags present in the font. </summary>
	public IReadOnlyCollection<OpenTypeTag> Tags => _tables.Keys;

	#endregion

	#region Methods

	/// <summary> Whether the named table is present. </summary>
	public bool Contains(string tag)
	{
		return _tables.ContainsKey(OpenTypeTag.Parse(tag));
	}

	/// <summary>
	/// Builds a <see cref="GlyphTypeface" /> via the public constructor, which does
	/// <b> not </b> swallow parse exceptions — use this to assert whether a corruption
	/// throws out of construction (vs. degrading gracefully).
	/// </summary>
	public GlyphTypeface CreateGlyphTypeface(FontSimulations simulations = FontSimulations.None)
	{
		return new(ToPlatformTypeface(), simulations);
	}

	/// <summary> Loads and parses one of the embedded test font assets. </summary>
	public static SyntheticFont FromAsset(string assetUri)
	{
		var assetLoader = new StandardAssetLoader();
		using var stream = assetLoader.Open(new Uri(assetUri));
		return FromStream(stream);
	}

	/// <summary>
	/// Parses the sfnt table directory of <paramref name="font" /> into an editable
	/// table set. Each table is copied into its own array so later mutation can't
	/// alias the source bytes. TrueType Collections (<c> ttcf </c>) are not supported.
	/// </summary>
	public static SyntheticFont FromBytes(ReadOnlySpan<byte> font)
	{
		if (font.Length < 12)
		{
			throw new ArgumentException("Not a valid sfnt: shorter than the offset table.", nameof(font));
		}

		var sfntVersion = BinaryPrimitives.ReadUInt32BigEndian(font);

		// 'ttcf' — a font collection. Out of scope: callers pass single-font assets.
		if (sfntVersion == 0x74746366)
		{
			throw new NotSupportedException("TrueType Collections are not supported by SyntheticFont.");
		}

		int numTables = BinaryPrimitives.ReadUInt16BigEndian(font.Slice(4));

		var tables = new Dictionary<OpenTypeTag, byte[]>(numTables);

		for (var i = 0; i < numTables; i++)
		{
			var record = 12 + (i * 16);
			if ((record + 16) > font.Length)
			{
				throw new ArgumentException("Not a valid sfnt: table directory exceeds the file.", nameof(font));
			}

			var tag = new OpenTypeTag(BinaryPrimitives.ReadUInt32BigEndian(font.Slice(record)));
			var offset = (int) BinaryPrimitives.ReadUInt32BigEndian(font.Slice(record + 8));
			var length = (int) BinaryPrimitives.ReadUInt32BigEndian(font.Slice(record + 12));

			if ((offset < 0) || (length < 0) || (((long) offset + length) > font.Length))
			{
				throw new ArgumentException($"Not a valid sfnt: table '{tag}' is out of range.", nameof(font));
			}

			tables[tag] = font.Slice(offset, length).ToArray();
		}

		return new SyntheticFont(sfntVersion, tables);
	}

	/// <summary> Parses a font from a stream. </summary>
	public static SyntheticFont FromStream(Stream stream)
	{
		using var ms = new MemoryStream();
		stream.CopyTo(ms);
		return FromBytes(ms.ToArray());
	}

	/// <summary> Returns a copy of the named table's current bytes. </summary>
	public byte[] GetTable(string tag)
	{
		var bytes = Require(tag);
		return (byte[]) bytes.Clone();
	}

	/// <summary> Runs an arbitrary edit against the named table's backing array. </summary>
	public SyntheticFont Mutate(string tag, Action<byte[]> edit)
	{
		edit(Require(tag));
		return this;
	}

	/// <summary> Overwrites a big-endian <c> uint16 </c> at <paramref name="offset" /> within the named table. </summary>
	public SyntheticFont PatchUInt16(string tag, int offset, ushort value)
	{
		BinaryPrimitives.WriteUInt16BigEndian(EditableSlice(tag, offset, 2), value);
		return this;
	}

	/// <summary> Overwrites a big-endian <c> uint32 </c> at <paramref name="offset" /> within the named table. </summary>
	public SyntheticFont PatchUInt32(string tag, int offset, uint value)
	{
		BinaryPrimitives.WriteUInt32BigEndian(EditableSlice(tag, offset, 4), value);
		return this;
	}

	/// <summary> Overwrites a single byte at <paramref name="offset" /> within the named table. </summary>
	public SyntheticFont PatchUInt8(string tag, int offset, byte value)
	{
		EditableSlice(tag, offset, 1)[0] = value;
		return this;
	}

	/// <summary> Removes the named table from the font (no-op if absent). </summary>
	public SyntheticFont Remove(string tag)
	{
		_tables.Remove(OpenTypeTag.Parse(tag));
		return this;
	}

	/// <summary> Replaces the named table's bytes wholesale (adding it if absent). </summary>
	public SyntheticFont Replace(string tag, byte[] bytes)
	{
		_tables[OpenTypeTag.Parse(tag)] = bytes ?? throw new ArgumentNullException(nameof(bytes));
		return this;
	}

	/// <summary> The current byte length of the named table. </summary>
	public int TableLength(string tag)
	{
		return Require(tag).Length;
	}

	/// <summary>
	/// Re-assembles the current table set into a valid sfnt byte array (offset table +
	/// 4-byte-aligned tables, directory sorted by tag). Table checksums are written as
	/// zero — the loader does not validate them. Use this only when a test needs the
	/// real <c> UnmanagedFontMemory </c> directory parser in the loop; otherwise prefer
	/// <see cref="ToPlatformTypeface" />.
	/// </summary>
	public byte[] ToBytes()
	{
		var ordered = _tables.OrderBy(kvp => (uint) kvp.Key).ToArray();
		var numTables = ordered.Length;

		var directorySize = 12 + (numTables * 16);
		var totalSize = directorySize;
		foreach (var kvp in ordered)
		{
			totalSize += Align4(kvp.Value.Length);
		}

		var font = new byte[totalSize];
		var span = font.AsSpan();

		// Offset table.
		BinaryPrimitives.WriteUInt32BigEndian(span, _sfntVersion);
		BinaryPrimitives.WriteUInt16BigEndian(span.Slice(4), (ushort) numTables);

		var maxPow2 = 1;
		var entrySelector = 0;
		while ((maxPow2 * 2) <= numTables)
		{
			maxPow2 *= 2;
			entrySelector++;
		}
		var searchRange = maxPow2 * 16;
		BinaryPrimitives.WriteUInt16BigEndian(span.Slice(6), (ushort) searchRange);
		BinaryPrimitives.WriteUInt16BigEndian(span.Slice(8), (ushort) entrySelector);
		BinaryPrimitives.WriteUInt16BigEndian(span.Slice(10), (ushort) ((numTables * 16) - searchRange));

		// Directory records + table bodies.
		var dataOffset = directorySize;
		for (var i = 0; i < numTables; i++)
		{
			var (tag, bytes) = ordered[i];
			var record = 12 + (i * 16);

			BinaryPrimitives.WriteUInt32BigEndian(span.Slice(record), tag);
			BinaryPrimitives.WriteUInt32BigEndian(span.Slice(record + 4), 0u); // checksum (not validated)
			BinaryPrimitives.WriteUInt32BigEndian(span.Slice(record + 8), (uint) dataOffset);
			BinaryPrimitives.WriteUInt32BigEndian(span.Slice(record + 12), (uint) bytes.Length);

			bytes.AsSpan().CopyTo(span.Slice(dataOffset));
			dataOffset += Align4(bytes.Length);
		}

		return font;
	}

	/// <summary>
	/// Returns an <see cref="IPlatformTypeface" /> that serves the current (possibly
	/// corrupted) tables. This is the primary seam: it drives the
	/// <see cref="GlyphTypeface" /> constructor and every table parser without an sfnt
	/// round-trip. Each call snapshots the current table set, so a typeface is
	/// unaffected by later mutation of this <see cref="SyntheticFont" />.
	/// </summary>
	public IPlatformTypeface ToPlatformTypeface(string familyName = "Synthetic")
	{
		var snapshot = new Dictionary<OpenTypeTag, byte[]>(_tables.Count);
		foreach (var kvp in _tables)
		{
			snapshot[kvp.Key] = (byte[]) kvp.Value.Clone();
		}

		return new SyntheticPlatformTypeface(snapshot, familyName);
	}

	/// <summary>
	/// Truncates the named table to <paramref name="newLength" /> bytes — the canonical
	/// "table is shorter than its header claims" corruption.
	/// </summary>
	public SyntheticFont Truncate(string tag, int newLength)
	{
		var bytes = Require(tag);
		if ((newLength < 0) || (newLength > bytes.Length))
		{
			throw new ArgumentOutOfRangeException(nameof(newLength), newLength,
				$"Truncation length must be in [0, {bytes.Length}] for table '{tag}'.");
		}

		_tables[OpenTypeTag.Parse(tag)] = bytes.AsSpan(0, newLength).ToArray();
		return this;
	}

	/// <summary>
	/// Attempts to build a <see cref="GlyphTypeface" /> over the current tables via
	/// <see cref="GlyphTypeface.TryCreate" /> — returns <c> null </c> when the font is
	/// rejected (the swallow-and-deny path: any parse exception during construction is caught
	/// and turned into a null result).
	/// </summary>
	public GlyphTypeface TryCreateGlyphTypeface(FontSimulations simulations = FontSimulations.None)
	{
		return GlyphTypeface.TryCreate(ToPlatformTypeface(), simulations);
	}

	private static int Align4(int length)
	{
		return (length + 3) & ~3;
	}

	private Span<byte> EditableSlice(string tag, int offset, int size)
	{
		var bytes = Require(tag);
		if ((offset < 0) || (((long) offset + size) > bytes.Length))
		{
			throw new ArgumentOutOfRangeException(nameof(offset), offset,
				$"[{offset}, {offset + size}) is out of range for table '{tag}' (length {bytes.Length}).");
		}

		return bytes.AsSpan(offset, size);
	}

	private byte[] Require(string tag)
	{
		if (!_tables.TryGetValue(OpenTypeTag.Parse(tag), out var bytes))
		{
			throw new InvalidOperationException($"Font has no '{tag}' table.");
		}

		return bytes;
	}

	#endregion

	#region Classes

	/// <summary> Well-known embedded test font asset URIs, all in Cornerstone.Presentation.UnitTests.Base. </summary>
	public static class Assets
	{
		#region Constants

		/// <summary> Variable CFF2 <c> .otf </c> (blend / vstore). </summary>
		public const string AdobeVfPrototype = Prefix + "AdobeVFPrototype-Subset.otf" + Suffix;

		/// <summary> PostScript (CFF / Type2) <c> .otf </c>. </summary>
		public const string CffTest = Prefix + "CffTest.otf" + Suffix;

		/// <summary> CID-keyed CFF <c> .otf </c> (FDSelect / FDArray). </summary>
		public const string CidTest = Prefix + "CidTest.otf" + Suffix;

		/// <summary> Static TrueType (<c> glyf </c>) font. </summary>
		public const string InterRegular = Prefix + "Inter-Regular.ttf" + Suffix;

		/// <summary> Variable TrueType font: carries <c> fvar </c>/<c> avar </c>/<c> gvar </c>/<c> HVAR </c>/<c> MVAR </c>. </summary>
		public const string InterVariable = Prefix + "InterVariable.ttf" + Suffix;

		private const string Prefix = "resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts.";
		private const string Suffix = "?assembly=Cornerstone.Presentation.UnitTests";

		#endregion
	}

	/// <summary>
	/// An <see cref="IPlatformTypeface" /> that serves a fixed <c> tag → bytes </c> map.
	/// No SkiaSharp face, no real stream — just enough for the managed font pipeline.
	/// </summary>
	private sealed class SyntheticPlatformTypeface : IPlatformTypeface
	{
		#region Fields

		private readonly Dictionary<OpenTypeTag, byte[]> _tables;

		#endregion

		#region Constructors

		public SyntheticPlatformTypeface(Dictionary<OpenTypeTag, byte[]> tables, string familyName)
		{
			_tables = tables;
			FamilyName = familyName;
		}

		#endregion

		#region Properties

		public string FamilyName { get; }
		public FontSimulations FontSimulations => FontSimulations.None;
		public FontStretch Stretch => FontStretch.Normal;
		public FontStyle Style => FontStyle.Normal;
		public FontWeight Weight => FontWeight.Normal;

		#endregion

		#region Methods

		public void Dispose()
		{
		}

		public bool TryGetStream([NotNullWhen(true)] out Stream stream)
		{
			stream = null;
			return false;
		}

		public bool TryGetTable(OpenTypeTag tag, out ReadOnlyMemory<byte> table)
		{
			if (_tables.TryGetValue(tag, out var bytes))
			{
				table = bytes;
				return true;
			}

			table = default;
			return false;
		}

		#endregion
	}

	#endregion
}