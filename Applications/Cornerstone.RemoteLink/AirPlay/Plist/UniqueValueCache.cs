#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.RemoteLink.AirPlay.Plist;

/// <summary>
/// Provides a cache of unique primitive values when writing a binary plist.
/// </summary>
internal sealed class UniqueValueCache
{
	#region Fields

	private readonly Dictionary<bool, int> booleans = new();
	private readonly Dictionary<DateTime, int> dates = new();
	private readonly Dictionary<double, int> doubles = new();
	private readonly Dictionary<float, int> floats = new();
	private readonly Dictionary<long, int> integers = new();
	private readonly Dictionary<string, int> strings = new();

	#endregion

	#region Methods

	/// <summary>
	/// Gets a value indicating whether the cache contains the given value.
	/// </summary>
	/// <param name="value"> The value to check for. </param>
	/// <returns> True if the cache contains the value, false otherwise. </returns>
	public bool Contains(bool value)
	{
		return booleans.ContainsKey(value);
	}

	/// <summary>
	/// Gets a value indicating whether the cache contains the given value.
	/// </summary>
	/// <param name="value"> The value to check for. </param>
	/// <returns> True if the cache contains the value, false otherwise. </returns>
	public bool Contains(long value)
	{
		return integers.ContainsKey(value);
	}

	/// <summary>
	/// Gets a value indicating whether the cache contains the given value.
	/// </summary>
	/// <param name="value"> The value to check for. </param>
	/// <returns> True if the cache contains the value, false otherwise. </returns>
	public bool Contains(float value)
	{
		return floats.ContainsKey(value);
	}

	/// <summary>
	/// Gets a value indicating whether the cache contains the given value.
	/// </summary>
	/// <param name="value"> The value to check for. </param>
	/// <returns> True if the cache contains the value, false otherwise. </returns>
	public bool Contains(double value)
	{
		return doubles.ContainsKey(value);
	}

	/// <summary>
	/// Gets a value indicating whether the cache contains the given value.
	/// </summary>
	/// <param name="value"> The value to check for. </param>
	/// <returns> True if the cache contains the value, false otherwise. </returns>
	public bool Contains(DateTime value)
	{
		return dates.ContainsKey(value);
	}

	/// <summary>
	/// Gets a value indicating whether the cache contains the given value.
	/// </summary>
	/// <param name="value"> The value to check for. </param>
	/// <returns> True if the cache contains the value, false otherwise. </returns>
	public bool Contains(string value)
	{
		return strings.ContainsKey(value);
	}

	/// <summary>
	/// Gets the index in the object table for the given value, assuming it has already been added to the cache.
	/// </summary>
	/// <param name="value"> The value to get the index of. </param>
	/// <returns> The index of the value. </returns>
	public int GetIndex(bool value)
	{
		return booleans[value];
	}

	/// <summary>
	/// Gets the index in the object table for the given value, assuming it has already been added to the cache.
	/// </summary>
	/// <param name="value"> The value to get the index of. </param>
	/// <returns> The index of the value. </returns>
	public int GetIndex(long value)
	{
		return integers[value];
	}

	/// <summary>
	/// Gets the index in the object table for the given value, assuming it has already been added to the cache.
	/// </summary>
	/// <param name="value"> The value to get the index of. </param>
	/// <returns> The index of the value. </returns>
	public int GetIndex(float value)
	{
		return floats[value];
	}

	/// <summary>
	/// Gets the index in the object table for the given value, assuming it has already been added to the cache.
	/// </summary>
	/// <param name="value"> The value to get the index of. </param>
	/// <returns> The index of the value. </returns>
	public int GetIndex(double value)
	{
		return doubles[value];
	}

	/// <summary>
	/// Gets the index in the object table for the given value, assuming it has already been added to the cache.
	/// </summary>
	/// <param name="value"> The value to get the index of. </param>
	/// <returns> The index of the value. </returns>
	public int GetIndex(DateTime value)
	{
		return dates[value];
	}

	/// <summary>
	/// Gets the index in the object table for the given value, assuming it has already been added to the cache.
	/// </summary>
	/// <param name="value"> The value to get the index of. </param>
	/// <returns> The index of the value. </returns>
	public int GetIndex(string value)
	{
		return strings[value];
	}

	/// <summary>
	/// Sets the index in the object table for the given value.
	/// </summary>
	/// <param name="value"> The value to set the index for. </param>
	/// <param name="index"> The index to set. </param>
	public void SetIndex(bool value, int index)
	{
		booleans[value] = index;
	}

	/// <summary>
	/// Sets the index in the object table for the given value.
	/// </summary>
	/// <param name="value"> The value to set the index for. </param>
	/// <param name="index"> The index to set. </param>
	public void SetIndex(long value, int index)
	{
		integers[value] = index;
	}

	/// <summary>
	/// Sets the index in the object table for the given value.
	/// </summary>
	/// <param name="value"> The value to set the index for. </param>
	/// <param name="index"> The index to set. </param>
	public void SetIndex(float value, int index)
	{
		floats[value] = index;
	}

	/// <summary>
	/// Sets the index in the object table for the given value.
	/// </summary>
	/// <param name="value"> The value to set the index for. </param>
	/// <param name="index"> The index to set. </param>
	public void SetIndex(double value, int index)
	{
		doubles[value] = index;
	}

	/// <summary>
	/// Sets the index in the object table for the given value.
	/// </summary>
	/// <param name="value"> The value to set the index for. </param>
	/// <param name="index"> The index to set. </param>
	public void SetIndex(string value, int index)
	{
		strings[value] = index;
	}

	/// <summary>
	/// Sets the index in the object table for the given value.
	/// </summary>
	/// <param name="value"> The value to set the index for. </param>
	/// <param name="index"> The index to set. </param>
	public void SetIndex(DateTime value, int index)
	{
		dates[value] = index;
	}

	#endregion
}