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

	private static readonly object _doubleLock, _floatLock;

	#endregion

	#region Constructors

	static ThreadSafe()
	{
		_doubleLock = new object();
		_floatLock = new object();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Decrement an float by a value or float.Epsilon if not provided.
	/// </summary>
	/// <param name="value"> The value to be decremented. </param>
	/// <param name="decrease"> An optional decrease. The value defaults to the smallest possible value. </param>
	/// <returns> The incremented value. </returns>
	public static void Decrement(ref float value, float decrease = float.Epsilon)
	{
		lock (_doubleLock)
		{
			value = value.Decrement(decrease);
		}
	}

	/// <summary>
	/// Decrement an double by a value or double.Epsilon if not provided.
	/// </summary>
	/// <param name="value"> The value to be decremented. </param>
	/// <param name="decrease"> An optional decrease. The value defaults to the smallest possible value. </param>
	/// <returns> The incremented value. </returns>
	public static void Decrement(ref double value, double decrease = double.Epsilon)
	{
		lock (_doubleLock)
		{
			value = value.Decrement(decrease);
		}
	}

	/// <summary>
	/// Decrement an int by a value or default(1) if not provided.
	/// </summary>
	/// <param name="value"> The value to be decremented. </param>
	/// <param name="decrease"> An optional decrease. The value defaults to the smallest possible value. </param>
	public static void Decrement(ref int value, int decrease = 1)
	{
		Interlocked.Add(ref value, decrease * -1);
	}

	/// <summary>
	/// Increment an float by a value or float.Epsilon if not provided.
	/// </summary>
	/// <param name="value"> The value to be incremented. </param>
	/// <param name="increase"> An optional increase. The value defaults to the smallest possible value. </param>
	/// <returns> The incremented value. </returns>
	public static void Increment(ref float value, float increase = float.Epsilon)
	{
		lock (_floatLock)
		{
			value = value.Increment(increase);
		}
	}

	/// <summary>
	/// Increment an double by a value or double.Epsilon if not provided.
	/// </summary>
	/// <param name="value"> The value to be incremented. </param>
	/// <param name="increase"> An optional increase. The value defaults to the smallest possible value. </param>
	/// <returns> The incremented value. </returns>
	public static void Increment(ref double value, double increase = double.Epsilon)
	{
		lock (_doubleLock)
		{
			value = value.Increment(increase);
		}
	}

	/// <summary>
	/// Increment an int by a value or default(1) if not provided.
	/// </summary>
	/// <param name="value"> The value to be incremented. </param>
	/// <param name="increase"> An optional increase. The value defaults to the smallest possible value. </param>
	public static void Increment(ref int value, int increase = 1)
	{
		Interlocked.Add(ref value, increase);
	}

	/// <summary>
	/// Set the value to new value.
	/// </summary>
	/// <param name="value"> The value to be updated. </param>
	/// <param name="newValue"> The new value to be set. </param>
	public static void Set(ref int value, int newValue)
	{
		Interlocked.Exchange(ref value, newValue);
	}

	#endregion
}