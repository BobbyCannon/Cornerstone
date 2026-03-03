#region References

using System;
using System.Text;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Reflection;

/// <summary>
/// Activator help when creating new instances of types.
/// </summary>
public static class SourceTypes
{
	#region Constructors

	static SourceTypes()
	{
		NonDecimalNumberTypes =
		[
			typeof(byte), typeof(byte?),
			typeof(sbyte), typeof(sbyte?),
			typeof(short), typeof(short?),
			typeof(ushort), typeof(ushort?),
			typeof(int), typeof(int?),
			typeof(uint), typeof(uint?),
			typeof(long), typeof(long?),
			typeof(ulong), typeof(ulong?),
			typeof(nint), typeof(nint?),
			typeof(nuint), typeof(nuint?),
			typeof(Int128), typeof(Int128?),
			typeof(UInt128), typeof(UInt128?)
		];

		DecimalNumberTypes =
		[
			typeof(decimal), typeof(decimal?),
			typeof(double), typeof(double?),
			typeof(float), typeof(float?)
		];

		BooleanTypes = [typeof(bool), typeof(bool?)];
		CharTypes = [typeof(char), typeof(char?)];
		DateTypes =
		[
			typeof(DateOnly), typeof(DateOnly?),
			typeof(DateTime), typeof(DateTime?),
			typeof(DateTimeOffset), typeof(DateTimeOffset?)

			//typeof(OscTimeTag), typeof(OscTimeTag?)
		];
		GuidTypes =
		[
			typeof(Guid), typeof(Guid?)

			//typeof(ShortGuid), typeof(ShortGuid?)
		];
		NumberTypes = ArrayExtensions.CombineArrays(NonDecimalNumberTypes, DecimalNumberTypes);
		StringTypes =
		[
			typeof(string),
			typeof(StringGapBuffer),
			typeof(GapBuffer<char>),
			typeof(StringBuilder)
		];
		TimeTypes =
		[
			typeof(TimeOnly), typeof(TimeOnly?),
			typeof(TimeSpan), typeof(TimeSpan?)
		];

		AllTypes = ArrayExtensions.CombineArrays(BooleanTypes, NumberTypes, CharTypes,
			StringTypes, DateTypes, TimeTypes, GuidTypes
		);
	}

	#endregion

	#region Properties

	/// <summary>
	/// All types known by the Activator.
	/// </summary>
	public static Type[] AllTypes { get; }

	/// <summary>
	/// All types for Boolean. Types supported are the following: bool, bool?
	/// </summary>
	public static Type[] BooleanTypes { get; }

	/// <summary>
	/// All types for Char. Types supported are the following: char, char?
	/// </summary>
	public static Type[] CharTypes { get; }

	/// <summary>
	/// All types for dates (time). Types supported are the following:
	/// DateTime, DateTimeOffset
	/// </summary>
	public static Type[] DateTypes { get; }

	/// <summary>
	/// All types for decimal numbers. Types supported are the following:
	/// decimal, float, double
	/// </summary>
	public static Type[] DecimalNumberTypes { get; }

	/// <summary>
	/// All types for Guid. Types supported are the following:
	/// Guid, ShortGuid
	/// </summary>
	public static Type[] GuidTypes { get; }

	/// <summary>
	/// All types for non-decimal numbers. Types supported are the following:
	/// byte, short, ushort, int, uint, long, ulong
	/// </summary>
	public static Type[] NonDecimalNumberTypes { get; }

	/// <summary>
	/// All types for numbers. Types supported are the following:
	/// byte, sbyte, short, ushort, int, uint, long, ulong, decimal, float, double
	/// </summary>
	public static Type[] NumberTypes { get; }

	/// <summary>
	/// All types for strings. Types supported are the following: string, StringBuilder, StringBuilder
	/// </summary>
	public static Type[] StringTypes { get; }

	/// <summary>
	/// All types for dates (time). Types supported are the following:
	/// TimeOnly, TimeSpan
	/// </summary>
	public static Type[] TimeTypes { get; }

	#endregion
}