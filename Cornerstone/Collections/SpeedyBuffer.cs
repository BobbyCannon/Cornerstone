#region References

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

#endregion

namespace Cornerstone.Collections;

public sealed class SpeedyBuffer<T> : IDisposable where T : unmanaged
{
	#region Constants

	public const int DefaultCapacity = 1024;

	#endregion

	#region Fields

	private T[] _buffer;
	private bool _disposed;

	[ThreadStatic]
	private static SpeedyBuffer<T> _threadLocalBuffer;

	#endregion

	#region Constructors

	public SpeedyBuffer() : this(DefaultCapacity)
	{
	}

	public SpeedyBuffer(int initialCapacity)
	{
		if (initialCapacity < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(initialCapacity));
		}

		_buffer = ArrayPool<T>.Shared.Rent(initialCapacity);
		_disposed = false;

		Position = 0;
		Length = 0;
	}

	#endregion

	#region Properties

	public int Capacity => _buffer.Length;

	public int Length { get; private set; }

	public int Position { get; private set; }

	public int Remaining => _buffer.Length - Position;

	#endregion

	#region Methods

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Append(T[] data, int offset, int count)
	{
		if (data == null)
		{
			throw new ArgumentNullException(nameof(data));
		}
		if ((offset < 0) || (count < 0) || ((offset + count) > data.Length))
		{
			throw new ArgumentOutOfRangeException(nameof(count));
		}
		EnsureCapacity(Position + count);
		Array.Copy(data, offset, _buffer, Position, count);
		Position += count;
		Length = Math.Max(Length, Position);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Append(ReadOnlySpan<T> data)
	{
		EnsureCapacity(Position + data.Length);
		data.CopyTo(_buffer.AsSpan(Position));
		Position += data.Length;
		Length = Math.Max(Length, Position);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Append(T value)
	{
		EnsureCapacity(Position + 1);
		_buffer[Position++] = value;
		Length = Math.Max(Length, Position);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<T> AsSpan()
	{
		return _buffer.AsSpan(0, Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<T> AsSpan(int start, int length)
	{
		if ((start < 0) || (length < 0) || ((start + length) > Length))
		{
			throw new ArgumentOutOfRangeException(nameof(length));
		}
		return _buffer.AsSpan(start, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Clear()
	{
		Position = 0;
		Length = 0;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			ArrayPool<T>.Shared.Return(_buffer);
			_buffer = null;
			_disposed = true;
		}
	}

	public static SpeedyBuffer<T> GetThreadLocalInstance(int initialCapacity = 1024)
	{
		if ((_threadLocalBuffer == null) || _threadLocalBuffer._disposed)
		{
			_threadLocalBuffer = new SpeedyBuffer<T>(initialCapacity);
		}
		else
		{
			_threadLocalBuffer.Reset();
		}
		return _threadLocalBuffer;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Read(T[] destination, int offset, int count)
	{
		if (destination == null)
		{
			throw new ArgumentNullException(nameof(destination));
		}
		if ((offset < 0) || (count < 0) || ((offset + count) > destination.Length))
		{
			throw new ArgumentOutOfRangeException(nameof(count));
		}
		if ((Position + count) > Length)
		{
			throw new InvalidOperationException("Not enough data to read.");
		}
		Array.Copy(_buffer, Position, destination, offset, count);
		Position += count;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T Read()
	{
		if (Position >= Length)
		{
			throw new InvalidOperationException("Not enough data to read.");
		}
		return _buffer[Position++];
	}

	public void Reset()
	{
		Position = 0;
		Length = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Seek(int position)
	{
		if ((position < 0) || (position > Length))
		{
			throw new ArgumentOutOfRangeException(nameof(position));
		}
		Position = position;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T[] ToArray()
	{
		var result = new T[Length];
		Array.Copy(_buffer, 0, result, 0, Length);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override string ToString()
	{
		if (typeof(T) == typeof(byte))
		{
			var t = Unsafe.As<T[], byte[]>(ref _buffer);
			return Encoding.UTF8.GetString(t, 0, Length);
		}
		if (typeof(T) == typeof(char))
		{
			return new string(Unsafe.As<T[], char[]>(ref _buffer), 0, Length);
		}
		return base.ToString();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void EnsureCapacity(int neededCapacity)
	{
		if (neededCapacity <= _buffer.Length)
		{
			return;
		}

		var newCapacity = Math.Max(neededCapacity, _buffer.Length * 2);
		var newBuffer = ArrayPool<T>.Shared.Rent(newCapacity);
		Array.Copy(_buffer, 0, newBuffer, 0, Length);
		ArrayPool<T>.Shared.Return(_buffer);
		_buffer = newBuffer;
	}

	#endregion
}