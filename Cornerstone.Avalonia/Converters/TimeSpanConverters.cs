#region References

using System;
using Avalonia.Data.Converters;
using Cornerstone.Text.Human;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class TimeSpanConverters
{
	#region Fields

	public static readonly FuncValueConverter<TimeSpan, object, int> ToSeconds = new(ConvertToSeconds);

	/// <summary>
	/// A value converter that returns the TimeSpan as a human string.
	/// </summary>
	public static readonly IValueConverter Humanize = new FuncValueConverter<TimeSpan, string>(x => x.Humanize());

	/// <summary>
	/// A value converter that returns the TimeSpan as a formatted string.
	/// </summary>
	public static readonly IValueConverter Format = new FuncValueConverter<TimeSpan, string, string>(ConvertTimeSpan);

	#endregion

	#region Methods

	private static string ConvertTimeSpan(TimeSpan value, string format)
	{
		return value.ToString(format);
	}

	private static int ConvertToSeconds(TimeSpan value, object parameter)
	{
		return (int) value.TotalSeconds;
	}

	#endregion
}