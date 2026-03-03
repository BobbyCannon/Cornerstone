#region References

using System;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Text.CodeGenerators.TypeGenerators;

public class DateCodeGenerator : CodeGenerator
{
	#region Constructors

	public DateCodeGenerator() : base(SourceTypes.DateTypes)
	{
	}

	#endregion

	#region Methods

	public override void WriteObject(CodeBuilder builder, SourceTypeInfo typeInfo, object value)
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
			_ => throw new CornerstoneException($"Type ({typeInfo.Type.FullName}) not supported.")
		};

		builder.Write(code);
	}

	private string GetDateOnly(DateOnly value)
	{
		var d = value;
		return $"new DateOnly({d.Year}, {d.Month}, {d.Day})";
	}

	private string GetDateTime(DateTime value)
	{
		var d = value;

		if (d.Microsecond > 0)
		{
			return value.Kind == DateTimeKind.Unspecified
				? $"new DateTime({d.Year}, {d.Month}, {d.Day}, {d.Hour}, {d.Minute}, {d.Second}, {d.Millisecond}, {d.Microsecond})"
				: $"new DateTime({d.Year}, {d.Month}, {d.Day}, {d.Hour}, {d.Minute}, {d.Second}, {d.Millisecond}, {d.Microsecond}, DateTimeKind.{value.Kind})";
		}

		if (d.Millisecond > 0)
		{
			return value.Kind == DateTimeKind.Unspecified
				? $"new DateTime({d.Year}, {d.Month}, {d.Day}, {d.Hour}, {d.Minute}, {d.Second}, {d.Millisecond})"
				: $"new DateTime({d.Year}, {d.Month}, {d.Day}, {d.Hour}, {d.Minute}, {d.Second}, {d.Millisecond}, DateTimeKind.{value.Kind})";
		}

		if (d.TimeOfDay == TimeSpan.Zero)
		{
			return value.Kind == DateTimeKind.Unspecified
				? $"new DateTime({d.Year}, {d.Month}, {d.Day})"
				: $"new DateTime({d.Year}, {d.Month}, {d.Day}, DateTimeKind.{value.Kind})";
		}

		return value.Kind == DateTimeKind.Unspecified
			? $"new DateTime({d.Year}, {d.Month}, {d.Day}, {d.Hour}, {d.Minute}, {d.Second})"
			: $"new DateTime({d.Year}, {d.Month}, {d.Day}, {d.Hour}, {d.Minute}, {d.Second}, DateTimeKind.{value.Kind})";
	}

	private string GetDateTimeOffset(DateTimeOffset value)
	{
		var d = value;
		#if NET7_0_OR_GREATER
		return $"new DateTimeOffset({d.Year}, {d.Month}, {d.Day}, {d.Hour}, {d.Minute}, {d.Second}, {d.Millisecond}, {d.Microsecond}, {TimeCodeGenerator.GetTimeSpan(d.Offset)})";
		#else
		return $"new DateTimeOffset({d.Year}, {d.Month}, {d.Day}, {d.Hour}, {d.Minute}, {d.Second}, {d.Millisecond}, {TimeCSharpGenerator.GetTimeSpan(d.Offset)})";
		#endif
	}

	#endregion
}