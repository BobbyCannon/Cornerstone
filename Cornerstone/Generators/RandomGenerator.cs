﻿#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Generators;

/// <summary>
/// Random generator can generate many different types of random data.
/// </summary>
public static class RandomGenerator
{
	#region Constants

	/// <summary>
	/// All characters including Alphabet, Numbers, Symbols, NonPrintable
	/// </summary>
	public const string AllCharacters = AlphabetAndNumbers + Symbols + NonPrintable;

	/// <summary>
	/// The full alphabet with lower and upper cased versions.
	/// </summary>
	public const string Alphabet = AlphabetLowerOnly + AlphabetUpperOnly;

	/// <summary>
	/// The full alphabet with lower / upper cased versions and numbers.
	/// </summary>
	public const string AlphabetAndNumbers = Alphabet + Numbers;

	/// <summary>
	/// The full alphabet with only lower cased versions.
	/// </summary>
	public const string AlphabetLowerOnly = "abcdefghijklmnopqrstuvwxyz";

	/// <summary>
	/// The full alphabet with lower / upper cased versions, numbers, and symbols
	/// </summary>
	public const string AlphabetNumbersAndSymbols = Alphabet + Numbers + Symbols;

	/// <summary>
	/// The full alphabet with only upper cased versions.
	/// </summary>
	public const string AlphabetUpperOnly = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

	/// <summary>
	/// The full alphabet with only upper cased versions and numbers.
	/// </summary>
	public const string AlphabetUpperOnlyAndNumbers = AlphabetUpperOnly + Numbers;

	/// <summary>
	/// Full table of all 255 ascii characters.
	/// </summary>
	public const string FullTable = "\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09\x0A\x0B\x0C\x0D\x0E\x0F\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19\x1A\x1B\x1C\x1D\x1E\x1F\x20\x21\x22\x23\x24\x25\x26\x27\x28\x29\x2A\x2B\x2C\x2D\x2E\x2F\x30\x31\x32\x33\x34\x35\x36\x37\x38\x39\x3A\x3B\x3C\x3D\x3E\x3F\x40\x41\x42\x43\x44\x45\x46\x47\x48\x49\x4A\x4B\x4C\x4D\x4E\x4F\x50\x51\x52\x53\x54\x55\x56\x57\x58\x59\x5A\x5B\x5C\x5D\x5E\x5F\x60\x61\x62\x63\x64\x65\x66\x67\x68\x69\x6A\x6B\x6C\x6D\x6E\x6F\x70\x71\x72\x73\x74\x75\x76\x77\x78\x79\x7A\x7B\x7C\x7D\x7E\x7F\x80\x81\x82\x83\x84\x85\x86\x87\x88\x89\x8A\x8B\x8C\x8D\x8E\x8F\x90\x91\x92\x93\x94\x95\x96\x97\x98\x99\x9A\x9B\x9C\x9D\x9E\x9F\xA0\xA1\xA2\xA3\xA4\xA5\xA6\xA7\xA8\xA9\xAA\xAB\xAC\xAD\xAE\xAF\xB0\xB1\xB2\xB3\xB4\xB5\xB6\xB7\xB8\xB9\xBA\xBB\xBC\xBD\xBE\xBF\xC0\xC1\xC2\xC3\xC4\xC5\xC6\xC7\xC8\xC9\xCA\xCB\xCC\xCD\xCE\xCF\xD0\xD1\xD2\xD3\xD4\xD5\xD6\xD7\xD8\xD9\xDA\xDB\xDC\xDD\xDE\xDF\xE0\xE1\xE2\xE3\xE4\xE5\xE6\xE7\xE8\xE9\xEA\xEB\xEC\xED\xEE\xEF\xF0\xF1\xF2\xF3\xF4\xF5\xF6\xF7\xF8\xF9\xFA\xFB\xFC\xFD\xFE\xFF";

	/// <summary>
	/// A subset of non printable characters. Not an exhaustive list.
	/// </summary>
	public const string NonPrintable = "\r\n\x1B\a\f\t\v";

	/// <summary>
	/// All numbers 0-9.
	/// </summary>
	public const string Numbers = "0123456789";

	/// <summary>
	/// A subset of symbols. Not an exhaustive list.
	/// </summary>
	public const string Symbols = "!\"#$%&\'()*+,-./:;<=>?@\\]^_`~";

	#endregion

	#region Fields

	/// <summary>
	/// The full array of characters for <see cref="Alphabet" />
	/// </summary>
	public static readonly char[] AlphabetCharacters = Alphabet.ToArray();

	/// <summary>
	/// The full array of characters for <see cref="AlphabetAndNumbers" />
	/// </summary>
	public static readonly char[] AlphabetAndNumbersCharacters = AlphabetAndNumbers.ToArray();

	/// <summary>
	/// A list of lorem ipsum words.
	/// </summary>
	public static readonly string[] LoremIpsumWords =
	[
		"lorem", "ipsum", "dolor", "sit", "amet", "consectetuer", "adipiscing", "elit", "sed", "do", "eiusmod", "tempor", "incididunt", "ut", "labore",
		"et", "dolore", "magna", "aliqua", "enim", "ad", "minim", "veniam,", "quis", "nostrud", "exercitation", "ullamco", "laboris", "nisi",
		"aliquip", "ex", "ea", "commodo", "consequat", "duis", "aute", "irure", "in", "reprehenderit", "voluptate", "velit", "esse", "cillum",
		"eu", "fugiat", "nulla", "pariatur", "excepteur", "sint", "occaecat", "cupidatat", "non", "proident", "sunt", "culpa", "qui", "officia", "deserunt",
		"mollit", "anim", "id", "est", "laborum"
	];

	private static readonly object _syncLockForRandom;

	#endregion

	#region Constructors

	static RandomGenerator()
	{
		#if NETSTANDARD
		_oldRandom = new Random();
		_rng = RandomNumberGenerator.Create();
		#endif

		_syncLockForRandom = new object();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Returns a byte array with random data values.
	/// </summary>
	/// <param name="numberOfBytes"> The number of bytes to generate. </param>
	/// <returns> The generated random byte array data. </returns>
	public static byte[] GetBytes(int numberOfBytes)
	{
		var response = new byte[numberOfBytes];

		for (var i = 0; i < response.Length; i++)
		{
			response[i] = (byte) NextInteger(0, 256);
		}

		return response;
	}

	/// <summary>
	/// Gets a random item from a list.
	/// </summary>
	/// <typeparam name="T"> The type of the item in the collection. </typeparam>
	/// <param name="items"> The list of items. </param>
	/// <returns> A random item or the default value if the list is empty. </returns>
	public static T GetItem<T>(IEnumerable<T> items)
	{
		return GetItem(items.ToList());
	}

	/// <summary>
	/// Gets a random item from a list.
	/// </summary>
	/// <typeparam name="T"> The type of the item in the collection. </typeparam>
	/// <param name="items"> The list of items. </param>
	/// <returns> A random item or the default value if the list is empty. </returns>
	public static T GetItem<T>(IList<T> items)
	{
		return items.Count <= 0 ? default : items[NextInteger(0, items.Count)];
	}

	/// <summary>
	/// Get a random password.
	/// </summary>
	/// <param name="length"> The length to get. Defaults to 16. </param>
	/// <param name="excludeSymbols"> Optional exclude symbols. Defaults to true. </param>
	/// <returns> </returns>
	public static SecureString GetPassword(int length = 16, bool excludeSymbols = true)
	{
		var response = new SecureString();
		SetPassword(response, length, excludeSymbols);
		return response;
	}

	/// <summary>
	/// Gets a randomly generated phone.
	/// </summary>
	/// <param name="formatted"> If true then format the number as "(123) 456-7890". </param>
	/// <returns> The phone number. </returns>
	public static string GetPhoneNumber(bool formatted = false)
	{
		//
		// todo: add format like "(aaa) 
		// ex - +1 123 456-7890
		// cc - country code    +1
		// aaa - area code      123
		// ppp - prefix         456
		// lll - line number    7890
		//
		// ex - 011 39 23-456-7890
		// eee - exit code      011 (us / canada)
		// cc  - country code   39 (italy)
		// aaa - area code      123
		// ppp - prefix         456
		// lll - line number    7890
		//
		var areaCode = NextInteger(000, 999);
		var start = NextInteger(000, 999);
		var end = NextInteger(0000, 9999);

		return formatted
			? $"({areaCode}) {start}-{end}"
			: $"{areaCode}{start}{end}";
	}

	/// <summary>
	/// Create a random string containing the "lorem ipsum" words. This is very useful during testing.
	/// </summary>
	/// <param name="minWords"> The minimumInclusive number of words per sentence. </param>
	/// <param name="maxWords"> The maximumExclusive number of words per sentence. </param>
	/// <param name="minSentences"> The minimumInclusive number of sentences per paragraph. </param>
	/// <param name="maxSentences"> The maximumExclusive number of sentences per paragraph. </param>
	/// <param name="numParagraphs"> The number of paragraphs to generate. </param>
	/// <param name="prefix"> An optional paragraph prefix. </param>
	/// <param name="suffix"> An optional paragraph suffix. </param>
	/// <returns> The generated lorem ipsum data. </returns>
	public static string LoremIpsum(int minWords = 1, int maxWords = 25, int minSentences = 1, int maxSentences = 10, int numParagraphs = 1, string prefix = "", string suffix = "\r\n")
	{
		// todo: add argument validation;

		var numSentences = NextInteger(minSentences, maxSentences);
		var result = new TextBuilder();

		for (var p = 0; p < numParagraphs; p++)
		{
			if (prefix.Length > 0)
			{
				result.Append(prefix);
			}

			var numWords = NextInteger(minWords, maxWords);

			for (var s = 0; s < numSentences; s++)
			{
				for (var w = 0; w < numWords; w++)
				{
					if (w > 0)
					{
						result.Append(" ");
					}

					result.Append(LoremIpsumWords[NextInteger(0, LoremIpsumWords.Length - 1)]);
				}

				result.Append(". ");
			}

			if (suffix.Length > 0)
			{
				result.Append(suffix);
			}
		}

		if (suffix.Length > 0)
		{
			result.Remove(result.Length - suffix.Length - 1, suffix.Length + 1);
		}

		return result.ToString();
	}

	/// <summary>
	/// Returns a random bool value.
	/// </summary>
	/// <returns> The random bool value. </returns>
	public static bool NextBool()
	{
		return (NextInteger(0, 10000) % 2) == 0;
	}

	/// <summary>
	/// Returns a random byte value.
	/// </summary>
	/// <returns> The random byte value. </returns>
	public static byte NextByte()
	{
		return (byte) NextInteger(byte.MinValue, byte.MaxValue);
	}

	/// <summary>
	/// Returns a random sbyte value.
	/// </summary>
	/// <returns> The random sbyte value. </returns>
	public static sbyte NextSignedByte()
	{
		return (sbyte) NextInteger(sbyte.MinValue, sbyte.MaxValue);
	}

	/// <summary>
	/// Returns a random short value.
	/// </summary>
	/// <returns> The random short value. </returns>
	public static short NextShort()
	{
		return (short) NextInteger(short.MinValue, short.MaxValue);
	}

	/// <summary>
	/// Returns a random ushort value.
	/// </summary>
	/// <returns> The random ushort value. </returns>
	public static ushort NextUnsignedShort()
	{
		return (ushort) NextInteger(ushort.MinValue, ushort.MaxValue);
	}

	/// <summary>
	/// Returns a random timespan that is within a specified range.
	/// </summary>
	/// <returns>
	/// A timespan greater than or equal to minValue and less than maxValue; that is, the range of return
	/// values includes minValue but not maxValue. If minValue equals maxValue, minValue is returned.
	/// </returns>
	public static TimeSpan NextTimeSpan()
	{
		return NextTimeSpan(TimeSpan.MinValue, TimeSpan.MaxValue);
	}

	/// <summary>
	/// Returns a random timespan that is within a specified range.
	/// </summary>
	/// <param name="minimumInclusive"> The inclusive lower bound of the random number returned. </param>
	/// <param name="maximumExclusive"> The exclusive maximumExclusive bound of the random number returned. </param>
	/// <returns>
	/// A timespan greater than or equal to minValue and less than maxValue; that is, the range of return
	/// values includes minValue but not maxValue. If minValue equals maxValue, minValue is returned.
	/// </returns>
	public static TimeSpan NextTimeSpan(TimeSpan minimumInclusive, TimeSpan maximumExclusive)
	{
		if (maximumExclusive <= minimumInclusive)
		{
			return minimumInclusive;
		}

		lock (_syncLockForRandom)
		{
			var ticks = NextLong(minimumInclusive.Ticks, maximumExclusive.Ticks);
			return new TimeSpan(ticks);
		}
	}

	/// <summary>
	/// Returns a random datetime that is within a specified range.
	/// </summary>
	/// <returns>
	/// A datetime greater than or equal to minValue and less than maxValue; that is, the range of return
	/// values includes minValue but not maxValue. If minValue equals maxValue, minValue is returned.
	/// </returns>
	public static DateTime NextDateTime()
	{
		return NextDateTime(DateTime.MinValue, DateTime.MaxValue);
	}

	/// <summary>
	/// Returns a random datetime that is within a specified range.
	/// </summary>
	/// <param name="minimumInclusive"> The inclusive lower bound of the random number returned. </param>
	/// <param name="maximumExclusive"> The exclusive maximumExclusive bound of the random number returned. </param>
	/// <returns>
	/// A datetime greater than or equal to minValue and less than maxValue; that is, the range of return
	/// values includes minValue but not maxValue. If minValue equals maxValue, minValue is returned.
	/// </returns>
	public static DateTime NextDateTime(DateTime minimumInclusive, DateTime maximumExclusive)
	{
		if (maximumExclusive <= minimumInclusive)
		{
			return minimumInclusive;
		}

		lock (_syncLockForRandom)
		{
			var ticks = NextLong(minimumInclusive.Ticks, maximumExclusive.Ticks);
			return new DateTime(ticks);
		}
	}

	/// <summary>
	/// Returns a random decimal number that is within a specified range.
	/// </summary>
	/// <param name="minimumInclusive"> The inclusive lower bound of the random number returned. </param>
	/// <param name="maximumExclusive"> The exclusive maximumExclusive bound of the random number returned. </param>
	/// <param name="scale"> The scale about to include in the next double. Defaults to 0. </param>
	/// <returns>
	/// A decimal number greater than or equal to minValue and less than maxValue; that is, the range of return
	/// values includes minValue but not maxValue. If minValue equals maxValue, minValue is returned.
	/// </returns>
	public static decimal NextDecimal(decimal minimumInclusive = 0, decimal maximumExclusive = decimal.MaxValue, byte scale = 0)
	{
		if ((maximumExclusive < minimumInclusive) || (minimumInclusive == maximumExclusive))
		{
			return minimumInclusive;
		}

		lock (_syncLockForRandom)
		{
			decimal response;
			var value = new decimal(NextInteger(), NextInteger(), NextInteger(), false, scale);

			if ((Math.Sign(minimumInclusive) == Math.Sign(maximumExclusive)) || (minimumInclusive == 0) || (maximumExclusive == 0))
			{
				var remainder = (maximumExclusive != decimal.MaxValue) || (minimumInclusive == maximumExclusive)
					? decimal.Remainder(value, (maximumExclusive - minimumInclusive) + 1)
					: decimal.Remainder(value, maximumExclusive - minimumInclusive);
				response = remainder + minimumInclusive;
			}
			else
			{
				var getFromNegativeRange = ((double) minimumInclusive + (InternalNextDouble() * ((double) maximumExclusive - (double) minimumInclusive))) < 0;
				response = getFromNegativeRange ? decimal.Remainder(value, -minimumInclusive + 1) + minimumInclusive : decimal.Remainder(value, maximumExclusive + 1);
			}

			if (response < minimumInclusive)
			{
				response = minimumInclusive;
			}
			else if (response > maximumExclusive)
			{
				response = maximumExclusive;
			}

			return response;
		}
	}

	/// <summary>
	/// Returns a random double floating point that is within a specified range.
	/// </summary>
	/// <param name="minimumInclusive"> The inclusive lower bound of the random number returned. </param>
	/// <param name="maximumExclusive"> The exclusive maximumExclusive bound of the random number returned. </param>
	/// <param name="scale"> The scale of the double. How precise? 1 = 0.1, 2 = 0.01. Defaults to 3 (0.000). </param>
	/// <returns>
	/// A double floating point number greater than or equal to minValue and less than maxValue; that is, the
	/// range of return values includes minValue but not maxValue. If minValue equals maxValue, minValue is returned.
	/// </returns>
	public static double NextDouble(double minimumInclusive = double.MinValue, double maximumExclusive = double.MaxValue, byte scale = 3)
	{
		if (maximumExclusive <= minimumInclusive)
		{
			return minimumInclusive;
		}

		lock (_syncLockForRandom)
		{
			var nextDouble = InternalNextDouble();
			if ((minimumInclusive.CompareTo(double.MinValue) == 0)
				&& (maximumExclusive.CompareTo(double.MaxValue) == 0))
			{
				return Math.Round(nextDouble, scale);
			}
			return Math.Round((nextDouble * (maximumExclusive - minimumInclusive)) + minimumInclusive, scale);
		}
	}

	/// <summary>
	/// Returns a random integer that is within a specified range.
	/// </summary>
	/// <param name="minimumInclusive"> The inclusive lower bound of the random number returned. </param>
	/// <param name="maximumExclusive"> The exclusive upper bound of the random number returned. </param>
	/// <returns>
	/// A 32-bit signed integer greater than or equal to minimumInclusive and less than maximumExclusive; that is, the range
	/// of return values includes minimumInclusive but not maximumExclusive. If minimumInclusive equals maximumExclusive, minimumInclusive is returned.
	/// </returns>
	public static int NextInteger(int minimumInclusive = int.MinValue, int maximumExclusive = int.MaxValue)
	{
		if (maximumExclusive <= minimumInclusive)
		{
			return minimumInclusive;
		}

		lock (_syncLockForRandom)
		{
			#if NETSTANDARD
			return _oldRandom.Next(minimumInclusive, maximumExclusive);
			#else
			return RandomNumberGenerator.GetInt32(minimumInclusive, maximumExclusive);
			#endif
		}
	}

	/// <summary>
	/// Returns a random 64-bit integer that is within a specified range.
	/// </summary>
	/// <param name="minimumInclusive"> The inclusive lower bound of the random number returned. </param>
	/// <param name="maximumExclusive"> The exclusive upper bound of the random number returned. </param>
	/// <returns>
	/// A 64-bit signed integer greater than or equal to minimumInclusive and less than maximumExclusive; that is, the range
	/// of return values includes minimumInclusive but not maximumExclusive. If minimumInclusive equals maximumExclusive, minimumInclusive is returned.
	/// </returns>
	public static long NextLong(long minimumInclusive = long.MinValue, long maximumExclusive = long.MaxValue)
	{
		if (maximumExclusive <= minimumInclusive)
		{
			return minimumInclusive;
		}

		lock (_syncLockForRandom)
		{
			#if NETSTANDARD
			var data = new byte[8];
			_rng.GetBytes(data);
			#else
			var data = new byte[8];
			var span = new Span<byte>(data);
			RandomNumberGenerator.Fill(span);
			#endif
			var response = BitConverter.ToInt64(data, 0);
			var range = maximumExclusive - minimumInclusive;
			return range == -1 ? response : Math.Abs(response % range) + minimumInclusive;
		}
	}

	/// <summary>
	/// Generate a random string value.
	/// </summary>
	/// <param name="length"> The length of the string to create. </param>
	/// <param name="allowedChars"> The allowed characters. Defaults to <see cref="AlphabetAndNumbers" />. </param>
	/// <returns> </returns>
	/// <exception cref="ArgumentOutOfRangeException"> </exception>
	public static string NextString(int length, string allowedChars = AlphabetAndNumbers)
	{
		if (length <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero.");
		}

		if (string.IsNullOrEmpty(allowedChars))
		{
			throw new ArgumentOutOfRangeException(nameof(allowedChars), "You must provide some characters.");
		}

		var result = new StringBuilder(length, length);
		var uppercaseCount = 0;

		while (result.Length < length)
		{
			var c = allowedChars[NextInteger(0, allowedChars.Length - 1)];
			if (char.IsUpper(c))
			{
				uppercaseCount++;

				if (uppercaseCount >= 2)
				{
					continue;
				}
			}
			else
			{
				uppercaseCount = 0;
			}

			result.Append(c);
		}

		return result.ToString();
	}

	/// <summary>
	/// Returns a random unsigned integer that is within a specified range.
	/// </summary>
	/// <param name="minimumInclusive"> The inclusive lower bound of the random number returned. </param>
	/// <param name="maximumExclusive"> The exclusive upper bound of the random number returned. </param>
	/// <returns>
	/// A 32-bit unsigned integer greater than or equal to minimumInclusive and less than maximumExclusive; that is, the range
	/// of return values includes minimumInclusive but not maximumExclusive. If minimumInclusive equals maximumExclusive, minimumInclusive is returned.
	/// </returns>
	public static uint NextUnsignedInteger(uint minimumInclusive = uint.MinValue, uint maximumExclusive = uint.MaxValue)
	{
		if (maximumExclusive <= minimumInclusive)
		{
			return minimumInclusive;
		}

		lock (_syncLockForRandom)
		{
			return (uint) NextUnsignedLong(minimumInclusive, maximumExclusive);
		}
	}

	/// <summary>
	/// Returns a random unsigned 64-bit integer that is within a specified range.
	/// </summary>
	/// <param name="minimumInclusive"> The inclusive lower bound of the random number returned. </param>
	/// <param name="maximumExclusive"> The exclusive upper bound of the random number returned. </param>
	/// <returns>
	/// A 64-bit signed integer greater than or equal to minimumInclusive and less than maximumExclusive; that is, the range
	/// of return values includes minimumInclusive but not maximumExclusive. If minimumInclusive equals maximumExclusive, minimumInclusive is returned.
	/// </returns>
	public static ulong NextUnsignedLong(ulong minimumInclusive = ulong.MinValue, ulong maximumExclusive = ulong.MaxValue)
	{
		if (maximumExclusive <= minimumInclusive)
		{
			return minimumInclusive;
		}

		lock (_syncLockForRandom)
		{
			#if NETSTANDARD
			var data = new byte[8];
			_rng.GetBytes(data);
			#else
			var data = new byte[8];
			var span = new Span<byte>(data);
			RandomNumberGenerator.Fill(span);
			#endif
			var response = (decimal) BitConverter.ToUInt64(data, 0);
			return (ulong) Math.Abs(response % (maximumExclusive - minimumInclusive)) + minimumInclusive;
		}
	}

	/// <summary>
	/// Populate an array of char.
	/// </summary>
	/// <param name="data"> The array to populate. </param>
	public static void Populate(ref char[] data)
	{
		for (var i = 0; i < data.Length; i++)
		{
			data[i] = GetItem(AlphabetAndNumbersCharacters);
		}
	}

	/// <summary>
	/// Populate an array of char.
	/// </summary>
	/// <param name="data"> The array to populate. </param>
	/// <param name="source"> The source of items to choose from. </param>
	public static void PopulateUnique<T>(ref T[] data, T[] source)
	{
		var sourceList = source.Distinct().ToList();
		if (sourceList.Count < data.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(source), "The source doesn't contain enough unique items.");
		}

		for (var i = 0; i < data.Length; i++)
		{
			data[i] = GetItem(sourceList);
			sourceList.Remove(data[i]);
		}
	}

	/// <summary>
	/// Set a random password to the provided builder.
	/// </summary>
	/// <param name="secureString"> The builder to be updated. </param>
	/// <param name="length"> The length to get. Defaults to 16. </param>
	/// <param name="excludeSymbols"> Optional exclude symbols. Defaults to true. </param>
	/// <returns> </returns>
	public static void SetPassword(SecureString secureString, int length = 16, bool excludeSymbols = true)
	{
		var nextCharacter = char.MinValue;
		var lastCharacter = nextCharacter;
		var buffer = new char[length];
		var characters = excludeSymbols ? AlphabetAndNumbers : AlphabetNumbersAndSymbols;
		var uppercaseCount = 0;
		var lowercaseCount = 0;
		var count = 0;

		secureString.Clear();

		try
		{
			while (count < length)
			{
				nextCharacter = characters[NextInteger(0, characters.Length - 1)];
				while (nextCharacter == lastCharacter)
				{
					nextCharacter = characters[NextInteger(0, characters.Length - 1)];
				}

				var duplicateIndex = Array.IndexOf(buffer, nextCharacter);
				while ((duplicateIndex != -1) && ((count - duplicateIndex) < 2))
				{
					nextCharacter = characters[NextInteger(0, characters.Length - 1)];
					duplicateIndex = Array.IndexOf(buffer, nextCharacter);
				}

				if (char.IsUpper(nextCharacter))
				{
					lowercaseCount = 0;
					uppercaseCount++;
				}
				else if (char.IsLower(nextCharacter))
				{
					lowercaseCount++;
					uppercaseCount = 0;
				}
				else
				{
					lowercaseCount = 0;
					uppercaseCount = 0;
				}

				if ((lowercaseCount > 2) || (uppercaseCount > 2))
				{
					continue;
				}

				buffer[count++] = nextCharacter;
				secureString.AppendChar(nextCharacter);
				lastCharacter = nextCharacter;
			}
		}
		finally
		{
			Array.Clear(buffer, 0, buffer.Length);
		}
	}

	/// <summary>
	/// Shuffle a list of items into a random order.
	/// </summary>
	/// <typeparam name="T"> The type of the items in the list. </typeparam>
	/// <param name="list"> The list of items. </param>
	/// <returns> The shuffled list. </returns>
	public static IList<T> Shuffle<T>(this IList<T> list)
	{
		var n = list.Count;

		while (n > 1)
		{
			var k = NextInteger(0, n--);
			(list[k], list[n]) = (list[n], list[k]);
		}

		var f = NextInteger(1, n);
		(list[f], list[0]) = (list[f], list[0]);

		return list;
	}

	private static double InternalNextDouble()
	{
		#if NETSTANDARD
		var data = new byte[sizeof(ulong)];
		_rng.GetBytes(data);
		#else
		var data = new byte[sizeof(ulong)];
		var bytes = new Span<byte>(data);
		RandomNumberGenerator.Fill(bytes);
		#endif

		var nextULong = BitConverter.ToUInt64(data, 0);
		var response = (nextULong >> 11) * (1.0 / (1ul << 53));
		return response;
	}

	#endregion

	#if NETSTANDARD
	private static readonly Random _oldRandom;
	private static readonly RandomNumberGenerator _rng;
	#endif
}