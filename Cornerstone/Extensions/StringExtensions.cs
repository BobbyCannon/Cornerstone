#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for the string type.
/// </summary>
public static class StringExtensions
{
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
	/// <returns> The unencoded byte array. </returns>
	public static byte[] FromBase64StringToByteArray(this string data)
	{
		const string Key = ";base64,";
		var index = data.IndexOf(Key);
		if (index >= 0)
		{
			data = data.Substring(index + Key.Length);
		}

		return Convert.FromBase64String(data);
	}

	/// <summary>
	/// Convert the text to a camel case string.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The string in the desired format. </returns>
	public static string ToCamelCase(this string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return value ?? "";
		}

		ReadOnlySpan<char> s = value;
		var buf = value.Length <= 72
			? stackalloc char[value.Length]
			: new char[value.Length];

		int i = 0, j = 0;
		var upperNext = false;

		// Skip leading non-letters
		while ((i < s.Length) && !char.IsLetter(s[i]))
		{
			i++;
		}

		if (i == s.Length)
		{
			return "";
		}

		// First real letter → lowercase
		buf[j++] = char.ToLowerInvariant(s[i++]);

		for (; i < s.Length; i++)
		{
			var c = s[i];

			if (char.IsLetterOrDigit(c))
			{
				buf[j++] = upperNext ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c);
				upperNext = false;
			}
			else
			{
				upperNext = true;
			}
		}

		return (j == value.Length) && !upperNext
			? value
			: buf[..j].ToString();
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