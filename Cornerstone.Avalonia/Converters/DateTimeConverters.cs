#region References

using System;
using Avalonia.Data.Converters;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class DateTimeConverters
{
	#region Fields

	public static readonly FuncValueConverter<DateTime, string> ToLocalTime;

	#endregion

	#region Constructors

	static DateTimeConverters()
	{
		ToLocalTime = new(x => x.ToLocalTime().ToString());
	}

	#endregion
}