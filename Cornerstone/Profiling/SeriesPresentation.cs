#region References

using System;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// Publishes sample arrays into a view <see cref="SeriesDataProvider" /> for charts.
/// Prefer calling from AppDispatcher apply (same thread as the UI binding) — no extra
/// <c>IDispatcher.Dispatch</c>. Same length mutates in place; new length assigns a new provider.
/// </summary>
public static class SeriesPresentation
{
	#region Methods

	/// <summary>
	/// Ensures at least two samples (LineChart needs a segment). Empty → [0,0]; single → [0, v].
	/// </summary>
	public static double[] NormalizeChartPayload(double[] values)
	{
		if ((values == null) || (values.Length == 0))
		{
			return [0d, 0d];
		}

		if (values.Length == 1)
		{
			return [0d, values[0]];
		}

		return values;
	}

	/// <summary>
	/// Writes <paramref name="values" /> into the current view series or replaces the provider when length changes.
	/// Raises <see cref="ISeriesDataProvider.DataChanged" /> once per call path (ReplaceAll / assign of pre-filled series).
	/// </summary>
	public static void Publish(
		double[] values,
		SeriesDataProvider current,
		Action<SeriesDataProvider> assign)
	{
		if (assign is null)
		{
			throw new ArgumentNullException(nameof(assign));
		}

		var payload = NormalizeChartPayload(values);
		var length = payload.Length;

		if ((current != null) && (current.Length == length))
		{
			current.ReplaceAll(payload);
			return;
		}

		var series = new SeriesDataProvider(length);
		series.ReplaceAll(payload);
		assign(series);
	}

	#endregion
}
