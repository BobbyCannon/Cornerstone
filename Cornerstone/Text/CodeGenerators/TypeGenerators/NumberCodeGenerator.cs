#region References

using System;
using Cornerstone.Extensions;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Text.CodeGenerators.TypeGenerators;

public class NumberCodeGenerator : CodeGenerator
{
	#region Constructors

	public NumberCodeGenerator() : base(SourceTypes.NumberTypes)
	{
	}

	#endregion

	#region Methods

	public override void WriteObject(CodeBuilder builder, SourceTypeInfo typeInfo, object value)
	{
		var requiresCast = typeInfo.Type != value?.GetType();

		var code = value switch
		{
			null => "null",
			byte.MaxValue => "byte.MaxValue",
			byte.MinValue => "byte.MinValue",
			byte sValue => requiresCast ? $"(byte) {sValue}" : $"{sValue}",
			sbyte.MaxValue => "sbyte.MaxValue",
			sbyte.MinValue => "sbyte.MinValue",
			sbyte sValue => requiresCast ? $"(sbyte) {sValue}" : $"{sValue}",
			short.MaxValue => "short.MaxValue",
			short.MinValue => "short.MinValue",
			short sValue => requiresCast ? $"(short) {sValue}" : $"{sValue}",
			ushort.MaxValue => "ushort.MaxValue",
			ushort.MinValue => "ushort.MinValue",
			ushort sValue => requiresCast ? $"(ushort) {sValue}" : $"{sValue}",
			int.MaxValue => "int.MaxValue",
			int.MinValue => "int.MinValue",
			int sValue => requiresCast ? $"(int) {sValue}" : $"{sValue}",
			uint.MaxValue => "uint.MaxValue",
			uint.MinValue => "uint.MinValue",
			uint sValue => requiresCast ? $"(uint) {sValue}" : $"{sValue}",
			long.MaxValue => "long.MaxValue",
			long.MinValue => "long.MinValue",
			long sValue => requiresCast ? $"(long) {sValue}" : $"{sValue}",
			ulong.MaxValue => "ulong.MaxValue",
			ulong.MinValue => "ulong.MinValue",
			ulong sValue => requiresCast ? $"(ulong) {sValue}" : $"{sValue}",
			Int128 sValue when sValue == Int128.MaxValue => "Int128.MaxValue",
			Int128 sValue when sValue == Int128.MinValue => "Int128.MinValue",
			Int128 sValue => requiresCast ? $"(Int128) {sValue}" : $"{sValue}",
			UInt128 sValue when sValue == UInt128.MaxValue => "UInt128.MaxValue",
			UInt128 sValue when sValue == UInt128.MinValue => "UInt128.MinValue",
			UInt128 sValue => requiresCast ? $"(UInt128) {sValue}" : $"{sValue}",
			nint sValue when sValue == nint.MaxValue => "IntPtr.MaxValue",
			nint sValue when sValue == nint.MinValue => "IntPtr.MinValue",
			nint sValue => $"(IntPtr) {sValue}",
			nuint sValue when sValue == nuint.MaxValue => "UIntPtr.MaxValue",
			nuint sValue when sValue == nuint.MinValue => "UIntPtr.MinValue",
			nuint sValue => $"(UIntPtr) {sValue}",

			// decimal
			decimal.MaxValue => "decimal.MaxValue",
			decimal.MinusOne => "decimal.MinusOne",
			decimal.MinValue => "decimal.MinValue",
			decimal.One => "decimal.One",
			decimal.Zero => "decimal.Zero",
			decimal sValue => $"(decimal) {sValue}m",

			// double
			double.Epsilon => "double.Epsilon",
			double.MaxValue => "double.MaxValue",
			double.MinValue => "double.MinValue",
			double.NaN => "double.NaN",
			double.Pi => "double.Pi",
			double.Tau => "double.Tau",
			double sValue when double.IsNegativeInfinity(sValue) => "double.NegativeInfinity",
			double sValue when double.IsPositiveInfinity(sValue) => "double.PositiveInfinity",
			double sValue when double.NegativeZero.AreEqual(sValue, null, true) => "double.NegativeZero",
			double sValue => $"(double) {sValue}d",

			// float (single)
			float.Epsilon => "float.Epsilon",
			float.MaxValue => "float.MaxValue",
			float.MinValue => "float.MinValue",
			float.NaN => "float.NaN",
			float.Pi => "float.Pi",
			float.Tau => "float.Tau",
			float sValue when float.IsNegativeInfinity(sValue) => "float.NegativeInfinity",
			float sValue when float.IsPositiveInfinity(sValue) => "float.PositiveInfinity",
			float sValue when float.NegativeZero.AreEqual(sValue, null, true) => "float.NegativeZero",
			float sValue => $"(float) {sValue}f",

			// Unsupported
			_ => throw new CornerstoneException($"Type ({typeInfo.Type.FullName}) not supported.")
		};

		builder.Write(code);
	}

	#endregion
}