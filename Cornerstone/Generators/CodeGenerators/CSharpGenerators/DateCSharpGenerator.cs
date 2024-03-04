#region References

using System;
using Cornerstone.Exceptions;
using Cornerstone.Protocols.Osc;

#endregion

namespace Cornerstone.Generators.CodeGenerators.CSharpGenerators;

/// <inheritdoc />
public class DateCSharpGenerator : CSharpCodeGenerator
{
	#region Constructors

	/// <inheritdoc />
	public DateCSharpGenerator() : base(Activator.DateTypes)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void AppendCode(ICodeWriter codeWriter, Type type, object value)
	{
		//DateTime.Parse("", null, DateTimeStyles.AdjustToUniversal)

		var code = value switch
		{
			null => "null",
			#if !NETSTANDARD
			DateOnly sValue when sValue == DateOnly.MaxValue => "DateOnly.MaxValue",
			DateOnly sValue when sValue == DateOnly.MinValue => "DateOnly.MinValue",
			DateOnly sValue => GetDateOnly(sValue),
			#endif
			DateTime sValue when sValue == DateTime.MaxValue => "DateTime.MaxValue",
			DateTime sValue when sValue == DateTime.MinValue => "DateTime.MinValue",
			DateTime sValue => GetDateTime(sValue),
			DateTimeOffset sValue when sValue == DateTimeOffset.MaxValue => "DateTimeOffset.MaxValue",
			DateTimeOffset sValue when sValue == DateTimeOffset.MinValue => "DateTimeOffset.MinValue",
			DateTimeOffset sValue => GetDateTimeOffset(sValue),
			IsoDateTime sValue when sValue == IsoDateTime.MaxValue => "IsoDateTime.MaxValue",
			IsoDateTime sValue when sValue == IsoDateTime.MinValue => "IsoDateTime.MinValue",
			IsoDateTime sValue => GetIsoDateTime(sValue),
			OscTimeTag sValue when sValue == OscTimeTag.MaxValue => "OscTimeTag.MaxValue",
			OscTimeTag sValue when sValue == OscTimeTag.MinValue => "OscTimeTag.MinValue",
			OscTimeTag sValue => GetOscTimeTag(sValue),
			_ => throw new CornerstoneException($"Type ({type.FullName}) not supported.")
		};

		codeWriter.Append(code);
	}

	#if NET6_0_OR_GREATER
	private string GetDateOnly(DateOnly value)
	{
		var d = value;
		return $"new DateOnly({d.Year}, {d.Month}, {d.Day})";
	}

	#endif

	private string GetDateTime(DateTime value)
	{
		var d = value;

		#if NET7_0_OR_GREATER
		if (d.Microsecond > 0)
		{
			return $"new DateTime({d.Year}, {d.Month}, {d.Day}, {d.Hour}, {d.Minute}, {d.Second}, {d.Millisecond}, {d.Microsecond}, DateTimeKind.{value.Kind})";
		}
		#endif

		if (d.Millisecond > 0)
		{
			return $"new DateTime({d.Year}, {d.Month}, {d.Day}, {d.Hour}, {d.Minute}, {d.Second}, {d.Millisecond}, DateTimeKind.{value.Kind})";
		}

		return $"new DateTime({d.Year}, {d.Month}, {d.Day}, {d.Hour}, {d.Minute}, {d.Second}, DateTimeKind.{value.Kind})";
	}

	private string GetDateTimeOffset(DateTimeOffset value)
	{
		var d = value;
		#if NET7_0_OR_GREATER
		return $"new DateTimeOffset({d.Year}, {d.Month}, {d.Day}, {d.Hour}, {d.Minute}, {d.Second}, {d.Millisecond}, {d.Microsecond}, {TimeCSharpGenerator.GetTimeSpan(d.Offset)})";
		#else
		return $"new DateTimeOffset({d.Year}, {d.Month}, {d.Day}, {d.Hour}, {d.Minute}, {d.Second}, {d.Millisecond}, {TimeCSharpGenerator.GetTimeSpan(d.Offset)})";
		#endif
	}

	private string GetIsoDateTime(IsoDateTime value)
	{
		var d = value;
		return $"new IsoDateTime({GetDateTime(value)}, {TimeCSharpGenerator.GetTimeSpan(d.Duration)})";
	}

	private string GetOscTimeTag(OscTimeTag value)
	{
		return $"new OscTimeTag({GetDateTime(value.ToDateTime())})";
	}

	#endregion
}