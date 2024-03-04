#region References

using System;
using Cornerstone.Exceptions;

#endregion

namespace Cornerstone.Generators.CodeGenerators.CSharpGenerators;

/// <inheritdoc />
public class TimeCSharpGenerator : CSharpCodeGenerator
{
	#region Constructors

	/// <inheritdoc />
	public TimeCSharpGenerator() : base(Activator.TimeTypes)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void AppendCode(ICodeWriter codeWriter, Type type, object value)
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
			_ => throw new CornerstoneException($"Type ({type.FullName}) not supported.")
		};

		codeWriter.Append(code);
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