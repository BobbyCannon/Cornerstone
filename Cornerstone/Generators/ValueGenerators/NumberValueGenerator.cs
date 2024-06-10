#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Generators.ValueGenerators;

/// <inheritdoc />
public class NumberValueGenerator : ValueGenerator
{
	#region Constructors

	/// <inheritdoc />
	public NumberValueGenerator() : base(Activator.NumberTypes)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override object GenerateValue(Type type)
	{
		if ((type == typeof(byte)) || (type == typeof(byte?)))
		{
			return RandomGenerator.NextByte();
		}

		if ((type == typeof(sbyte)) || (type == typeof(sbyte?)))
		{
			return RandomGenerator.NextSignedByte();
		}

		if ((type == typeof(short)) || (type == typeof(short?)))
		{
			return RandomGenerator.NextShort();
		}

		if ((type == typeof(ushort)) || (type == typeof(ushort?)))
		{
			return RandomGenerator.NextUnsignedShort();
		}

		if ((type == typeof(int)) || (type == typeof(int?)))
		{
			return RandomGenerator.NextInteger();
		}

		if ((type == typeof(uint)) || (type == typeof(uint?)))
		{
			return RandomGenerator.NextUnsignedInteger();
		}

		if ((type == typeof(long)) || (type == typeof(long?)))
		{
			return RandomGenerator.NextLong();
		}

		if ((type == typeof(ulong)) || (type == typeof(ulong?)))
		{
			return RandomGenerator.NextUnsignedLong();
		}

		if ((type == typeof(IntPtr)) || (type == typeof(IntPtr?)))
		{
			return new IntPtr(RandomGenerator.NextInteger());
		}

		if ((type == typeof(UIntPtr)) || (type == typeof(UIntPtr?)))
		{
			return new UIntPtr(RandomGenerator.NextUnsignedInteger());
		}

		if ((type == typeof(decimal)) || (type == typeof(decimal?)))
		{
			return RandomGenerator.NextDecimal();
		}

		if ((type == typeof(float)) || (type == typeof(float?)))
		{
			return (float) RandomGenerator.NextDouble(float.MinValue, float.MaxValue);
		}

		if ((type == typeof(double)) || (type == typeof(double?)))
		{
			return RandomGenerator.NextDouble();
		}

		throw new NotSupportedException($"The type ({type.FullName}) is not supported.");
	}

	/// <inheritdoc />
	public override IList<object> GenerateValues(Type type)
	{
		if ((type == typeof(byte)) || (type == typeof(byte?)))
		{
			return new object[] { byte.MinValue, (byte) 0, byte.MaxValue };
		}

		if ((type == typeof(sbyte)) || (type == typeof(sbyte?)))
		{
			return new object[] { sbyte.MinValue, (sbyte) 0, sbyte.MaxValue };
		}

		if ((type == typeof(short)) || (type == typeof(short?)))
		{
			return new object[] { short.MinValue, (short) 0, short.MaxValue };
		}

		if ((type == typeof(ushort)) || (type == typeof(ushort?)))
		{
			return new object[] { ushort.MinValue, (ushort) 0, ushort.MaxValue };
		}

		if ((type == typeof(int)) || (type == typeof(int?)))
		{
			return new object[] { int.MinValue, 0, int.MaxValue };
		}

		if ((type == typeof(uint)) || (type == typeof(uint?)))
		{
			return new object[] { uint.MinValue, (uint) 0, uint.MaxValue };
		}

		if ((type == typeof(long)) || (type == typeof(long?)))
		{
			return new object[] { long.MinValue, (long) 0, long.MaxValue };
		}

		if ((type == typeof(ulong)) || (type == typeof(ulong?)))
		{
			return new object[] { ulong.MinValue, (ulong) 0, ulong.MaxValue };
		}
		
		#if NET7_0_OR_GREATER
		if ((type == typeof(Int128)) || (type == typeof(Int128?)))
		{
			return new object[] { Int128.MinValue, (Int128) 0, Int128.MaxValue };
		}

		if ((type == typeof(UInt128)) || (type == typeof(UInt128?)))
		{
			return new object[] { UInt128.MinValue, (UInt128) 0, UInt128.MaxValue };
		}
		#endif

		if ((type == typeof(IntPtr)) || (type == typeof(IntPtr?)))
		{
			#if !NETSTANDARD
			return new object[] { IntPtr.MinValue, (IntPtr) 0, IntPtr.MaxValue };
			#else
			return new object[] { new IntPtr(int.MinValue), (IntPtr) 0, new IntPtr(int.MaxValue) };
			#endif
		}

		if ((type == typeof(UIntPtr)) || (type == typeof(UIntPtr?)))
		{
			#if !NETSTANDARD
			return new object[] { UIntPtr.MinValue, (UIntPtr) 0, UIntPtr.MaxValue };
			#else
			return new object[] { new UIntPtr(uint.MinValue), (UIntPtr) 0, new UIntPtr(uint.MaxValue) };
			#endif
		}

		if ((type == typeof(decimal)) || (type == typeof(decimal?)))
		{
			return new object[] { decimal.MinValue, (decimal) 0, decimal.MaxValue };
		}

		if ((type == typeof(float)) || (type == typeof(float?)))
		{
			return new object[]
			{
				float.MinValue, (float) 0, float.MaxValue,
				float.NegativeInfinity, float.PositiveInfinity, float.NaN,
				#if NET7_0_OR_GREATER
				float.NegativeZero, float.Pi
				#endif
			};
		}

		if ((type == typeof(double)) || (type == typeof(double?)))
		{
			return new object[]
			{
				double.MinValue, (double) 0, double.MaxValue,
				double.NegativeInfinity, double.PositiveInfinity, double.NaN,
				#if NET7_0_OR_GREATER
				double.NegativeZero, double.Pi
				#endif
			};
		}

		throw new NotSupportedException($"The type ({type.FullName}) is not supported.");
	}

	#endregion
}