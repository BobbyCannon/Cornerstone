#region References

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for the string type.
/// </summary>
public static class StringExtensions
{
	#region Fields

	/// <summary>
	/// Covers the common newlines: CR, LF, plus Unicode NEL / LS / PS
	/// </summary>
	public static readonly SearchValues<char> NewLineChars;

	#endregion

	#region Constructors

	static StringExtensions()
	{
		NewLineChars = SearchValues.Create("\r\n\u0085\u2028\u2029");
	}

	#endregion

	#region Methods

	/// <summary>
	/// To literal version of the string.
	/// </summary>
	/// <param name="input"> The string input. </param>
	/// <returns> The literal version of the string. </returns>
	public static string Escape(this string input)
	{
		if (input == null)
		{
			// todo: which one is more correct?
			//return "null";
			return null;
		}

		using var rented = StringBuilderPool.Rent(input.Length);
		var builder = rented.Value;

		try
		{
			foreach (var c in input)
			{
				if (TryProcessCharacter(c, builder))
				{
					continue;
				}

				if (c == 0)
				{
					builder.Append(@"\u");
					builder.Append(((int) c).ToString("X4"));
				}
				else if ((c >= 0x20) && (c <= 0x7e))
				{
					// ASCII printable character
					builder.Append(c);
				}
				else
				{
					// As UTF16 escaped character
					builder.Append(@"\u");
					builder.Append(((int) c).ToString("X4"));
				}
			}

			return builder.ToString();
		}
		finally
		{
			StringBuilderPool.Return(builder);
		}
	}

	/// <summary>
	/// Return the first string that is not null or empty.
	/// </summary>
	/// <param name="collection"> The collection of string to parse. </param>
	public static string FirstNotNullOrEmptyValue(this IEnumerable<string> collection)
	{
		return collection.FirstOrDefault(item => !string.IsNullOrEmpty(item));
	}

	/// <summary>
	/// Convert string from a base 64 string.
	/// </summary>
	/// <param name="data"> The data to be converted. </param>
	/// <returns> The unencoded string. </returns>
	public static string FromBase64String(this string data)
	{
		var bytes = System.Convert.FromBase64String(data);
		return Encoding.UTF8.GetString(bytes);
	}

	/// <summary>
	/// Convert string from a base 64 string.
	/// </summary>
	/// <param name="data"> The data to be converted. </param>
	/// <returns> The unencoded byte array. </returns>
	public static byte[] FromBase64StringToByteArray(this string data)
	{
		const string key = ";base64,";
		var index = data.IndexOf(key);
		if (index >= 0)
		{
			data = data.Substring(index + key.Length);
		}

		return System.Convert.FromBase64String(data);
	}

	/// <summary>
	/// Convert the hex string back to byte array.
	/// </summary>
	/// <param name="value"> The hex string to be converter. </param>
	/// <returns> The byte array. </returns>
	public static ReadOnlySpan<byte> FromHexStringToByteArray(this string value)
	{
		Span<byte> buffer = stackalloc byte[value.Length / 2];
		var status = System.Convert.FromHexString(value, buffer, out var charsConsumed, out var bytesWritten);

		if ((status == OperationStatus.Done) && (bytesWritten == buffer.Length))
		{
			return buffer.ToArray();
		}

		// Handle invalid SHA (e.g., log error, throw, or set to empty)
		return ReadOnlySpan<byte>.Empty;
	}

	/// <summary>
	/// Gets a stable hash code for a string value.
	/// </summary>
	/// <param name="value"> The string value. </param>
	/// <returns> The hash code for the value. </returns>
	public static int GetStableHashCode(this string value)
	{
		if (value is null)
		{
			throw new ArgumentNullException(nameof(value));
		}

		unchecked
		{
			var hash1 = 5381;
			var hash2 = hash1;

			for (var i = 0; i < value.Length; i += 2)
			{
				hash1 = ((hash1 << 5) + hash1) ^ value[i];
				if (i == (value.Length - 1))
				{
					break;
				}
				hash2 = ((hash2 << 5) + hash2) ^ value[i + 1];
			}

			return hash1 + (hash2 * 1566083941);
		}
	}

	/// <summary>
	/// Check to see if the value is only new line characters.
	/// </summary>
	/// <param name="value"> The value to check. </param>
	/// <returns> True if the value is only newlines otherwise false. </returns>
	public static bool IsNewLines(this string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}

		// Returns true only when every character is one of the newline chars
		return value.AsSpan().IndexOfAnyExcept(NewLineChars) < 0;
	}

	/// <summary>
	/// Trims string to a maximum length.
	/// </summary>
	/// <param name="value"> The value to process. </param>
	/// <param name="max"> The maximum length of the string. </param>
	/// <param name="addEllipses"> The option to add ellipses to shorted strings. Defaults to false. </param>
	/// <returns> The value limited to the maximum length. </returns>
	public static string MaxLength(this string value, int max, bool addEllipses = false)
	{
		if (string.IsNullOrWhiteSpace(value) || (max <= 0))
		{
			return string.Empty;
		}

		if (value.Length <= max)
		{
			return value;
		}

		var copyLength = addEllipses && (max >= 4) ? max - 3 : max;

		return string.Create(max, (value, copyLength), (span, state) =>
		{
			state.value.AsSpan().Slice(0, state.copyLength).CopyTo(span);
			if (state.copyLength >= max)
			{
				return;
			}
			span[state.copyLength] = '.';
			span[state.copyLength + 1] = '.';
			span[state.copyLength + 2] = '.';
		});
	}

	/// <summary>
	/// Splits the values into an array using the delimiter
	/// </summary>
	/// <param name="value"> The roles for the account. </param>
	/// <param name="delimiter"> The delimiter to split on </param>
	/// <returns> The array of values. </returns>
	public static string[] SplitTagsIntoArray(this string value, string delimiter = ",")
	{
		return value?.Split([delimiter], StringSplitOptions.RemoveEmptyEntries) ?? [];
	}

	/// <summary>
	/// Convert the text to a camel case string.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The string in the desired format. </returns>
	public static string ToCamelCase(this string value)
	{
		using var rented = StringBuilderPool.Rent();
		var builder = rented.Value;
		var nextCharUpper = false;

		for (var i = 0; i < value.Length; i++)
		{
			var c = value[i];

			if (i == 0)
			{
				builder.Append(char.IsUpper(c) ? char.ToLower(c) : c);
				continue;
			}

			if ((c == ' ') || !char.IsLetterOrDigit(c))
			{
				nextCharUpper = true;
				continue;
			}

			if (nextCharUpper)
			{
				builder.Append(char.ToUpper(c));
				nextCharUpper = false;
				continue;
			}

			builder.Append(c);
		}
		return builder.ToString();
	}

	/// <summary>
	/// Calculate an MD5 hash for the string.
	/// </summary>
	/// <param name="input"> The string to hash. </param>
	/// <returns> The MD5 formatted hash for the input. </returns>
	public static string ToMd5HashHexString(this string input)
	{
		// Calculate MD5 hash from input.
		var inputBytes = Encoding.ASCII.GetBytes(input);

		// Calculate MD5 hash from input.
		var md5 = MD5.Create();
		var hash = md5.ComputeHash(inputBytes);

		// Convert byte array to hex string.
		var sb = new StringBuilder();
		foreach (var item in hash)
		{
			sb.Append(item.ToString("X2"));
		}

		// Return the MD5 string.
		return sb.ToString().ToLower();
	}

	/// <summary>
	/// Convert a string into a secure string.
	/// </summary>
	/// <param name="input"> The string. </param>
	/// <param name="makeReadOnly"> Option to make the SecureString read only. </param>
	/// <returns> The secure string. </returns>
	public static SecureString ToSecureString(this string input, bool makeReadOnly = false)
	{
		if (input == null)
		{
			return null;
		}

		var secure = new SecureString();
		foreach (var c in input)
		{
			secure.AppendChar(c);
		}
		if (makeReadOnly)
		{
			secure.MakeReadOnly();
		}
		return secure;
	}

	public static bool TryProcessCharacter(char c, StringBuilder builder)
	{
		switch (c)
		{
			case '\'':
				builder.Append(@"\'");
				return true;
			case '\"':
				builder.Append("\\\"");
				return true;
			case '\\':
				builder.Append(@"\\");
				return true;
			case '\0':
				builder.Append(@"\0");
				return true;
			case '\a':
				builder.Append(@"\a");
				return true;
			case '\b':
				builder.Append(@"\b");
				return true;
			case '\f':
				builder.Append(@"\f");
				return true;
			case '\n':
				builder.Append(@"\n");
				return true;
			case '\r':
				builder.Append(@"\r");
				return true;
			case '\t':
				builder.Append(@"\t");
				return true;
			case '\v':
				builder.Append(@"\v");
				return true;
			default:
				return false;
		}
	}

	#endregion
}