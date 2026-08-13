#region References

using System;
using System.Collections;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// Fixed-size ring buffer of doubles for series charts and profiling history.
/// Use <see cref="CopyFrom" /> to snapshot a model series into a view series once per
/// dispatch tick; <see cref="Version" /> aligns so the view can detect drift without an
/// external applied-version field (<c>model.Version != view.Version</c>).
/// </summary>
public class SeriesDataProvider : ISeriesDataProvider
{
	#region Fields

	private readonly double[] _buffer;

	#endregion

	#region Constructors

	public SeriesDataProvider() : this(60)
	{
	}

	public SeriesDataProvider(int size)
	{
		if (size <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(size));
		}
		_buffer = new double[size];
		NextIndex = 0;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Chronological sample at <paramref name="index" /> (0 = oldest, Length-1 = newest).
	/// The setter is a raw write and does not bump <see cref="Version" /> or raise
	/// <see cref="DataChanged" />; prefer <see cref="Add" />, <see cref="AddRange" />, or
	/// <see cref="CopyFrom" /> for observable updates.
	/// </summary>
	public double this[int index]
	{
		get
		{
			if ((index < 0) || (index >= _buffer.Length))
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			var actual = (NextIndex + index) % _buffer.Length;
			return _buffer[actual];
		}
		set
		{
			if ((index < 0) || (index >= _buffer.Length))
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			var actual = (NextIndex + index) % _buffer.Length;
			_buffer[actual] = value;
		}
	}

	public int Length => _buffer.Length;

	public int NextIndex { get; private set; }

	/// <summary>
	/// Monotonic revision for observable mutations (<see cref="Add" />, <see cref="AddRange" />).
	/// <see cref="CopyFrom" /> sets this to the source's version so a view series can mirror
	/// a model series with <c>HasModelChanges =&gt; model.Version != view.Version</c>.
	/// </summary>
	public ulong Version { get; private set; }

	#endregion

	#region Methods

	public void Add(double value)
	{
		_buffer[NextIndex] = value;
		NextIndex = (NextIndex + 1) % _buffer.Length;
		Version++;
		OnDataChanged();
	}

	/// <summary>
	/// Appends many samples through the ring and raises <see cref="DataChanged" /> once.
	/// </summary>
	public void AddRange(ReadOnlySpan<double> values)
	{
		if (values.IsEmpty)
		{
			return;
		}

		for (var i = 0; i < values.Length; i++)
		{
			_buffer[NextIndex] = values[i];
			NextIndex = (NextIndex + 1) % _buffer.Length;
		}

		Version++;
		OnDataChanged();
	}

	/// <summary>
	/// Overwrites the entire buffer with chronological samples (index 0 = oldest),
	/// sets a linear layout (<see cref="NextIndex" /> = 0), and raises
	/// <see cref="DataChanged" /> once. Length must match.
	/// Prefer this for static chart snapshots (daily totals) over ring <see cref="AddRange" />.
	/// </summary>
	public void ReplaceAll(ReadOnlySpan<double> values)
	{
		if (values.Length != _buffer.Length)
		{
			throw new ArgumentException(
				$"Value count ({values.Length}) must match series length ({_buffer.Length}).",
				nameof(values));
		}

		for (var i = 0; i < values.Length; i++)
		{
			_buffer[i] = values[i];
		}

		NextIndex = 0;
		Version++;
		OnDataChanged();
	}

	/// <summary>
	/// Replaces this series with a chronological snapshot of <paramref name="source" />
	/// and sets <see cref="Version" /> to <paramref name="source" />'s version (single notify).
	/// Lengths must match.
	/// </summary>
	public void CopyFrom(ISeriesDataProvider source)
	{
		if (source is null)
		{
			throw new ArgumentNullException(nameof(source));
		}

		if (source.Length != Length)
		{
			throw new ArgumentException(
				$"Source length ({source.Length}) must match destination length ({Length}).",
				nameof(source));
		}

		if (ReferenceEquals(source, this))
		{
			return;
		}

		// Fast path: same concrete type — copy ring storage layout.
		if (source is SeriesDataProvider concrete)
		{
			Array.Copy(concrete._buffer, _buffer, _buffer.Length);
			NextIndex = concrete.NextIndex;
			Version = concrete.Version;
			OnDataChanged();
			return;
		}

		// Any ISeriesDataProvider: chronological oldest→newest into a linear layout.
		var i = 0;
		foreach (var value in source)
		{
			_buffer[i++] = value;
		}

		NextIndex = 0;
		Version = source.Version;
		OnDataChanged();
	}

	public double GetCurrentValue()
	{
		return this[Length - 1];
	}

	public IEnumerator<double> GetEnumerator()
	{
		var start = NextIndex;

		for (var i = 0; i < _buffer.Length; i++)
		{
			yield return _buffer[(start + i) % _buffer.Length];
		}
	}

	public override string ToString()
	{
		return $"Next write at index {NextIndex}, Capacity = {Length}, Version = {Version}";
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private void OnDataChanged()
	{
		DataChanged?.Invoke(this, EventArgs.Empty);
	}

	#endregion

	#region Events

	public event EventHandler DataChanged;

	#endregion
}

public interface ISeriesDataProvider : IEnumerable<double>
{
	#region Properties

	double this[int index] { get; set; }

	int Length { get; }

	/// <summary>
	/// Revision for change detection (see <see cref="SeriesDataProvider.Version" />).
	/// </summary>
	ulong Version { get; }

	#endregion

	#region Methods

	void Add(double value);

	/// <summary>
	/// Appends many samples; implementations should raise <see cref="DataChanged" /> once.
	/// </summary>
	void AddRange(ReadOnlySpan<double> values);

	/// <summary>
	/// Overwrites all samples (chronological, linear layout) and raises <see cref="DataChanged" /> once.
	/// Length must match.
	/// </summary>
	void ReplaceAll(ReadOnlySpan<double> values);

	/// <summary>
	/// Snapshot <paramref name="source" /> into this instance and align <see cref="Version" />.
	/// </summary>
	void CopyFrom(ISeriesDataProvider source);

	double GetCurrentValue();

	#endregion

	#region Events

	event EventHandler DataChanged;

	#endregion
}
