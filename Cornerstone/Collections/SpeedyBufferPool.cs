#region References

using System.Threading;

#endregion

namespace Cornerstone.Collections;

public class SpeedyBufferPool<T> where T : unmanaged
{
	#region Fields

	private readonly ThreadLocal<SpeedyBuffer<T>> _buffer;

	#endregion

	#region Constructors

	public SpeedyBufferPool()
	{
		_buffer = new ThreadLocal<SpeedyBuffer<T>>(() => null);
	}

	#endregion

	#region Methods

	public SpeedyBuffer<T> Rent(int initialCapacity)
	{
		var buffer = _buffer.Value;
		if (buffer == null)
		{
			buffer = new SpeedyBuffer<T>(initialCapacity);
		}
		else
		{
			buffer.EnsureCapacity(initialCapacity);
			_buffer.Value = null; // Remove from thread-local storage
		}
		return buffer;
	}

	public void Return(SpeedyBuffer<T> buffer)
	{
		if (buffer == null)
		{
			return;
		}

		buffer.Clear();
		if (_buffer.Value == null)
		{
			_buffer.Value = buffer; // Store back in thread-local storage
		}
	}

	#endregion
}