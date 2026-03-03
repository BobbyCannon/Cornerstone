#region References

using System;
using System.Collections;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Profiling;

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
			var actual = (NextIndex + index) % _buffer.Length;
			_buffer[actual] = value;
		}
	}

	public int Length => _buffer.Length;

	public int NextIndex { get; private set; }

	#endregion

	#region Methods

	public void Add(double value)
	{
		_buffer[NextIndex] = value;

		NextIndex = (NextIndex + 1) % _buffer.Length;
		DataChanged?.Invoke(this, EventArgs.Empty);
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
		return $"Next write at index {NextIndex}, Capacity = {Length}";
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
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

	#endregion

	#region Methods

	void Add(double value);

	#endregion

	#region Events

	event EventHandler DataChanged;

	#endregion
}