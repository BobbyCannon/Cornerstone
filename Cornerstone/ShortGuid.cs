#region References

using System;

#endregion

namespace Cornerstone;

/// <summary>
/// Represents a globally unique identifier (GUID) with a shorter string value.
/// </summary>
public readonly struct ShortGuid : IComparable<ShortGuid>, IComparable, IEquatable<ShortGuid>
{
	#region Fields

	/// <summary>
	/// A read-only instance of the ShortGuid class whose value
	/// is guaranteed to be all zeroes.
	/// </summary>
	public static readonly string EmptyValue = Encode(Guid.Empty);

	/// <summary>
	/// A read-only instance of the ShortGuid class whose value
	/// is guaranteed to be all ones.
	/// </summary>
	public static readonly ShortGuid AllBitsSet = new(Guid.AllBitsSet);

	/// <summary>
	/// A read-only instance of the ShortGuid class whose value
	/// is guaranteed to be all zeroes.
	/// </summary>
	public static readonly ShortGuid Empty = new(Guid.Empty);

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a ShortGuid.
	/// </summary>
	public ShortGuid()
	{
		Guid = Guid.Empty;
		Value = EmptyValue;
	}

	/// <summary>
	/// Creates a ShortGuid from a base64 encoded string.
	/// </summary>
	/// <param name="value">
	/// The encoded guid as a base64 string.
	/// </param>
	public ShortGuid(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			Guid = Empty;
			Value = EmptyValue;
		}
		else if (value.Length == 22)
		{
			Guid = Decode(value);
			Value = value;
		}
		else
		{
			Guid = Guid.Parse(value);
			Value = Encode(Guid);
		}
	}

	/// <summary>
	/// Creates a ShortGuid from a Guid.
	/// </summary>
	/// <param name="guid"> The Guid to encode. </param>
	public ShortGuid(Guid guid)
	{
		Value = Encode(guid);
		Guid = guid;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets/sets the underlying Guid.
	/// </summary>
	public Guid Guid { get; init; }

	/// <summary>
	/// Gets/sets the underlying base64 encoded string.
	/// </summary>
	public string Value { get; init; }

	#endregion

	#region Methods

	public int CompareTo(ShortGuid other)
	{
		var guidComparison = Guid.CompareTo(other.Guid);
		if (guidComparison != 0)
		{
			return guidComparison;
		}
		return string.Compare(Value, other.Value, StringComparison.Ordinal);
	}

	public int CompareTo(object obj)
	{
		return CompareTo(obj is ShortGuid guid ? guid : default);
	}

	public override bool Equals(object obj)
	{
		return obj switch
		{
			ShortGuid sGuid => Guid.Equals(sGuid.Guid),
			Guid guid => Guid.Equals(guid),
			string sValue => Guid.Equals(new ShortGuid(sValue).Guid),
			_ => false
		};
	}

	public bool Equals(ShortGuid other)
	{
		return Value == other.Value;
	}

	/// <summary>
	/// Returns the HashCode for underlying Guid.
	/// </summary>
	/// <returns> </returns>
	public override int GetHashCode()
	{
		return Guid.GetHashCode();
	}

	/// <summary>
	/// Initializes a new instance of the ShortGuid class.
	/// </summary>
	/// <returns> </returns>
	public static ShortGuid NewGuid()
	{
		return new(Guid.NewGuid());
	}

	/// <summary>
	/// Determines if both ShortGuids have the same underlying.
	/// Guid value.
	/// </summary>
	/// <param name="x"> The guid to be tested. </param>
	/// <param name="y"> The guid to be tested against. </param>
	/// <returns> Returns true if the values match or otherwise false if they do not match. </returns>
	public static bool operator ==(ShortGuid x, ShortGuid y)
	{
		return x.Guid == y.Guid;
	}

	/// <summary>
	/// Implicitly converts the ShortGuid to its string equivalent
	/// </summary>
	/// <param name="shortGuid"> </param>
	/// <returns> </returns>
	public static implicit operator string(ShortGuid shortGuid)
	{
		return shortGuid.Value;
	}

	/// <summary>
	/// Implicitly converts the ShortGuid to it's Guid equivalent
	/// </summary>
	/// <param name="shortGuid"> </param>
	/// <returns> </returns>
	public static implicit operator Guid(ShortGuid shortGuid)
	{
		return shortGuid.Guid;
	}

	/// <summary>
	/// Implicitly converts the string to a ShortGuid
	/// </summary>
	/// <param name="shortGuid"> </param>
	/// <returns> </returns>
	public static implicit operator ShortGuid(string shortGuid)
	{
		return new(shortGuid);
	}

	/// <summary>
	/// Implicitly converts the Guid to a ShortGuid
	/// </summary>
	/// <param name="guid"> </param>
	/// <returns> </returns>
	public static implicit operator ShortGuid(Guid guid)
	{
		return new(guid);
	}

	/// <summary>
	/// Determines if both ShortGuids do not have the
	/// same underlying Guid value.
	/// </summary>
	/// <param name="x"> The guid to be tested. </param>
	/// <param name="y"> The guid to be tested against. </param>
	/// <returns> Returns true if the values do not match or otherwise false if they do match. </returns>
	public static bool operator !=(ShortGuid x, ShortGuid y)
	{
		return !(x == y);
	}

	/// <summary>
	/// Creates a new short Guid from the provided string.
	/// </summary>
	/// <param name="value"> The guid (full or short) to be parsed. </param>
	/// <returns> The short guid version of the string value. </returns>
	public static ShortGuid Parse(string value)
	{
		return new ShortGuid(value);
	}

	/// <summary>
	/// Returns the base64 encoded guid as a string.
	/// </summary>
	/// <returns> </returns>
	public override string ToString()
	{
		return Value;
	}

	/// <summary>
	/// Creates a new short Guid from the provided string.
	/// </summary>
	/// <param name="value"> The guid (full or short) to be parsed. </param>
	/// <param name="guid"> The short guid version of the string value. </param>
	/// <returns> True if the parse was successful otherwise false. </returns>
	public static bool TryParse(string value, out ShortGuid guid)
	{
		try
		{
			guid = new ShortGuid(value);
			return true;
		}
		catch
		{
			guid = default;
			return false;
		}
	}

	/// <summary>
	/// Decodes the given base64 string.
	/// </summary>
	/// <param name="value"> The base64 encoded string of a Guid. </param>
	/// <returns> A new Guid </returns>
	private static Guid Decode(string value)
	{
		Span<char> buffer = stackalloc char[24];

		value.AsSpan().CopyTo(buffer);
		buffer[22] = '=';
		buffer[23] = '=';

		for (var i = 0; i < 22; i++)
		{
			buffer[i] = buffer[i] switch
			{
				'-' => '+',
				'_' => '/',
				_ => buffer[i]
			};
		}

		Span<byte> bytes = stackalloc byte[16];
		if (!System.Convert.TryFromBase64Chars(buffer, bytes, out _))
		{
			throw new FormatException("Invalid base64 string.");
		}

		return new Guid(bytes);
	}

	/// <summary>
	/// Encodes the given Guid as a base64 string that is 22.
	/// characters long.
	/// </summary>
	/// <param name="guid"> The Guid to encode. </param>
	/// <returns> The short string that is only 22 characters. </returns>
	private static string Encode(Guid guid)
	{
		var bytes = guid.ToByteArray();
		var charArray = new char[24]; // Base64 for 16 bytes = 24 chars
		System.Convert.ToBase64CharArray(bytes, 0, bytes.Length, charArray, 0);

		// Replace '+' with '-' and '/' with '_' for URL-safe encoding
		for (var i = 0; i < 22; i++)
		{
			charArray[i] = charArray[i] switch
			{
				'+' => '-',
				'/' => '_',
				_ => charArray[i]
			};
		}

		return new string(charArray, 0, 22);
	}

	#endregion
}