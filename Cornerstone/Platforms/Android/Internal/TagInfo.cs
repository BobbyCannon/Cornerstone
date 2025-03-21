#region References

#endregion

namespace Cornerstone.Platforms.Android.Internal;

/// <summary>
/// Default implementation of <see cref="ITagInfo" />
/// </summary>
public class TagInfo : ITagInfo
{
	#region Constructors

	/// <summary>
	/// Default constructor
	/// </summary>
	public TagInfo()
	{
		IsSupported = true;
	}

	/// <summary>
	/// Custom constructor
	/// </summary>
	/// <param name="identifier"> Tag Identifier </param>
	/// <param name="isNdef"> Is Ndef tag </param>
	public TagInfo(byte[] identifier, bool isNdef = true)
	{
		Identifier = identifier;
		SerialNumber = NfcUtilities.ByteArrayToHexString(identifier);
		IsSupported = isNdef;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Capacity of tag in bytes
	/// </summary>
	public int Capacity { get; set; }

	public byte[] Identifier { get; }

	/// <summary>
	/// Empty tag
	/// </summary>
	public bool IsEmpty => (Records == null) || (Records.Length == 0) || (Records[0] == null) || (Records[0].TypeFormat == NFCNdefTypeFormat.Empty);

	/// <summary>
	/// </summary>
	public bool IsSupported { get; }

	/// <summary>
	/// Writable tag
	/// </summary>
	public bool IsWritable { get; set; }

	/// <summary>
	/// Array of <see cref="NFCNdefRecord" /> of tag
	/// </summary>
	public NFCNdefRecord[] Records { get; set; }

	/// <summary>
	/// Tag Serial Number
	/// </summary>
	public string SerialNumber { get; }

	#endregion

	#region Methods

	public override string ToString()
	{
		return $"TagInfo: identifier: {Identifier}, SerialNumber:{SerialNumber}, Capacity:{Capacity} bytes, IsSupported:{IsSupported}, IsEmpty:{IsEmpty}, IsWritable:{IsWritable}";
	}

	#endregion
}

/// <summary>
/// Interface for ITagInfo
/// </summary>
public interface ITagInfo
{
	#region Properties

	/// <summary>
	/// Capacity of tag in bytes
	/// </summary>
	int Capacity { get; set; }

	/// <summary>
	/// Tag Raw Identifier
	/// </summary>
	byte[] Identifier { get; }

	/// <summary>
	/// Empty tag
	/// </summary>
	bool IsEmpty { get; }

	/// <summary>
	/// Supported tag
	/// </summary>
	bool IsSupported { get; }

	/// <summary>
	/// Writable tag
	/// </summary>
	bool IsWritable { get; set; }

	/// <summary>
	/// Array of <see cref="NFCNdefRecord" /> of tag
	/// </summary>
	NFCNdefRecord[] Records { get; set; }

	/// <summary>
	/// Tag Serial Number
	/// </summary>
	string SerialNumber { get; }

	#endregion
}

/// <summary>
/// Class describing the information containing within a NFC tag
/// </summary>
public class NFCNdefRecord
{
	#region Properties

	/// <summary>
	/// External domain used for <see cref="NFCNdefTypeFormat.External" /> type
	/// </summary>
	public string ExternalDomain { get; set; }

	/// <summary>
	/// External type used for <see cref="NFCNdefTypeFormat.External" /> type
	/// </summary>
	public string ExternalType { get; set; }

	/// <summary>
	/// Two letters ISO 639-1 Language Code (ex: en, fr, de...)
	/// </summary>
	public string LanguageCode { get; set; }

	/// <summary>
	/// String formatted payload
	/// </summary>
	public string Message => NfcUtilities.GetMessage(TypeFormat, Payload, Uri);

	/// <summary>
	/// MimeType used for <see cref="NFCNdefTypeFormat.Mime" /> type
	/// </summary>
	public string MimeType { get; set; } = "text/plain";

	/// <summary>
	/// Payload
	/// </summary>
	public byte[] Payload { get; set; }

	/// <summary>
	/// NDEF Type
	/// </summary>
	public NFCNdefTypeFormat TypeFormat { get; set; }

	/// <summary>
	/// Uri
	/// </summary>
	public string Uri { get; set; }

	#endregion
}

/// <summary>
/// Enumeration of NDEF type
/// </summary>
public enum NFCNdefTypeFormat
{
	Empty = 0x00,
	WellKnown = 0x01,
	Mime = 0x02,
	Uri = 0x03,
	External = 0x04,
	Unknown = 0x05,
	Unchanged = 0x06,
	Reserved = 0x07
}