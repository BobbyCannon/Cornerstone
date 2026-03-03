#region References

using System;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Text.CodeGenerators.TypeGenerators;

public class TimeCodeGenerator : CodeGenerator
{
	#region Constructors

	public TimeCodeGenerator() : base(SourceTypes.TimeTypes)
	{
	}

	#endregion

	#region Methods

	public override void WriteObject(CodeBuilder builder, SourceTypeInfo typeInfo, object value)
	{
		var code = value switch
		{
			null => "null",
			#if !NETSTANDARD
			TimeOnly sValue when sValue == TimeOnly.MaxValue => "TimeOnly.MaxValue",
			TimeOnly sValue when sValue == TimeOnly.MinValue => "TimeOnly.MinValue",
			TimeOnly sValue => $"TimeOnly.Parse(\"{sValue:O}\")",
			#endif
			TimeSpan sValue when sValue == TimeSpan.MaxValue => "TimeSpan.MaxValue",
			TimeSpan sValue when sValue == TimeSpan.MinValue => "TimeSpan.MinValue",
			TimeSpan sValue when sValue == TimeSpan.Zero => "TimeSpan.Zero",
			TimeSpan sValue => GetTimeSpan(sValue),
			_ => throw new CornerstoneException($"Type ({typeInfo.Type.FullName}) not supported.")
		};

		builder.Write(code);
	}

	internal static string GetTimeSpan(TimeSpan timeSpan)
	{
		var t = timeSpan;

		#if NET7_0_OR_GREATER
		if (t.Microseconds > 0)
		{
			return $"new TimeSpan({t.Days},{t.Hours}, {t.Minutes}, {t.Seconds}, {t.Milliseconds}, {t.Microseconds})";
		}
		#endif

		if (t.Milliseconds > 0)
		{
			return $"new TimeSpan({t.Days},{t.Hours}, {t.Minutes}, {t.Seconds}, {t.Milliseconds})";
		}

		return t.Days > 0
			? $"new TimeSpan({t.Days},{t.Hours}, {t.Minutes}, {t.Seconds})"
			: $"new TimeSpan({t.Hours}, {t.Minutes}, {t.Seconds})";
	}

	#endregion
}