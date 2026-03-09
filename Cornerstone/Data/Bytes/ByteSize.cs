#region References

using System;
using System.Globalization;
using static System.Globalization.NumberStyles;

#endregion

namespace Cornerstone.Data.Bytes;

/// <summary>
/// Represents a byte size value.
/// </summary>
public readonly partial struct ByteSize : IComparable<ByteSize>, IEquatable<ByteSize>, IComparable
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

		const NumberStyles NumberStyles = AllowDecimalPoint | AllowThousands | AllowLeadingSign;
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

		if (!found)
		{
			result = default;
			return false;
		}

		var lastNumber = num;

		// Cut the input string in half
		var numberPart = s.Substring(0, lastNumber).Trim();
		var sizePart = s.Substring(lastNumber, s.Length - lastNumber).Trim();

		// Get the numeric part
		if (!decimal.TryParse(numberPart, NumberStyles, formatProvider, out var number))
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

	private static NumberFormatInfo GetNumberFormatInfo(IFormatProvider formatProvider)
	{
		if (formatProvider is NumberFormatInfo numberFormat)
		{
			return numberFormat;
		}

		var culture = formatProvider as CultureInfo ?? CultureInfo.CurrentCulture;

		return culture.NumberFormat;
	}

	#endregion
}