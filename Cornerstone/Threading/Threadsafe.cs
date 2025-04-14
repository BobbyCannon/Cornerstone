#region References

using System.Threading;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Threading;

/// <summary>
/// Provides thread safe operations for variables that are shared by multiple threads.
/// </summary>
public static class ThreadSafe
{
	#region Fields

	private static readonly object _decimalLock, _doubleLock, _floatLock, _ulongLock;

	#endregion

	#region Constructors

	static ThreadSafe()
	{
		_decimalLock = new object();
		_doubleLock = new object();
		_floatLock = new object();
		_ulongLock = new object();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Decrement a float by a value or float.Epsilon if not provided.
	/// </summary>
	/// <param name="value"> The value to be decremented. </param>
	/// <param name="decrease"> An optional decrease. The value defaults to the smallest possible value. </param>
	/// <returns> The incremented value. </returns>
	public static float Decrement(ref float value, float decrease = float.Epsilon)
	{
		lock (_doubleLock)
		{
			value = value.Decrement(decrease);
			return value;
		}
	}

	/// <summary>
	/// Decrement a double by a value or double.Epsilon if not provided.
	/// </summary>
	/// <param name="value"> The value to be decremented. </param>
	/// <param name="decrease"> An optional decrease. The value defaults to the smallest possible value. </param>
	/// <returns> The incremented value. </returns>
	public static double Decrement(ref double value, double decrease = double.Epsilon)
	{
		lock (_doubleLock)
		{
			value = value.Decrement(decrease);
			return value;
		}
	}

	/// <summary>
	/// Decrement an ulong by a value or default(1) if not provided.
	/// </summary>
	/// <param name="value"> The value to be decremented. </param>
	/// <param name="decrease"> An optional decrease. The value defaults to the smallest possible value. </param>
	/// <returns> The incremented value. </returns>
	public static ulong Decrement(ref ulong value, ulong decrease = 1)
	{
		lock (_ulongLock)
		{
			value -= decrease;
			return value;
		}
	}

	/// <summary>
	/// Decrement an int by a value or default(1) if not provided.
	/// </summary>
	/// <param name="value"> The value to be decremented. </param>
	/// <param name="decrease"> An optional decrease. The value defaults to the smallest possible value. </param>
	public static int Decrement(ref int value, int decrease = 1)
	{
		return Interlocked.Add(ref value, decrease * -1);
	}

	/// <summary>
	/// Decrement a decimal by a value or default(1) if not provided.
	/// </summary>
	/// <param name="value"> The value to be decremented. </param>
	/// <param name="decrease"> An optional decrease. The value defaults to the smallest possible value. </param>
	public static decimal Decrement(ref decimal value, decimal decrease = 1)
	{
		lock (_decimalLock)
		{
			value -= decrease;
			return value;
		}
	}

	/// <summary>
	/// Decrement a long by a value or default(1) if not provided.
	/// </summary>
	/// <param name="value"> The value to be decremented. </param>
	/// <param name="decrease"> An optional decrease. The value defaults to the smallest possible value. </param>
	public static long Decrement(ref long value, long decrease = 1)
	{
		return Interlocked.Add(ref value, decrease * -1);
	}

	/// <summary>
	/// Increment a float by a value or float.Epsilon if not provided.
	/// </summary>
	/// <param name="value"> The value to be incremented. </param>
	/// <param name="increase"> An optional increase. The value defaults to the smallest possible value. </param>
	/// <returns> The incremented value. </returns>
	public static float Increment(ref float value, float increase = float.Epsilon)
	{
		lock (_floatLock)
		{
			value = value.Increment(increase);
			return value;
		}
	}

	/// <summary>
	/// Increment a double by a value or double.Epsilon if not provided.
	/// </summary>
	/// <param name="value"> The value to be incremented. </param>
	/// <param name="increase"> An optional increase. The value defaults to the smallest possible value. </param>
	/// <returns> The incremented value. </returns>
	public static double Increment(ref double value, double increase = double.Epsilon)
	{
		lock (_doubleLock)
		{
			value = value.Increment(increase);
			return value;
		}
	}

	/// <summary>
	/// Increment a decimal by a value or default(1) if not provided.
	/// </summary>
	/// <param name="value"> The value to be incremented. </param>
	/// <param name="increase"> An optional increase. The value defaults to the smallest possible value. </param>
	/// <returns> The incremented value. </returns>
	public static decimal Increment(ref decimal value, decimal increase = 1)
	{
		lock (_doubleLock)
		{
			value += increase;
			return value;
		}
	}

	/// <summary>
	/// Increment an int by a value or default(1) if not provided.
	/// </summary>
	/// <param name="value"> The value to be incremented. </param>
	/// <param name="increase"> An optional increase. The value defaults to the smallest possible value. </param>
	public static int Increment(ref int value, int increase = 1)
	{
		return Interlocked.Add(ref value, increase);
	}

	/// <summary>
	/// Increment a long by a value or default(1) if not provided.
	/// </summary>
	/// <param name="value"> The value to be incremented. </param>
	/// <param name="increase"> An optional increase. The value defaults to the smallest possible value. </param>
	public static long Increment(ref long value, long increase = 1)
	{
		return Interlocked.Add(ref value, increase);
	}

	/// <summary>
	/// Increment an ulong by a value or default(1) if not provided.
	/// </summary>
	/// <param name="value"> The value to be incremented. </param>
	/// <param name="increase"> An optional increase. The value defaults to the smallest possible value. </param>
	public static ulong Increment(ref ulong value, ulong increase = 1)
	{
		lock (_ulongLock)
		{
			value += increase;
			return value;
		}
	}

	/// <summary>
	/// Set the value to new value.
	/// </summary>
	/// <param name="value"> The value to be updated. </param>
	/// <param name="newValue"> The new value to be set. </param>
	public static int Set(ref int value, int newValue)
	{
		return Interlocked.Exchange(ref value, newValue);
	}

	/// <summary>
	/// Set the value to new value.
	/// </summary>
	/// <param name="value"> The value to be updated. </param>
	/// <param name="newValue"> The new value to be set. </param>
	public static double Set(ref double value, double newValue)
	{
		return Interlocked.Exchange(ref value, newValue);
	}

	/// <summary>
	/// Set the value to new value.
	/// </summary>
	/// <param name="value"> The value to be updated. </param>
	/// <param name="newValue"> The new value to be set. </param>
	public static float Set(ref float value, float newValue)
	{
		return Interlocked.Exchange(ref value, newValue);
	}

	/// <summary>
	/// Set the value to new value.
	/// </summary>
	/// <param name="value"> The value to be updated. </param>
	/// <param name="newValue"> The new value to be set. </param>
	public static ulong Set(ref ulong value, ulong newValue)
	{
		lock (_ulongLock)
		{
			value = newValue;
			return value;
		}
	}

	/// <summary>
	/// Set the value to new value.
	/// </summary>
	/// <param name="value"> The value to be updated. </param>
	/// <param name="newValue"> The new value to be set. </param>
	public static decimal Set(ref decimal value, decimal newValue)
	{
		lock (_decimalLock)
		{
			value = newValue;
			return value;
		}
	}

	#endregion
}