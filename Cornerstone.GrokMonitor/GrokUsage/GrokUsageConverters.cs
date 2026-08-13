#region References

using System.Collections.Generic;
using System.Linq;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage;

/// <summary>
/// XAML converters for Grok usage views.
/// </summary>
public static class GrokUsageConverters
{
	#region Fields

	/// <summary>
	/// Formats token counts as compact K/M/B (e.g. 1.5K, 65.2M).
	/// </summary>
	public static readonly FuncValueConverter<object, string> CompactTokens;

	/// <summary>
	/// Multi-binding: TotalTokens, enabled, soft threshold, hot threshold → heat brush.
	/// </summary>
	public static readonly FuncMultiValueConverter<object, IBrush> TokenHeatBackground;

	#endregion

	#region Constructors

	static GrokUsageConverters()
	{
		CompactTokens = new FuncValueConverter<object, string>(FormatCompactTokens);
		TokenHeatBackground = new FuncMultiValueConverter<object, IBrush>(ToTokenHeatBrush);
	}

	#endregion

	#region Methods

	private static string FormatCompactTokens(object value)
	{
		if (value == null)
		{
			return "0";
		}

		return value switch
		{
			long l => GrokUsageAnalytics.FormatCompactTokens(l),
			int i => GrokUsageAnalytics.FormatCompactTokens(i),
			double d => GrokUsageAnalytics.FormatCompactTokens(d),
			float f => GrokUsageAnalytics.FormatCompactTokens(f),
			decimal m => GrokUsageAnalytics.FormatCompactTokens((double) m),
			_ => double.TryParse(value.ToString(), out var parsed)
				? GrokUsageAnalytics.FormatCompactTokens(parsed)
				: "0"
		};
	}

	private static long ToLong(object value)
	{
		return value switch
		{
			long l => l,
			int i => i,
			uint u => u,
			double d => (long) d,
			float f => (long) f,
			decimal m => (long) m,
			_ => 0L
		};
	}

	private static IBrush ToTokenHeatBrush(IEnumerable<object> values)
	{
		if (values == null)
		{
			return Brushes.Transparent;
		}

		var list = values.ToList();
		if (list.Count < 4)
		{
			return Brushes.Transparent;
		}

		var tokens = ToLong(list[0]);

		// Unset / non-bool during recycle: treat as enabled so default heat still works.
		var enabled = list[1] is not bool flag || flag;

		var soft = ToLong(list[2]);
		var hot = ToLong(list[3]);
		if ((soft <= 0) && (hot <= 0))
		{
			soft = GrokUsageAnalytics.TokenHeatSoftThreshold;
			hot = GrokUsageAnalytics.TokenHeatHotThreshold;
		}

		var heat = GrokUsageAnalytics.GetTokenHeat(tokens, enabled, soft, hot);
		if (heat.IsNone)
		{
			return Brushes.Transparent;
		}

		return new ImmutableSolidColorBrush(Color.FromArgb(heat.A, heat.R, heat.G, heat.B));
	}

	#endregion
}