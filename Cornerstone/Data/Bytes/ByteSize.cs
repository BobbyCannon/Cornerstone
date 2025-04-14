#region References

using System;
using System.Globalization;
using System.Linq;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Text;
using static System.Globalization.NumberStyles;

#endregion

namespace Cornerstone.Data.Bytes;

/// <summary>
/// Represents a byte size value.
/// </summary>
public readonly partial struct ByteSize : IComparable<ByteSize>, IEquatable<ByteSize>, IComparable, IFormattable, IObjectCodeWriter
{
	#region Constants

	public const string Bit = "bit";
	public const long BitsInByte = 8;
	public const long BitsInGigabit = BitsInByte * BytesInGigabit;
	public const long BitsInGigabyte = BitsInByte * BytesInGigabyte;
	public const long BitsInKilobit = BitsInByte * BytesInKilobit;
	public const long BitsInKilobyte = BitsInByte * BytesInKilobyte;
	public const long BitsInMegabit = BitsInByte * BytesInMegabit;
	public const long BitsInMegabyte = BitsInByte * BytesInMegabyte;
	public const long BitsInTerabit = BitsInByte * BytesInTerabit;
	public const long BitsInTerabyte = BitsInByte * BytesInTerabyte;
	public const string BitSymbol = "b";
	public const string Byte = "byte";
	public const long BytesInGigabit = 125000000;
	public const long BytesInGigabyte = 1073741824;
	public const long BytesInKilobit = 125;
	public const long BytesInKilobyte = 1024;
	public const long BytesInMegabit = 125000;
	public const long BytesInMegabyte = 1048576;
	public const long BytesInTerabit = 125000000000;
	public const long BytesInTerabyte = 1099511627776;
	public const string ByteSymbol = "B";
	public const string Gigabit = "gigabit";
	public const string GigabitSymbol = "Gb";
	public const string Gigabyte = "gigabyte";
	public const string GigabyteSymbol = "GB";
	public const string Kilobit = "kilobit";
	public const string KilobitSymbol = "Kb";
	public const string Kilobyte = "kilobyte";
	public const string KilobyteSymbol = "KB";
	public const string Megabit = "megabit";
	public const string MegabitSymbol = "Mb";
	public const string Megabyte = "megabyte";
	public const string MegabyteSymbol = "MB";
	public const string Terabit = "terabit";
	public const string TerabitSymbol = "Tb";
	public const string Terabyte = "terabyte";
	public const string TerabyteSymbol = "TB";

	#endregion

	#region Fields

	public static readonly ByteSize MinValue = FromBits(long.MinValue);
	public static readonly ByteSize MaxValue = FromBits(ulong.MaxValue);

	#endregion

	#region Constructors

	public ByteSize(decimal byteSize)
	{
		// Get ceiling because bits are whole units
		Bits = Math.Ceiling(byteSize * BitsInByte);

		Bytes = byteSize;
		Kilobits = byteSize / BytesInKilobit;
		Kilobytes = byteSize / BytesInKilobyte;
		Megabits = byteSize / BytesInMegabit;
		Megabytes = byteSize / BytesInMegabyte;
		Gigabits = byteSize / BytesInGigabit;
		Gigabytes = byteSize / BytesInGigabyte;
		Terabits = byteSize / BytesInTerabit;
		Terabytes = byteSize / BytesInTerabyte;
	}

	private ByteSize(decimal? byteSize)
		: this(byteSize ?? throw new ArgumentNullException(nameof(byteSize)))
	{
	}

	#endregion

	#region Properties

	public decimal Bits { get; }

	public decimal Bytes { get; }

	public decimal Gigabits { get; }

	public decimal Gigabytes { get; }

	public decimal Kilobits { get; }

	public decimal Kilobytes { get; }

	public string LargestWholeNumberFullWord => GetLargestWholeNumberFullWord();

	public string LargestWholeNumberSymbol => GetLargestWholeNumberSymbol();

	public decimal LargestWholeNumberValue => GetLargestWholeNumberValue();

	public decimal Megabits { get; }

	public decimal Megabytes { get; }

	public decimal Terabits { get; }

	public decimal Terabytes { get; }

	#endregion

	#region Methods

	public ByteSize Add(ByteSize value)
	{
		return new ByteSize(Bytes + value.Bytes);
	}

	public ByteSize AddBits(long value)
	{
		return this + FromBits(value);
	}

	public ByteSize AddBytes(decimal value)
	{
		return this + FromBytes(value);
	}

	public ByteSize AddGigabytes(decimal value)
	{
		return this + FromGigabytes(value);
	}

	public ByteSize AddKilobytes(decimal value)
	{
		return this + FromKilobytes(value);
	}

	public ByteSize AddMegabytes(decimal value)
	{
		return this + FromMegabytes(value);
	}

	public ByteSize AddTerabytes(decimal value)
	{
		return this + FromTerabytes(value);
	}

	public int CompareTo(object obj)
	{
		if (obj == null)
		{
			return 1;
		}

		if (obj is not ByteSize size)
		{
			throw new ArgumentException("Object is not a ByteSize");
		}

		return CompareTo(size);
	}

	public int CompareTo(ByteSize other)
	{
		return Bits.CompareTo(other.Bits);
	}

	public override bool Equals(object value)
	{
		if (value == null)
		{
			return false;
		}

		ByteSize other;

		if (value is ByteSize size)
		{
			other = size;
		}
		else
		{
			return false;
		}

		return Equals(other);
	}

	public bool Equals(ByteSize value)
	{
		return Bits == value.Bits;
	}

	public static ByteSize From(decimal value, ByteUnit unit)
	{
		return unit switch
		{
			ByteUnit.Bit => FromBits(value),
			ByteUnit.Byte => FromBytes(value),
			ByteUnit.Kilobit => FromKilobits(value),
			ByteUnit.Kilobyte => FromKilobytes(value),
			ByteUnit.Megabit => FromMegabits(value),
			ByteUnit.Megabyte => FromMegabytes(value),
			ByteUnit.Gigabit => FromGigabits(value),
			ByteUnit.Gigabyte => FromGigabytes(value),
			ByteUnit.Terabit => FromTerabits(value),
			ByteUnit.Terabyte => FromTerabytes(value),
			_ => throw new NotImplementedException()
		};
	}

	public override int GetHashCode()
	{
		return Bits.GetHashCode();
	}

	public ByteUnit GetLargestByteUnit()
	{
		if (Terabytes >= 1)
		{
			return ByteUnit.Terabyte;
		}

		if (Gigabytes >= 1)
		{
			return ByteUnit.Gigabyte;
		}

		if (Megabytes >= 1)
		{
			return ByteUnit.Megabyte;
		}

		if (Kilobytes >= 1)
		{
			return ByteUnit.Kilobyte;
		}

		if (Bytes >= 1)
		{
			return ByteUnit.Byte;
		}

		return ByteUnit.Bit;
	}

	public readonly string GetLargestWholeNumberFullWord()
	{
		if (Terabytes >= 1)
		{
			return ByteUnit.Terabyte.GetHumanizeStringFormat(Terabytes, WordFormat.Full);
		}

		if (Gigabytes >= 1)
		{
			return ByteUnit.Gigabyte.GetHumanizeStringFormat(Gigabytes, WordFormat.Full);
		}

		if (Megabytes >= 1)
		{
			return ByteUnit.Megabyte.GetHumanizeStringFormat(Megabytes, WordFormat.Full);
		}

		if (Kilobytes >= 1)
		{
			return ByteUnit.Kilobyte.GetHumanizeStringFormat(Kilobytes, WordFormat.Full);
		}

		if (Bytes >= 1)
		{
			return ByteUnit.Byte.GetHumanizeStringFormat(Bytes, WordFormat.Full);
		}

		return ByteUnit.Bit.GetHumanizeStringFormat(Bits, WordFormat.Full);
	}

	public string GetLargestWholeNumberSymbol()
	{
		if (Terabytes >= 1)
		{
			return ByteUnit.Terabyte.GetHumanizeStringFormat(Terabytes);
		}

		if (Gigabytes >= 1)
		{
			return ByteUnit.Gigabyte.GetHumanizeStringFormat(Gigabytes);
		}

		if (Megabytes >= 1)
		{
			return ByteUnit.Megabyte.GetHumanizeStringFormat(Megabytes);
		}

		if (Kilobytes >= 1)
		{
			return ByteUnit.Kilobyte.GetHumanizeStringFormat(Kilobytes);
		}

		if (Bytes >= 1)
		{
			return ByteUnit.Byte.GetHumanizeStringFormat(Bytes);
		}

		return ByteUnit.Bit.GetHumanizeStringFormat(Bits);
	}

	public decimal GetLargestWholeNumberValue()
	{
		if (Terabytes >= 1)
		{
			return Terabytes;
		}

		if (Gigabytes >= 1)
		{
			return Gigabytes;
		}

		if (Megabytes >= 1)
		{
			return Megabytes;
		}

		if (Kilobytes >= 1)
		{
			return Kilobytes;
		}

		if (Bytes >= 1)
		{
			return Bytes;
		}

		return Bits;
	}

	public static ByteSize operator +(ByteSize b1, ByteSize b2)
	{
		return new ByteSize(b1.Bytes + b2.Bytes);
	}

	public static ByteSize operator --(ByteSize b)
	{
		return new ByteSize(b.Bytes - 1);
	}

	public static bool operator ==(ByteSize b1, ByteSize b2)
	{
		return b1.Bits == b2.Bits;
	}

	public static bool operator >(ByteSize b1, ByteSize b2)
	{
		return b1.Bits > b2.Bits;
	}

	public static bool operator >=(ByteSize b1, ByteSize b2)
	{
		return b1.Bits >= b2.Bits;
	}

	public static ByteSize operator ++(ByteSize b)
	{
		return new ByteSize(b.Bytes + 1);
	}

	public static bool operator !=(ByteSize b1, ByteSize b2)
	{
		return b1.Bits != b2.Bits;
	}

	public static bool operator <(ByteSize b1, ByteSize b2)
	{
		return b1.Bits < b2.Bits;
	}

	public static bool operator <=(ByteSize b1, ByteSize b2)
	{
		return b1.Bits <= b2.Bits;
	}

	public static ByteSize operator -(ByteSize b1, ByteSize b2)
	{
		return new ByteSize(b1.Bytes - b2.Bytes);
	}

	public static ByteSize operator -(ByteSize b)
	{
		return new ByteSize(-b.Bytes);
	}

	public static ByteSize Parse(string s)
	{
		return Parse(s, null);
	}

	public static ByteSize Parse(string s, IFormatProvider formatProvider)
	{
		if (s == null)
		{
			throw new ArgumentNullException(nameof(s));
		}

		if (TryParse(s, formatProvider, out var result))
		{
			return result;
		}

		throw new FormatException("Value is not in the correct format");
	}

	public ByteSize Subtract(ByteSize bs)
	{
		return new ByteSize(Bytes - bs.Bytes);
	}

	/// <summary>
	/// Converts the value of the current ByteSize object to a string with
	/// full words. The metric prefix symbol (bit, byte, kilo, mega, giga,
	/// tera) used is the largest metric prefix such that the corresponding
	/// value is greater than or equal to one.
	/// </summary>
	public string ToFullWords(string format = null, IFormatProvider provider = null)
	{
		return ToString(format, provider, WordFormat.Full);
	}

	/// <summary>
	/// Converts the value of the current ByteSize object to a string.
	/// The metric prefix symbol (bit, byte, kilo, mega, giga, tera) used is
	/// the largest metric prefix such that the corresponding value is greater
	/// than or equal to one.
	/// </summary>
	public override string ToString()
	{
		return $"{LargestWholeNumberValue:0.##} {GetLargestWholeNumberSymbol()}";
	}

	public string ToString(string format)
	{
		return ToString(format, NumberFormatInfo.CurrentInfo);
	}

	public string ToString(string format, IFormatProvider provider)
	{
		return ToString(format, provider, WordFormat.Abbreviation);
	}

	public string ToUnitString(ByteUnit unit, WordFormat symbolFormat = WordFormat.Full)
	{
		return unit switch
		{
			ByteUnit.Byte => $"{Bytes:0.##} {ByteUnit.Byte.GetHumanizeStringFormat(Bytes, symbolFormat)}",
			ByteUnit.Kilobit => $"{Kilobits:0.##} {ByteUnit.Kilobit.GetHumanizeStringFormat(Kilobits, symbolFormat)}",
			ByteUnit.Kilobyte => $"{Kilobytes:0.##} {ByteUnit.Kilobyte.GetHumanizeStringFormat(Kilobytes, symbolFormat)}",
			ByteUnit.Megabit => $"{Megabits:0.##} {ByteUnit.Megabit.GetHumanizeStringFormat(Megabits, symbolFormat)}",
			ByteUnit.Megabyte => $"{Megabytes:0.##} {ByteUnit.Megabyte.GetHumanizeStringFormat(Megabytes, symbolFormat)}",
			ByteUnit.Gigabit => $"{Gigabits:0.##} {ByteUnit.Gigabit.GetHumanizeStringFormat(Gigabits, symbolFormat)}",
			ByteUnit.Gigabyte => $"{Gigabytes:0.##} {ByteUnit.Gigabyte.GetHumanizeStringFormat(Gigabytes, symbolFormat)}",
			ByteUnit.Terabit => $"{Terabits:0.##} {ByteUnit.Terabit.GetHumanizeStringFormat(Terabits, symbolFormat)}",
			ByteUnit.Terabyte => $"{Terabytes:0.##} {ByteUnit.Terabyte.GetHumanizeStringFormat(Terabytes, symbolFormat)}",
			_ => $"{Bits:0.##} {ByteUnit.Bit.GetHumanizeStringFormat(Bits, symbolFormat)}"
		};
	}

	public static bool TryParse(string s, out ByteSize result)
	{
		return TryParse(s, null, out result);
	}

	public static bool TryParse(string s, IFormatProvider formatProvider, out ByteSize result)
	{
		// Arg checking
		if (string.IsNullOrWhiteSpace(s))
		{
			result = default;
			return false;
		}

		// Acquiring culture-specific parsing info
		var numberFormat = GetNumberFormatInfo(formatProvider);

		const NumberStyles numberStyles = AllowDecimalPoint | AllowThousands | AllowLeadingSign;
		var numberSpecialChars = new[]
		{
			System.Convert.ToChar(numberFormat.NumberDecimalSeparator),
			System.Convert.ToChar(numberFormat.NumberGroupSeparator),
			System.Convert.ToChar(numberFormat.PositiveSign),
			System.Convert.ToChar(numberFormat.NegativeSign)
		};

		// Get the index of the first non-digit character
		s = s.TrimStart(); // Protect against leading spaces

		int num;
		var found = false;

		// Pick first non-digit number
		for (num = 0; num < s.Length; num++)
		{
			if (!(char.IsDigit(s[num]) || numberSpecialChars.Contains(s[num])))
			{
				found = true;
				break;
			}
		}

		if (found == false)
		{
			result = default;
			return false;
		}

		var lastNumber = num;

		// Cut the input string in half
		var numberPart = s.Substring(0, lastNumber).Trim();
		var sizePart = s.Substring(lastNumber, s.Length - lastNumber).Trim();

		// Get the numeric part
		if (!decimal.TryParse(numberPart, numberStyles, formatProvider, out var number))
		{
			result = default;
			return false;
		}

		ByteSize? response = sizePart switch
		{
			// Get the magnitude part
			BitSymbol => FromBits((long) number),
			ByteSymbol => FromBytes(number),
			KilobitSymbol => FromKilobits(number),
			KilobyteSymbol => FromKilobytes(number),
			MegabitSymbol => FromMegabits(number),
			MegabyteSymbol => FromMegabytes(number),
			GigabyteSymbol => FromGigabytes(number),
			TerabyteSymbol => FromTerabytes(number),
			_ => null
		};

		result = response ?? default;
		return response != null;
	}

	/// <inheritdoc />
	public void Write(ICodeWriter writer)
	{
		var code = GetLargestByteUnit() switch
		{
			ByteUnit.Terabyte => $"ByteSize.{nameof(FromTerabytes)}({LargestWholeNumberValue})",
			ByteUnit.Terabit => $"ByteSize.{nameof(FromTerabits)}({LargestWholeNumberValue})",
			ByteUnit.Gigabyte => $"ByteSize.{nameof(FromGigabytes)}({LargestWholeNumberValue})",
			ByteUnit.Gigabit => $"ByteSize.{nameof(FromGigabits)}({LargestWholeNumberValue})",
			ByteUnit.Megabyte => $"ByteSize.{nameof(FromMegabytes)}({LargestWholeNumberValue})",
			ByteUnit.Megabit => $"ByteSize.{nameof(FromMegabits)}({LargestWholeNumberValue})",
			ByteUnit.Kilobyte => $"ByteSize.{nameof(FromKilobytes)}({LargestWholeNumberValue})",
			ByteUnit.Kilobit => $"ByteSize.{nameof(FromKilobits)}({LargestWholeNumberValue})",
			ByteUnit.Byte => $"ByteSize.{nameof(FromBytes)}({LargestWholeNumberValue})",
			ByteUnit.Bit => $"ByteSize.{nameof(FromBits)}({LargestWholeNumberValue})",
			_ => $"ByteSize.{nameof(FromBits)}({LargestWholeNumberValue})"
		};
		writer.Append(code);
	}

	private static NumberFormatInfo GetNumberFormatInfo(IFormatProvider formatProvider)
	{
		if (formatProvider is NumberFormatInfo numberFormat)
		{
			return numberFormat;
		}

		var culture = formatProvider as CultureInfo ?? CultureInfo.CurrentCulture;

		return culture.NumberFormat;
	}

	private string ToString(string format, IFormatProvider provider, WordFormat symbolFormat)
	{
		format ??= "G";
		provider ??= CultureInfo.CurrentCulture;

		if (format == "G")
		{
			format = "0.##";
		}

		if (!format.Contains("#") && !format.Contains("0"))
		{
			format = "0.## " + format;
		}

		format = format.Replace("#.##", "0.##");

		var culture = provider as CultureInfo ?? CultureInfo.CurrentCulture;

		bool has(string s)
		{
			return culture.CompareInfo.IndexOf(format, s, CompareOptions.IgnoreCase) != -1;
		}

		string output(decimal n)
		{
			return n.ToString(format, provider);
		}

		if (has(TerabitSymbol))
		{
			format = format.Replace(TerabitSymbol, ByteUnit.Terabit.GetHumanizeStringFormat(Terabits, symbolFormat));
			return output(Terabits);
		}

		if (has(TerabyteSymbol))
		{
			format = format.Replace(TerabyteSymbol, ByteUnit.Terabyte.GetHumanizeStringFormat(Terabytes, symbolFormat));
			return output(Terabytes);
		}

		if (has(GigabitSymbol))
		{
			format = format.Replace(GigabitSymbol, ByteUnit.Gigabit.GetHumanizeStringFormat(Gigabits, symbolFormat));
			return output(Gigabits);
		}

		if (has(GigabyteSymbol))
		{
			format = format.Replace(GigabyteSymbol, ByteUnit.Gigabyte.GetHumanizeStringFormat(Gigabytes, symbolFormat));
			return output(Gigabytes);
		}

		if (has(MegabitSymbol))
		{
			format = format.Replace(MegabitSymbol, ByteUnit.Megabit.GetHumanizeStringFormat(Megabits, symbolFormat));
			return output(Megabits);
		}

		if (has(MegabyteSymbol))
		{
			format = format.Replace(MegabyteSymbol, ByteUnit.Megabyte.GetHumanizeStringFormat(Megabytes, symbolFormat));
			return output(Megabytes);
		}

		if (has(KilobitSymbol))
		{
			format = format.Replace(KilobitSymbol, ByteUnit.Kilobit.GetHumanizeStringFormat(Kilobits, symbolFormat));
			return output(Kilobits);
		}

		if (has(KilobyteSymbol))
		{
			format = format.Replace(KilobyteSymbol, ByteUnit.Kilobyte.GetHumanizeStringFormat(Kilobytes, symbolFormat));
			return output(Kilobytes);
		}

		// Byte and Bit symbol look must be case-sensitive
		if (format.IndexOf(ByteSymbol, StringComparison.Ordinal) != -1)
		{
			format = format.Replace(ByteSymbol, ByteUnit.Byte.GetHumanizeStringFormat(Bytes, symbolFormat));
			return output(Bytes);
		}

		if (format.IndexOf(BitSymbol, StringComparison.Ordinal) != -1)
		{
			format = format.Replace(BitSymbol, ByteUnit.Bit.GetHumanizeStringFormat(Bits, symbolFormat));
			return output(Bits);
		}

		var formattedLargeWholeNumberValue = LargestWholeNumberValue.ToString(format, provider);
		formattedLargeWholeNumberValue = formattedLargeWholeNumberValue.Equals(string.Empty) ? "0" : formattedLargeWholeNumberValue;

		return $"{formattedLargeWholeNumberValue} {(symbolFormat == WordFormat.Abbreviation ? GetLargestWholeNumberSymbol() : GetLargestWholeNumberFullWord())}";
	}

	#endregion
}