#region References

using System;
using Avalonia.Data.Converters;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class TimeSpanConverters
{
	#region Fields

	public static readonly IValueConverter Format;
	public static readonly IValueConverter Humanize;
	public static readonly FuncValueConverter<TimeSpan, object, int> ToSeconds;

	#endregion

	#region Constructors

	static TimeSpanConverters()
	{
		Humanize = new FuncValueConverter<TimeSpan, string>(x => x.Humanize());
		Format = new FuncValueConverter<TimeSpan, string, string>(ConvertTimeSpan);
		ToSeconds = new(ConvertToSeconds);
	}

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