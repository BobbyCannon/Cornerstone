#region References

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

#endregion

namespace Cornerstone.Protocols.Osc;

internal static class Extensions
{
	#region Methods

	public static int FirstIndexAfter<T>(this T[] items, int start, Func<T, bool> predicate)
	{
		if (items == null)
		{
			throw new ArgumentNullException(nameof(items));
		}

		if (predicate == null)
		{
			throw new ArgumentNullException(nameof(predicate));
		}

		if (start >= items.Count())
		{
			throw new ArgumentOutOfRangeException(nameof(start));
		}

		var retVal = 0;
		foreach (var item in items)
		{
			if ((retVal >= start) && predicate(item))
			{
				return retVal;
			}
			retVal++;
		}
		return -1;
	}

	/// <summary>
	/// Parse a single argument
	/// </summary>
	/// <param name="str"> string contain the argument to parse </param>
	/// <param name="provider"> format provider to use </param>
	/// <returns> the parsed argument </returns>
	public static object ParseArgument(this string str, IFormatProvider provider)
	{
		var argString = str.Trim();

		if (argString.Length == 0)
		{
			throw new Exception("Argument is empty");
		}

		// try to parse a hex value
		if ((argString.Length > 2) && argString.StartsWith("0x"))
		{
			var hexString = argString.Substring(2);

			if ((hexString.Length <= 8) && int.TryParse(hexString, NumberStyles.HexNumber, provider, out var iValue))
			{
				return iValue;
			}

			if ((hexString.Length <= 9) && (hexString[hexString.Length - 1] == 'u') && uint.TryParse(hexString.Substring(0, hexString.Length - 1), NumberStyles.HexNumber, provider, out var uiValue))
			{
				return uiValue;
			}

			if ((hexString.Length <= 16) && long.TryParse(hexString, NumberStyles.HexNumber, provider, out var lValue))
			{
				return lValue;
			}

			if ((hexString.Length <= 17) && (hexString[hexString.Length - 1] == 'L') && long.TryParse(hexString.Substring(0, hexString.Length - 1), NumberStyles.HexNumber, provider, out var lValue2))
			{
				return lValue2;
			}

			if (ulong.TryParse(hexString.Substring(0, hexString.Length - 1), NumberStyles.HexNumber, provider, out var value))
			{
				return value;
			}

			return -1;
		}

		switch (argString[argString.Length - 1])
		{
			case 'u':
			{
				if (uint.TryParse(argString.Substring(0, argString.Length - 1), NumberStyles.Integer, provider, out var u32))
				{
					return u32;
				}
				break;
			}
			case 'U':
			{
				if (ulong.TryParse(argString.Substring(0, argString.Length - 1), NumberStyles.Integer, provider, out var u64))
				{
					return u64;
				}
				break;
			}
			case 'L':
			{
				if (long.TryParse(argString.Substring(0, argString.Length - 1), NumberStyles.Integer, provider, out var value64))
				{
					return value64;
				}
				break;
			}
			case 'f':
			{
				var argument = argString.Substring(0, argString.Length - 1);
				if (float.TryParse(argument, NumberStyles.Float, provider, out var valueFloat))
				{
					return valueFloat;
				}
				break;
			}
			case 'd':
			{
				var argument = argString.Substring(0, argString.Length - 1);
				if (double.TryParse(argument, NumberStyles.Float, provider, out var valueDouble))
				{
					return valueDouble;
				}

				if (double.TryParse(argument, out valueDouble))
				{
					return valueDouble;
				}
				break;
			}
			case 'm':
			{
				var argument = argString.Substring(0, argString.Length - 1);
				if (decimal.TryParse(argument, out var value))
				{
					return value;
				}
				break;
			}
			default:
			{
				if (int.TryParse(argString, NumberStyles.Integer, provider, out var value32))
				{
					return value32;
				}

				if (long.TryParse(argString, NumberStyles.Integer, provider, out var value64))
				{
					return value64;
				}
				break;
			}
		}

		if (argString.Equals(float.PositiveInfinity.ToString(provider)))
		{
			return float.PositiveInfinity;
		}

		if (argString.Equals(float.NegativeInfinity.ToString(provider)))
		{
			return float.NegativeInfinity;
		}

		if (argString.Equals(float.NaN.ToString(provider)))
		{
			return float.NaN;
		}

		// parse bool
		if (bool.TryParse(argString, out var valueBool))
		{
			return valueBool;
		}

		// parse char
		if ((argString.Length == 3) && (argString[0] == '\'') && (argString[2] == '\''))
		{
			var c = str.Trim()[1];
			return c;
		}

		// parse null
		if (argString.Equals("null", StringComparison.OrdinalIgnoreCase) || argString.Equals("nil", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		// parse string
		if (argString[0] == '\"')
		{
			var end = argString.LastIndexOf('"');

			if (end < (argString.Length - 1))
			{
				// some kind of other value tacked on the end of a string! 
				throw new Exception($"Malformed string argument '{argString}'");
			}

			return argString.Substring(1, argString.Length - 2).Unescape();
		}

		// If all else fails just return on OscSymbol (AlternateString)
		return new OscSymbol(argString.Unescape());
	}

	public static byte[] ParseBlob(string str, IFormatProvider provider)
	{
		if (string.IsNullOrWhiteSpace(str))
		{
			return Array.Empty<byte>();
		}

		var trimmed = str.Trim();

		if (trimmed.StartsWith("64x"))
		{
			return System.Convert.FromBase64String(trimmed.Substring(3));
		}
		if (str.StartsWith("0x"))
		{
			trimmed = trimmed.Substring(2);

			if ((trimmed.Length % 2) != 0)
			{
				// this is an error
				throw new Exception("Invalid blob string length");
			}

			var length = trimmed.Length / 2;
			var bytes = new byte[length];

			for (var i = 0; i < bytes.Length; i++)
			{
				bytes[i] = byte.Parse(trimmed.Substring(i * 2, 2), NumberStyles.HexNumber, provider);
			}

			return bytes;
		}
		else
		{
			var parts = str.Split(',');
			var bytes = new byte[parts.Length];

			for (var i = 0; i < bytes.Length; i++)
			{
				bytes[i] = byte.Parse(parts[i], NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, provider);
			}

			return bytes;
		}
	}

	public static OscTimeTag ToOscTimeTag(this DateTime time)
	{
		return new OscTimeTag(time);
	}

	public static string ToStringBlob(this byte[] bytes)
	{
		// if the default is to be Base64 encoded
		// return "64x" + System.Convert.ToBase64String(bytes);

		var sb = new StringBuilder((bytes.Length * 2) + 2);

		sb.Append("0x");

		foreach (var b in bytes)
		{
			sb.Append(b.ToString("X2"));
		}

		return sb.ToString();
	}

	/// <summary>
	/// Turn a readable string into a byte array
	/// </summary>
	/// <param name="value"> a string, optionally with escape sequences in it </param>
	/// <returns> The unescape version of the provided value. </returns>
	public static string Unescape(this string value)
	{
		var count = 0;
		var isEscaped = false;
		var parseHexNext = false;
		var parseHexCount = 0;
		var parseHexCounts = new List<int>();

		// first we count the number of chars we will be returning
		for (var i = 0; i < value.Length; i++)
		{
			var c = value[i];

			if (parseHexNext)
			{
				if (Uri.IsHexDigit(c))
				{
					parseHexCount++;
				}
				else
				{
					parseHexCounts.Add(parseHexCount);
					count += parseHexCount;
					parseHexNext = false;
					parseHexCount = 0;
				}
			}

			// if we are not in  an escape sequence and the char is a escape char
			else if ((isEscaped == false) && (c == '\\'))
			{
				// escape
				isEscaped = true;

				// increment count
				count++;
			}

			// else if we are escaped
			else if (isEscaped)
			{
				// reset escape state
				isEscaped = false;

				// check the char against the set of known escape chars
				switch (char.ToLower(c))
				{
					case '0':
					case 'a':
					case 'b':
					case 'f':
					case 'n':
					case 'r':
					case 't':
					case 'v':
					case '\'':
					case '\\':
					case '"':
						// do not increment count
						break;

					case 'u':
						// Skip the 4 value because they are unicode values
						i += 4;
						break;

					case 'x':
						// do not increment count
						parseHexNext = true;
						parseHexCount = 0;
						break;

					default:
						// this is not a valid escape sequence
						throw new Exception($"Invalid escape sequence at char of [{c}] at offset {i - 1}.");
				}
			}
			else
			{
				// normal char increment count
				count++;
			}
		}

		if (parseHexNext)
		{
			if (parseHexCount <= 0)
			{
				throw new Exception($"Invalid escape sequence at char '{value.Length - 1}' missing hex value.");
			}

			parseHexCounts.Add(parseHexCount);
		}

		if (isEscaped)
		{
			throw new Exception($"Invalid escape sequence at char '{value.Length - 1}'.");
		}

		// create a character array for the result
		var chars = new char[count];
		var hexCountIndex = 0;
		var j = 0;

		// actually populate the array
		for (var i = 0; i < value.Length; i++)
		{
			var c = value[i];

			// if we are not in  an escape sequence and the char is a escape char
			if ((isEscaped == false) && (c == '\\'))
			{
				// escape
				isEscaped = true;
			}

			// else if we are escaped
			else if (isEscaped)
			{
				// reset escape state
				isEscaped = false;

				// check the char against the set of known escape chars
				switch (char.ToLower(value[i]))
				{
					case '0':
						chars[j++] = '\0';
						break;

					case 'a':
						chars[j++] = '\a';
						break;

					case 'b':
						chars[j++] = '\b';
						break;

					case 'f':
						chars[j++] = '\f';
						break;

					case 'n':
						chars[j++] = '\n';
						break;

					case 'r':
						chars[j++] = '\r';
						break;

					case 't':
						chars[j++] = '\t';
						break;

					case 'v':
						chars[j++] = '\v';
						break;

					case '\\':
						chars[j++] = '\\';
						break;

					case '\'':
						chars[j++] = '\'';
						break;

					case '"':
						chars[j++] = '"';
						break;

					case 'x':
						var hexCount = parseHexCounts[hexCountIndex++];
						for (var h = 0; h < hexCount; h += 2)
						{
							var hB = Uri.FromHex(value[++i]);
							var lB = Uri.FromHex(value[++i]);
							chars[j++] = (char) ((hB << 4) | lB);
						}
						break;

					case 'u':
						chars[j++] = (char) ((Uri.FromHex(value[++i]) << 12) | (Uri.FromHex(value[++i]) << 8) | (Uri.FromHex(value[++i]) << 4) | Uri.FromHex(value[++i]));
						break;
				}
			}
			else
			{
				// normal char
				chars[j++] = c;
			}
		}

		return new string(chars);
	}

	internal static int AlignedStringLength(this string val)
	{
		var len = val.Length + (4 - (val.Length % 4));
		if (len <= val.Length)
		{
			len += 4;
		}

		return len;
	}

	/// <summary>
	/// Parse arguments
	/// </summary>
	/// <param name="str"> string to parse </param>
	/// <param name="arguments"> the list to put the parsed arguments into </param>
	/// <param name="index"> the current index within the string </param>
	/// <param name="provider"> the format to use </param>
	/// <param name="parsers"> An optional set of OSC argument parsers. </param>
	internal static void ParseArguments(string str, List<object> arguments, int index, IFormatProvider provider, params OscArgumentParser[] parsers)
	{
		while (true)
		{
			if (index >= str.Length)
			{
				return;
			}

			// scan forward for the first control char ',', '[', '{', '"'
			var controlChar = str.IndexOfAny([',', '[', '{', '"'], index);

			if (controlChar == -1)
			{
				// no control char found 
				var argument = str.Substring(index, str.Length - index);
				arguments.Add(argument.ParseArgument(provider));
				return;
			}

			var c = str[controlChar];

			switch (c)
			{
				case ',':
				{
					if (index == controlChar)
					{
						index++;
						continue;
					}

					var argument = str.Substring(index, controlChar - index);
					arguments.Add(argument.ParseArgument(provider));
					index = controlChar + 1;
					break;
				}

				case '[':
				{
					var end = ScanForwardInArray(str, controlChar);
					var array = new List<object>();

					ParseArguments(str.Substring(controlChar + 1, end - (controlChar + 1)), array, 0, provider);

					arguments.Add(array.ToArray());

					end++;

					if (end >= str.Length)
					{
						return;
					}

					if (str[end] != ',')
					{
						controlChar = str.IndexOfAny([','], end);

						if (controlChar == -1)
						{
							return;
						}

						if (string.IsNullOrWhiteSpace(str.Substring(end, controlChar - end)) == false)
						{
							throw new Exception($"Malformed array '{str.Substring(index, controlChar - end)}'");
						}

						index = controlChar;
					}
					else
					{
						index = end + 1;
					}

					break;
				}

				case '{':
				{
					var end = ScanForwardObject(str, controlChar);

					arguments.Add(ParseObject(str.Substring(controlChar + 1, end - (controlChar + 1)), provider, parsers));

					end++;

					if (end >= str.Length)
					{
						return;
					}

					if (str[end] != ',')
					{
						controlChar = str.IndexOfAny([','], end);

						if (controlChar == -1)
						{
							return;
						}

						if (string.IsNullOrWhiteSpace(str.Substring(end, controlChar - end)) == false)
						{
							throw new Exception($"Malformed object '{str.Substring(index, controlChar - end)}'");
						}

						index = controlChar;
					}
					else
					{
						index = end + 1;
					}

					break;
				}

				case '"':
				{
					var start = controlChar + 1;
					var nextQuote = ScanForwardUntil(str, start, '"', '\\');
					var argument = str.Substring(start, nextQuote - start);
					arguments.Add(argument.Unescape());
					index = nextQuote + 1;
					break;
				}
			}
		}
	}

	/// <summary>
	/// Scan for object start and end control chars
	/// </summary>
	/// <param name="str"> the string to scan </param>
	/// <param name="controlChar"> the index of the starting control char </param>
	/// <returns> the index of the end char </returns>
	internal static int ScanForwardObject(string str, int controlChar)
	{
		return ScanForward(str, controlChar, '{', '}', "Expected '}'");
	}

	/// <summary>
	/// Parse an object value.
	/// </summary>
	/// <param name="str"> The string contain the object to parse. </param>
	/// <param name="provider"> The format provider to use. </param>
	/// <param name="parsers"> An optional set of OSC argument parsers. </param>
	/// <returns> The parsed argument or a string if unknown type. </returns>
	private static object ParseObject(string str, IFormatProvider provider, params OscArgumentParser[] parsers)
	{
		var strTrimmed = str.Trim();

		var colon = strTrimmed.IndexOf(':');

		if (colon <= 0)
		{
			throw new Exception($"Malformed object '{strTrimmed}', missing type name");
		}

		var name = strTrimmed.Substring(0, colon).Trim();

		if (name.Length == 0)
		{
			throw new Exception($"Malformed object '{strTrimmed}', missing type name");
		}

		if ((colon + 1) >= strTrimmed.Length)
		{
			throw new Exception($"Malformed object '{strTrimmed}'");
		}

		var value = strTrimmed.Substring(colon + 1).Trim();
		if (value.EndsWith("}"))
		{
			value = value.Substring(0, value.Length - 1).Trim();
		}

		switch (name)
		{
			case OscMidi.Name:
			case "midi":
			case "m":
			{
				return OscMidi.Parse(value, provider);
			}
			case OscTimeTag.Name:
			case "time":
			case "t":
			{
				if (DateTime.TryParse(value, null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var datetime))
				{
					if ((datetime < OscTimeTag.MinDateTime)
						|| (datetime > OscTimeTag.MaxDateTime))
					{
						// The date range is outside OscTimeTag range so return the datetime instead.
						return datetime;
					}

					return OscTimeTag.FromDateTime(datetime);
				}

				return OscTimeTag.Parse(value, provider);
			}
			case "TimeSpan":
			case "timespan":
			case "ts":
				return TimeSpan.Parse(value, provider);

			case OscRgba.Name:
			case "color":
			case "c":
			{
				return OscRgba.Parse(value, provider);
			}
			case "Blob":
			case "blob":
			case "b":
			case "Data":
			case "data":
			case "d":
			{
				return ParseBlob(value, provider);
			}
			default:
			{
				foreach (var parser in parsers)
				{
					if (!parser.CanParse(name))
					{
						continue;
					}

					return parser.Parse(value);
				}

				return value;
			}
		}
	}

	/// <summary>
	/// Scan for start and end control chars
	/// </summary>
	/// <param name="str"> the string to scan </param>
	/// <param name="controlChar"> the index of the starting control char </param>
	/// <param name="startChar"> start control char </param>
	/// <param name="endChar"> end control char </param>
	/// <param name="errorString"> string to use in the case of an error </param>
	/// <returns> the index of the end char </returns>
	private static int ScanForward(string str, int controlChar, char startChar, char endChar, string errorString)
	{
		var found = false;
		var count = 0;
		var index = controlChar + 1;
		var insideString = false;

		while (index < str.Length)
		{
			if (str[index] == '"')
			{
				insideString = !insideString;
			}
			else
			{
				if (insideString == false)
				{
					if (str[index] == startChar)
					{
						count++;
					}
					else if (str[index] == endChar)
					{
						if (count == 0)
						{
							found = true;

							break;
						}

						count--;
					}
				}
			}

			index++;
		}

		if (insideString)
		{
			throw new Exception(@"Expected '""'");
		}

		if (count > 0)
		{
			throw new Exception(errorString);
		}

		if (found == false)
		{
			throw new Exception(errorString);
		}

		return index;
	}

	/// <summary>
	/// Scan for array start and end control chars
	/// </summary>
	/// <param name="str"> the string to scan </param>
	/// <param name="controlChar"> the index of the starting control char </param>
	/// <returns> the index of the end char </returns>
	private static int ScanForwardInArray(string str, int controlChar)
	{
		return ScanForward(str, controlChar, '[', ']', "Expected ']'");
	}

	/// <summary>
	/// Scan for start and end control chars
	/// </summary>
	/// <param name="str"> the string to scan </param>
	/// <param name="index"> the index to start from </param>
	/// <param name="endChar"> end control char </param>
	/// <param name="escapeCharacter"> the escape character </param>
	/// <returns> the index of the end char </returns>
	private static int ScanForwardUntil(string str, int index, char endChar, char escapeCharacter)
	{
		var hasEscape = false;

		while (index < str.Length)
		{
			if ((str[index] == endChar) && !hasEscape)
			{
				return index;
			}

			hasEscape = !hasEscape && (str[index] == escapeCharacter);
			index++;
		}

		return -1;
	}

	#endregion
}