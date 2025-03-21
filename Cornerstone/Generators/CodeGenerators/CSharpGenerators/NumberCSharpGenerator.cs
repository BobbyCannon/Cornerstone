#region References

using System;
using Cornerstone.Exceptions;
#if NET7_0_OR_GREATER
using Cornerstone.Extensions;
#endif

#endregion

namespace Cornerstone.Generators.CodeGenerators.CSharpGenerators;

/// <inheritdoc />
public class NumberCSharpGenerator : CSharpCodeGenerator
{
	#region Constructors

	/// <inheritdoc />
	public NumberCSharpGenerator() : base(Activator.NumberTypes)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void AppendCode(ICodeWriter codeWriter, Type type, object value)
	{
		var requiresCast = type != value?.GetType();

		var code = value switch
		{
			null => "null",
			byte sValue when sValue == byte.MaxValue => "byte.MaxValue",
			byte sValue when sValue == byte.MinValue => "byte.MinValue",
			byte sValue => requiresCast ? $"(byte) {sValue}" : $"{sValue}",
			sbyte sValue when sValue == sbyte.MaxValue => "sbyte.MaxValue",
			sbyte sValue when sValue == sbyte.MinValue => "sbyte.MinValue",
			sbyte sValue => requiresCast ? $"(sbyte) {sValue}" : $"{sValue}",
			short sValue when sValue == short.MaxValue => "short.MaxValue",
			short sValue when sValue == short.MinValue => "short.MinValue",
			short sValue => requiresCast ? $"(short) {sValue}" : $"{sValue}",
			ushort sValue when sValue == ushort.MaxValue => "ushort.MaxValue",
			ushort sValue when sValue == ushort.MinValue => "ushort.MinValue",
			ushort sValue => requiresCast ? $"(ushort) {sValue}" : $"{sValue}",
			int sValue when sValue == int.MaxValue => "int.MaxValue",
			int sValue when sValue == int.MinValue => "int.MinValue",
			int sValue => requiresCast ? $"(int) {sValue}" : $"{sValue}",
			uint sValue when sValue == uint.MaxValue => "uint.MaxValue",
			uint sValue when sValue == uint.MinValue => "uint.MinValue",
			uint sValue => requiresCast ? $"(uint) {sValue}" : $"{sValue}",
			long sValue when sValue == long.MaxValue => "long.MaxValue",
			long sValue when sValue == long.MinValue => "long.MinValue",
			long sValue => requiresCast ? $"(long) {sValue}" : $"{sValue}",
			ulong sValue when sValue == ulong.MaxValue => "ulong.MaxValue",
			ulong sValue when sValue == ulong.MinValue => "ulong.MinValue",
			ulong sValue => requiresCast ? $"(ulong) {sValue}" : $"{sValue}",
			#if (NET7_0_OR_GREATER)
			Int128 sValue when sValue == Int128.MaxValue => "Int128.MaxValue",
			Int128 sValue when sValue == Int128.MinValue => "Int128.MinValue",
			Int128 sValue => requiresCast ? $"(Int128) {sValue}" : $"{sValue}",
			UInt128 sValue when sValue == UInt128.MaxValue => "UInt128.MaxValue",
			UInt128 sValue when sValue == UInt128.MinValue => "UInt128.MinValue",
			UInt128 sValue => requiresCast ? $"(UInt128) {sValue}" : $"{sValue}",
			#endif
			#if (NET6_0_OR_GREATER)
			IntPtr sValue when sValue == IntPtr.MaxValue => "IntPtr.MaxValue",
			IntPtr sValue when sValue == IntPtr.MinValue => "IntPtr.MinValue",
			#endif
			IntPtr sValue => $"(IntPtr) {sValue}",
			#if (NET6_0_OR_GREATER)
			UIntPtr sValue when sValue == UIntPtr.MaxValue => "UIntPtr.MaxValue",
			UIntPtr sValue when sValue == UIntPtr.MinValue => "UIntPtr.MinValue",
			#endif
			UIntPtr sValue => $"(UIntPtr) {sValue}",
			decimal sValue when sValue == decimal.MaxValue => "decimal.MaxValue",
			decimal sValue when sValue == decimal.MinValue => "decimal.MinValue",
			decimal sValue => $"(decimal) {sValue}m",
			double sValue when double.IsNaN(sValue) => "double.NaN",
			double sValue when double.IsNegativeInfinity(sValue) => "double.NegativeInfinity",
			double sValue when double.IsPositiveInfinity(sValue) => "double.PositiveInfinity",
			#if NET7_0_OR_GREATER
			double sValue when double.Pi.IsEqual(sValue) => "double.Pi",
			double sValue when double.NegativeZero.IsEqual(sValue) => "double.NegativeZero",
			#endif
			double sValue when sValue >= double.MaxValue => "double.MaxValue",
			double sValue when sValue <= double.MinValue => "double.MinValue",
			double sValue => $"(double) {sValue}d",
			float sValue when float.IsNaN(sValue) => "float.NaN",
			float sValue when float.IsNegativeInfinity(sValue) => "float.NegativeInfinity",
			float sValue when float.IsPositiveInfinity(sValue) => "float.PositiveInfinity",
			#if (NET7_0_OR_GREATER)
			float sValue when float.Pi.IsEqual(sValue) => "float.Pi",
			#endif
			float sValue when sValue >= float.MaxValue => "float.MaxValue",
			float sValue when sValue <= float.MinValue => "float.MinValue",
			float sValue => $"(float) {sValue}f",
			_ => throw new CornerstoneException($"Type ({type.FullName}) not supported.")
		};

		codeWriter.Append(code);
	}

	#endregion
}