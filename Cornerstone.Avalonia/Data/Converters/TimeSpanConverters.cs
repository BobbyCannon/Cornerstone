#region References

using System;
using Avalonia.Data.Converters;
using Cornerstone.Text.Human;

#endregion

namespace Cornerstone.Avalonia.Data.Converters;

/// <summary>
/// Provides a set of useful <see cref="IValueConverter" />s for working with TimeSpan values.
/// </summary>
public static class TimeSpanConverters
{
	#region Fields

	/// <summary>
	/// A value converter that returns the TimeSpan as a human string.
	/// </summary>
	public static readonly IValueConverter Humanize = new FuncValueConverter<TimeSpan, string>(x => x.Humanize());

	#endregion
}