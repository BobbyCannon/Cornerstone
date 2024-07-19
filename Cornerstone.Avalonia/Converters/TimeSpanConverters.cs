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

	#endregion

	#region Methods

	public static int ConvertToSeconds(TimeSpan value, object parameter)
	{
		return (int) value.TotalSeconds;
	}

	#endregion
}