#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Cornerstone.Serialization.Json;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for the string type.
/// </summary>
public static class StringExtensions
{
	#region Methods

	/// <summary>
	/// Calculate an MD5 hash for the string.
	/// </summary>
	/// <param name="input"> The string to hash. </param>
	/// <returns> The MD5 formatted hash for the input. </returns>
	public static string CalculateMd5Hash(this string input)
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
	/// Check string for a camel case match.
	/// </summary>
	/// <param name="text"> The text to check. </param>
	/// <param name="value"> The value to check. These characters don't have to be upper case. </param>
	/// <returns> True if the value is a camel case match. </returns>
	/// <remarks>
	/// Ex. TheQuickFox would return true with [tqf] value.
	/// </remarks>
	public static bool CamelCaseMatch(this string text, string value)
	{
		// We take the first letter of the text regardless of whether it's upper case, so we match
		// against camelCase text as well as PascalCase text ("cct" matches "camelCaseText")
		var theFirstLetterOfEachWord = text
			.Take(1)
			.Concat(text.Skip(1).Where(char.IsUpper))
			.ToList();
		var i = 0;

		foreach (var letter in theFirstLetterOfEachWord)
		{
			if (i > (value.Length - 1))
			{
				return true; // return true here for CamelCase partial match ("CQ" matches "CodeQualityAnalysis")
			}

			if (char.ToUpperInvariant(value[i]) != char.ToUpperInvariant(letter))
			{
				return false;
			}

			i++;
		}

		return i >= value.Length;
	}

	/// <summary>
	/// Combine the values into a single delimited string.
	/// </summary>
	/// <param name="values"> The values to combine. </param>
	/// <param name="delimiter"> The delimiter to combine with. </param>
	/// <param name="distinct"> Option to distinct the values. Defaults to true. </param>
	/// <returns> The values combine into a single delimited string. </returns>
	public static string CombineArray(this IEnumerable<string> values, string delimiter = ",", bool distinct = true)
	{
		return values != null
			? $"{delimiter}{string.Join(delimiter,
				distinct
					? values.Distinct().OrderBy(x => x)
					: values.OrderBy(x => x)
			)}{delimiter}"
			: delimiter + delimiter;
	}

	/// <summary>
	/// Check to see if a string contains any of the provided characters.
	/// </summary>
	/// <param name="value"> The string value. </param>
	/// <param name="characters"> The characters to validate. </param>
	/// <returns> True if the string contains any of the provided characters. </returns>
	public static bool ContainsAny(this string value, params char[] characters)
	{
		return ContainsAny(value, null, characters);
	}

	/// <summary>
	/// Check to see if a string contains any of the provided characters.
	/// </summary>
	/// <param name="value"> The string value. </param>
	/// <param name="comparer"> An optional char comparer. </param>
	/// <param name="characters"> The characters to validate. </param>
	/// <returns> True if the string contains any of the provided characters. </returns>
	public static bool ContainsAny(this string value, IEqualityComparer<char> comparer = null, params char[] characters)
	{
		return (characters.Length > 0) && value.Any(c => characters.Contains(c, comparer));
	}

	/// <summary>
	/// Checks to see if the string (value) ends with start of the provided string.
	/// </summary>
	/// <param name="value"> The value to test. </param>
	/// <param name="startOf"> The string to check against. </param>
	/// <param name="match"> The match if found. </param>
	/// <param name="ignoreCase"> Option to ignore case. </param>
	/// <returns> True if a match was found otherwise false. </returns>
	public static bool EndsWithStartOf(this string value, string startOf, out string match, bool ignoreCase)
	{
		if ((value == null) || (startOf == null))
		{
			match = null;
			return false;
		}

		var equals = false;

		for (var length = startOf.Length; length >= 0; length--)
		{
			for (var offset = 0; offset < length; offset++)
			{
				var a = startOf[length - offset - 1];
				var valueOffset = value.Length - offset - 1;
				if (valueOffset < 0)
				{
					match = null;
					return false;
				}

				var b = value[value.Length - offset - 1];
				equals = b.Equals(a, ignoreCase);

				if (!equals)
				{
					break;
				}
			}

			if (equals)
			{
				match = value.Substring(value.Length - length);
				return true;
			}
		}

		match = null;
		return false;
	}

	/// <summary>
	/// Compare char values with option to ignore case.
	/// </summary>
	/// <param name="value1"> The first value. </param>
	/// <param name="value2"> The second value. </param>
	/// <param name="ignoreCase"> Option to ignore case. </param>
	/// <returns> True if the characters are equal otherwise false. </returns>
	public static bool Equals(this char value1, char value2, bool ignoreCase)
	{
		if (value1.Equals(value2))
		{
			return true;
		}

		if (ignoreCase)
		{
			return (char.ToLowerInvariant(value1) == char.ToLowerInvariant(value2))
				|| (char.ToUpperInvariant(value1) == char.ToUpperInvariant(value2));
		}

		return false;
	}

	/// <summary>
	/// To literal version of the string.
	/// </summary>
	/// <param name="input"> The string input. </param>
	/// <returns> The literal version of the string. </returns>
	public static string Escape(this string input)
	{
		if (input == null)
		{
			return "null";
		}

		var builder = new StringBuilder(input.Length);

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
		var key = ";base64,";
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
	public static byte[] FromHexStringToArray(this string value)
	{
		var bytes = new byte[value.Length / 2];

		for (var i = 0; i < bytes.Length; i++)
		{
			bytes[i] = System.Convert.ToByte(value.Substring(i * 2, 2), 16);
		}

		return bytes;
	}

	/// <summary>
	/// Get the comparer for the comparison provided.
	/// </summary>
	/// <param name="comparison"> The string comparision type. </param>
	/// <returns> The comparer for the comparison type. </returns>
	public static IComparer GetComparer(this StringComparison comparison)
	{
		return comparison switch
		{
			StringComparison.CurrentCulture => StringComparer.CurrentCulture,
			StringComparison.CurrentCultureIgnoreCase => StringComparer.CurrentCultureIgnoreCase,
			StringComparison.Ordinal => StringComparer.Ordinal,
			StringComparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
			StringComparison.InvariantCulture => StringComparer.InvariantCulture,
			StringComparison.InvariantCultureIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
			_ => StringComparer.Ordinal
		};
	}

	/// <summary>
	/// Get the last index (offset) of the string value.
	/// </summary>
	/// <param name="value"> The value to process. </param>
	/// <returns> Return the last index or otherwise -1. </returns>
	public static int GetLastIndex(this string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return -1;
		}

		return value.Length - 1;
	}

	/// <summary>
	/// Gets a stable hash code for a string value.
	/// </summary>
	/// <param name="value"> The string value. </param>
	/// <returns> The hash code for the value. </returns>
	public static int GetStableHashCode(this string value)
	{
		unchecked
		{
			var hash1 = 5381;
			var hash2 = hash1;

			for (var i = 0; (i < value.Length) && (value[i] != '\0'); i += 2)
			{
				hash1 = ((hash1 << 5) + hash1) ^ value[i];
				if ((i == (value.Length - 1)) || (value[i + 1] == '\0'))
				{
					break;
				}
				hash2 = ((hash2 << 5) + hash2) ^ value[i + 1];
			}

			return hash1 + (hash2 * 1566083941);
		}
	}

	/// <summary>
	/// Gets the index of the first occurrence of any character in the specified array in reverse of provided start index.
	/// </summary>
	/// <param name="value"> The value to process. </param>
	/// <param name="anyOf"> Characters to search for </param>
	/// <param name="startIndex"> Start index of the area to search. </param>
	/// <returns> The first index where any character was found; or -1 if no occurrence was found. </returns>
	public static int IndexOfAnyReverse(this string value, char[] anyOf, int startIndex)
	{
		if (!value.ValidRange(startIndex, 0))
		{
			return -1;
		}

		if (anyOf.Contains(value[startIndex]))
		{
			return startIndex;
		}

		for (var index = startIndex - 1; index >= 0; index--)
		{
			if (anyOf.Contains(value[index]))
			{
				return index;
			}
		}

		return -1;
	}

	/// <summary>
	/// Determines if the string is a JSON string.
	/// </summary>
	/// <param name="input"> The value to validate. </param>
	/// <returns> True if the input is JSON or false if otherwise. </returns>
	public static bool IsJson(this string input)
	{
		input = input.Trim();

		var isWellFormed = new Func<bool>(() =>
		{
			try
			{
				JsonSerializer.Parse(input);
			}
			catch
			{
				return false;
			}

			return true;
		});

		return ((input.StartsWith("{") && input.EndsWith("}"))
				|| (input.StartsWith("[") && input.EndsWith("]"))
				|| (input.StartsWith("\"") && input.EndsWith("\""))
			) && isWellFormed();
	}

	/// <summary>
	/// Determines if the string is a query string
	/// </summary>
	/// <param name="input"> The value to validate. </param>
	/// <returns> True if the input is a query string or false if otherwise. </returns>
	public static bool IsQueryString(this string input)
	{
		return input is { Length: >= 1 }
			&& (input[0] == '?');
	}

	/// <summary>
	/// Determines if the string is a valid email address.
	/// </summary>
	/// <param name="emailAddress"> The string to validate. </param>
	/// <returns> Returns true if the email address is valid. False if otherwise. </returns>
	public static bool IsValidEmailAddress(this string emailAddress)
	{
		try
		{
			// ReSharper disable once ObjectCreationAsStatement
			new MailAddress(emailAddress);
			return true;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Replaces a specific character with a new value.
	/// </summary>
	/// <param name="input"> The input to process. </param>
	/// <param name="index"> The index to update. </param>
	/// <param name="value"> The value to replace with. </param>
	/// <returns> The updated string. </returns>
	public static string Replace(this string input, int index, char value)
	{
		var response = input.ToCharArray();
		response[index] = value;
		return new string(response);
	}

	/// <summary>
	/// Splits the values into an array using the delimiter
	/// </summary>
	/// <param name="value"> The roles for the account. </param>
	/// <param name="delimiter"> The delimiter to split on </param>
	/// <returns> The array of values. </returns>
	public static string[] SplitIntoArray(this string value, string delimiter = ",")
	{
		return value?.Split([delimiter], StringSplitOptions.RemoveEmptyEntries) ?? [];
	}

	/// <summary>
	/// Convert string to a base 64 string.
	/// </summary>
	/// <param name="data"> The data to be converted. </param>
	/// <returns> The base 64 encoded string. </returns>
	public static string ToBase64String(this string data)
	{
		return data == null ? string.Empty : Encoding.UTF8.GetBytes(data).ToBase64String();
	}

	/// <summary>
	/// Converts a string to hex string value. Ex. "A" -> "41"
	/// </summary>
	/// <param name="value"> The string value to convert. </param>
	/// <param name="delimiter"> An optional delimited to put between bytes of the data. </param>
	/// <param name="prefix"> An optional prefix to put before each byte of the data. </param>
	/// <returns> The string in a hex string format. </returns>
	public static string ToHexString(this string value, string delimiter = null, string prefix = null)
	{
		var bytes = Encoding.Default.GetBytes(value);
		var hexString = bytes.ToHexString(null, null, delimiter, prefix);
		return hexString;
	}

	/// <summary>
	/// Converts a byte array to a hex string format. Ex. [41],[42] = "4142"
	/// </summary>
	/// <param name="data"> The byte array to convert. </param>
	/// <param name="startIndex"> The starting position within value. </param>
	/// <param name="length"> The number of array elements in value to convert. </param>
	/// <param name="delimiter"> An optional delimited to put between bytes of the data. </param>
	/// <param name="prefix"> An optional prefix to put before each byte of the data. </param>
	/// <returns> The byte array in a hex string format. </returns>
	public static string ToHexString(this byte[] data, int? startIndex = null, int? length = null, string delimiter = null, string prefix = null)
	{
		var hexString = BitConverter.ToString(data, startIndex ?? 0, length ?? data.Length);
		hexString = (prefix ?? "") + hexString.Replace("-", (delimiter ?? "") + (prefix ?? ""));
		return hexString;
	}

	/// <summary>
	/// Convert a string into a secure string.
	/// </summary>
	/// <param name="input"> The string. </param>
	/// <param name="makeReadOnly"> Option to make the SecureString read only. </param>
	/// <returns> The secure string. </returns>
	public static SecureString ToSecureString(this string input, bool makeReadOnly = false)
	{
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

	/// <summary>
	/// Check index and length to ensure it is within bounds of the string.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool ValidRange(this string value, int index, int length)
	{
		if (value.Length <= 0)
		{
			return false;
		}

		if ((index < 0) || (index >= value.Length))
		{
			return false;
		}

		var end = index + length;
		if ((end < 0) || (end > value.Length))
		{
			return false;
		}

		return true;
	}

	private static bool TryProcessCharacter(char c, StringBuilder builder)
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