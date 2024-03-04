#region References

using System;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for enumerations
/// </summary>
public static class ExceptionExtensions
{
	#region Methods

	/// <summary>
	/// Gets the details of the exception.
	/// </summary>
	/// <param name="ex"> The exception to be processed. </param>
	/// <param name="includeStackTrace"> Optionally include the stack trace. Defaults to true. </param>
	/// <returns> The detailed string for the exception. </returns>
	public static string ToDetailedString(this Exception ex, bool includeStackTrace = true)
	{
		var builder = new TextBuilder();
		AddExceptionToBuilder(builder, ex, includeStackTrace);
		return builder.ToString().Trim().Trim(Environment.NewLine.ToCharArray());
	}

	/// <summary>
	/// Add the exception details to the string builder.
	/// </summary>
	/// <param name="builder"> The builder to be appended to. </param>
	/// <param name="exception"> The exception to be processed. </param>
	/// <param name="includeStackTrace"> Optionally include the stack trace. Defaults to true. </param>
	private static void AddExceptionToBuilder(TextBuilder builder, Exception exception, bool includeStackTrace = true)
	{
		while (exception != null)
		{
			AppendExceptionToBuilder(builder, exception, includeStackTrace);
			exception = exception.InnerException;
		}
	}

	private static void AppendExceptionToBuilder(TextBuilder builder, Exception ex, bool includeStackTrace)
	{
		builder.Append(builder.Length > 0 ? Environment.NewLine + ex.GetType().FullName : ex.GetType().FullName);
		builder.Append(builder.Length > 0 ? Environment.NewLine + ex.Message : ex.Message);

		if (includeStackTrace)
		{
			builder.Append(builder.Length > 0 ? Environment.NewLine + ex.StackTrace : ex.StackTrace);
		}

		builder.AppendLine();
	}

	#endregion
}