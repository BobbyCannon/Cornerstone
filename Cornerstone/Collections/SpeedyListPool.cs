#region References

using System.Threading;

#endregion

namespace Cornerstone.Collections;

public class SpeedyListPool<T> where T : unmanaged
{
	#region Fields

	private readonly ThreadLocal<SpeedyList<T>> _buffer;

	#endregion

	#region Constructors

	public SpeedyListPool()
	{
		_buffer = new ThreadLocal<SpeedyList<T>>(() => null);
	}

	#endregion

	#region Methods

	public SpeedyList<T> Rent(int initialCapacity)
	{
		var buffer = _buffer.Value;
		if (buffer == null)
		{
			buffer = new SpeedyList<T>(initialCapacity);
		}
		else
		{
			// Remove from thread-local storage
			_buffer.Value = null;

			buffer.EnsureCapacity(initialCapacity);
			buffer.Clear();
		}
		return buffer;
	}

	public void Return(SpeedyList<T> buffer)
	{
		if (buffer == null)
		{
			return;
		}

		// Clear is deferred to Rent() — no need to double-clear
		_buffer.Value ??= buffer;
	}

	#endregion
}