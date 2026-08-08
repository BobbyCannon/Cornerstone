#region References

using System;
using System.Text;

#endregion

namespace Cornerstone.Internal;

/// <summary>
/// An implementation of <see cref="IByteArrayEncoder" /> that encodes byte arrays as Base32 strings.
/// </summary>
internal class Base32ByteArrayEncoder : IByteArrayEncoder
{
	#region Constants

	/// <summary>
	/// Gets the Crockford Base32 alphabet.
	/// </summary>
	/// <remarks>
	/// See https://www.crockford.com/base32.html
	/// </remarks>
	public const string CrockfordAlphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

	/// <summary>
	/// Gets the RFC-4648 Base32 alphabet.
	/// </summary>
	/// <remarks>
	/// See https://datatracker.ietf.org/doc/html/rfc4648#section-6
	/// </remarks>
	public const string Rfc4648Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

	#endregion

	#region Fields

	/// <summary>
	/// Gets the alphabet in use.
	/// </summary>
	private readonly string _alphabet;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="Base32ByteArrayEncoder" /> class.
	/// </summary>
	public Base32ByteArrayEncoder() : this(CrockfordAlphabet)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Base32ByteArrayEncoder" /> class.
	/// </summary>
	/// <param name="alphabet"> The alphabet to use. </param>
	public Base32ByteArrayEncoder(string alphabet)
	{
		if (alphabet is null)
		{
			throw new ArgumentNullException(nameof(alphabet));
		}

		if (alphabet.Length != 32)
		{
			throw new ArgumentException("The alphabet must have a length of 32.", nameof(alphabet));
		}

		_alphabet = alphabet;
	}

	#endregion

	#region Methods

	/// <summary>
	/// Encodes the specified byte array as a string.
	/// </summary>
	/// <param name="bytes"> The byte array to encode. </param>
	/// <returns> The byte array encoded as a string. </returns>
	public string Encode(byte[] bytes)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException(nameof(bytes));
		}
		if (bytes.Length == 0)
		{
			return string.Empty;
		}

		var outputLength = ((bytes.Length * 8) + 4) / 5;
		var output = new char[outputLength];
		int inputIndex = 0, outputIndex = 0;

		// Process 5 bytes (40 bits) at a time to produce 8 Base32 chars
		while ((inputIndex + 4) < bytes.Length)
		{
			var buffer = ((long) bytes[inputIndex] << 32) |
				((long) bytes[inputIndex + 1] << 24) |
				((long) bytes[inputIndex + 2] << 16) |
				((long) bytes[inputIndex + 3] << 8) |
				bytes[inputIndex + 4];

			output[outputIndex] = _alphabet[(int) ((buffer >> 35) & 31)];
			output[outputIndex + 1] = _alphabet[(int) ((buffer >> 30) & 31)];
			output[outputIndex + 2] = _alphabet[(int) ((buffer >> 25) & 31)];
			output[outputIndex + 3] = _alphabet[(int) ((buffer >> 20) & 31)];
			output[outputIndex + 4] = _alphabet[(int) ((buffer >> 15) & 31)];
			output[outputIndex + 5] = _alphabet[(int) ((buffer >> 10) & 31)];
			output[outputIndex + 6] = _alphabet[(int) ((buffer >> 5) & 31)];
			output[outputIndex + 7] = _alphabet[(int) (buffer & 31)];

			inputIndex += 5;
			outputIndex += 8;
		}

		// Handle remaining bytes
		if (inputIndex < bytes.Length)
		{
			long buffer = 0;
			for (var i = 0; i < (bytes.Length - inputIndex); i++)
			{
				buffer = (buffer << 8) | bytes[inputIndex + i];
			}

			var bitsLeft = (bytes.Length - inputIndex) * 8;
			var charsToOutput = (bitsLeft + 4) / 5;
			buffer <<= 40 - bitsLeft; // Align to left for correct bit extraction

			for (var i = 0; i < charsToOutput; i++)
			{
				output[outputIndex++] = _alphabet[(int) ((buffer >> (35 - (i * 5))) & 31)];
			}
		}

		return new string(output);
	}

	/// <summary>
	/// Encodes the specified byte array as a string.
	/// </summary>
	/// <param name="bytes"> The byte array to encode. </param>
	/// <returns> The byte array encoded as a string. </returns>
	public string Encode2(byte[] bytes)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException(nameof(bytes));
		}

		if (bytes.Length == 0)
		{
			return string.Empty;
		}

		const int shift = 5;
		const int mask = 31;

		var outputLength = (((bytes.Length * 8) + shift) - 1) / shift;
		var sb = new StringBuilder(outputLength);

		var offset = 0;
		var last = bytes.Length;
		int buffer = bytes[offset++];
		var bitsLeft = 8;
		while ((bitsLeft > 0) || (offset < last))
		{
			if (bitsLeft < shift)
			{
				if (offset < last)
				{
					buffer <<= 8;
					buffer |= bytes[offset++] & 0xff;
					bitsLeft += 8;
				}
				else
				{
					var pad = shift - bitsLeft;
					buffer <<= pad;
					bitsLeft += pad;
				}
			}

			var index = mask & (buffer >> (bitsLeft - shift));
			bitsLeft -= shift;
			sb.Append(_alphabet[index]);
		}

		return sb.ToString();
	}

	#endregion
}

/// <summary>
/// Provides functionality to encode a byte array as a string.
/// </summary>
internal interface IByteArrayEncoder
{
	#region Methods

	/// <summary>
	/// Encodes the specified byte array as a string.
	/// </summary>
	/// <param name="bytes"> The byte array to encode. </param>
	/// <returns> The byte array encoded as a string. </returns>
	string Encode(byte[] bytes);

	#endregion
}