#region References

using System.Threading;

#endregion

namespace Cornerstone.Text;

public class StringBufferPool
{
	#region Fields

	private readonly ThreadLocal<StringBuffer> _buffer;

	#endregion

	#region Constructors

	public StringBufferPool()
	{
		_buffer = new ThreadLocal<StringBuffer>(() => null);
	}

	#endregion

	#region Methods

	public StringBuffer Rent(int initialCapacity)
	{
		var buffer = _buffer.Value;
		if (buffer == null)
		{
			buffer = new StringBuffer(initialCapacity);
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

	public void Return(StringBuffer buffer)
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