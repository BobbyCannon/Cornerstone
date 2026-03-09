#region References

using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Communications;

public abstract partial class Channel : IDisposable
{
	#region Constants

	public const int ClientHeaderOffset = HeaderSizePerQueue;
	public const int HeaderSizePerQueue = 16;
	public const int ServerDataStart = TotalHeaderSize;
	public const int ServerHeaderOffset = 0;

	/// <summary>
	/// 16 x 2 == 32
	/// </summary>
	public const int TotalHeaderSize = HeaderSizePerQueue * 2;

	#endregion

	#region Fields

	protected CancellationTokenSource CancellationTokenSource;
	protected EventWaitHandle ReceiveEvent;
	protected EventWaitHandle SendEvent;

	#endregion

	#region Constructors

	protected Channel(int totalCapacity)
	{
		if (totalCapacity < (TotalHeaderSize * 2))
		{
			throw new ArgumentException(Babel.Tower[BabelKeys.ArgumentInvalid]);
		}

		TotalCapacity = totalCapacity;
	}

	#endregion

	#region Properties

	public int ClientDataStart => TotalHeaderSize + ((TotalCapacity - TotalHeaderSize) / 2);

	public int DataCapacity => (TotalCapacity - TotalHeaderSize) / 2;

	[Notify]
	public partial bool IsConnected { get; private set; }

	[Notify]
	public partial bool IsListening { get; private set; }

	[Notify]
	public partial bool IsServer { get; protected set; }

	public Action<ReadOnlySpan<byte>> MessageHandler { get; set; }

	public int TotalCapacity { get; }

	#endregion

	#region Methods

	public virtual void Connect()
	{
		IsConnected = true;
		IsServer = false;
		CancellationTokenSource = new CancellationTokenSource();
		Task.Run(() => ReceiveLoopAsync(CancellationTokenSource.Token));
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Returns the current state of one queue (Server or Client) for drawing.
	/// Zero-allocation after first call.
	/// </summary>
	public QueueState GetQueueState()
	{
		if (this is not MemoryChannel mc || (mc.Buffer == null))
		{
			return null;
		}

		var headerOffset = IsServer ? ServerHeaderOffset : ClientHeaderOffset;
		var dataStart = IsServer ? ServerDataStart : ClientDataStart;
		var hdr = ReadHeader(headerOffset);

		return new QueueState(
			hdr.ReadPosition,
			hdr.WritePosition,
			DataCapacity,
			new ReadOnlyMemory<byte>(mc.Buffer, dataStart, DataCapacity)
		);
	}

	public virtual void Send(byte[] data, int offset, int length)
	{
		if ((data == null) || (length == 0))
		{
			return;
		}

		var headerOffset = IsServer ? ClientHeaderOffset : ServerHeaderOffset;
		var dataStart = IsServer ? ClientDataStart : ServerDataStart;

		var header = ReadHeader(headerOffset);
		var used = ((header.WritePosition - header.ReadPosition) + DataCapacity) % DataCapacity;
		if ((used + 4 + length) >= (DataCapacity - 1))
		{
			return;
		}

		Span<byte> lengthBytes = stackalloc byte[4];
		BinaryPrimitives.WriteInt32LittleEndian(lengthBytes, length);
		WriteWithWrap(dataStart, header.WritePosition, DataCapacity, lengthBytes);
		WriteWithWrap(dataStart, (header.WritePosition + 4) % DataCapacity, DataCapacity, data.AsSpan(offset, length));

		var newWritePos = (header.WritePosition + 4 + length) % DataCapacity;
		WriteInt32(headerOffset, newWritePos);

		SendEvent?.Set();
		OnDataChanged();
	}

	public virtual void Start()
	{
		IsListening = true;
		IsServer = true;
		CancellationTokenSource = new CancellationTokenSource();
		Task.Run(() => ReceiveLoopAsync(CancellationTokenSource.Token));
	}

	public bool TryToRead(byte[] buffer, byte[] lengthBuffer)
	{
		var headerOffset = IsServer ? ServerHeaderOffset : ClientHeaderOffset;
		var dataStart = IsServer ? ServerDataStart : ClientDataStart;

		var hdr = ReadHeader(headerOffset);
		if (hdr.ReadPosition == hdr.WritePosition)
		{
			return false;
		}

		ReadWithWrap(dataStart, hdr.ReadPosition, DataCapacity, lengthBuffer);
		var length = BitConverter.ToInt32(lengthBuffer, 0);

		if ((length <= 0) || (length > DataCapacity))
		{
			var newRead = (hdr.ReadPosition + 4 + Math.Max(0, length)) % DataCapacity;
			WriteInt32(headerOffset + 4, newRead);
			return false;
		}

		ReadWithWrap(dataStart, (hdr.ReadPosition + 4) % DataCapacity, DataCapacity, buffer.AsSpan(0, length));
		MessageHandler?.Invoke(buffer.AsSpan(0, length));

		var newReadPos = (hdr.ReadPosition + 4 + length) % DataCapacity;
		WriteInt32(headerOffset + 4, newReadPos);

		OnDataChanged();
		return true;
	}

	public bool WaitForPendingReads(TimeSpan timeout)
	{
		return ReceiveEvent?.WaitOne(timeout) ?? false;
	}

	protected virtual void Dispose(bool disposing)
	{
		CancellationTokenSource?.Cancel();
		CancellationTokenSource?.Dispose();
	}

	protected virtual void OnDataChanged()
	{
		DataChanged?.Invoke(this, EventArgs.Empty);
	}

	protected abstract void ReadBytes(long offset, Span<byte> destination);

	protected QueueHeader ReadHeader(int offset)
	{
		return new QueueHeader
		{
			WritePosition = ReadInt32(offset),
			ReadPosition = ReadInt32(offset + 4)
		};
	}

	protected abstract int ReadInt32(long offset);

	protected void ReadWithWrap(long baseOffset, int startPos, int capacity, Span<byte> dest)
	{
		var pos = startPos % capacity;
		var first = Math.Min(dest.Length, capacity - pos);
		ReadBytes(baseOffset + pos, dest[..first]);
		if (first < dest.Length)
		{
			ReadBytes(baseOffset, dest[first..]);
		}
	}

	protected virtual async Task ReceiveLoopAsync(CancellationToken token)
	{
		var buffer = new byte[DataCapacity];
		var lengthBuffer = new byte[4];

		while (!token.IsCancellationRequested)
		{
			try
			{
				// Wait for notification (or timeout)
				if (ReceiveEvent?.WaitOne(100) != true)
				{
					continue;
				}

				lock (this)
				{
					// Drain ALL pending messages, then go back to waiting
					while (!token.IsCancellationRequested
							&& TryToRead(buffer, lengthBuffer))
					{
						OnDataChanged();
					}
				}
			}
			catch (Exception)
				when (!token.IsCancellationRequested)
			{
				await Task.Delay(100, token);
			}
		}
	}

	protected void ResetHeader(int offset)
	{
		// Write Position, Read Position
		WriteInt32(offset, 0);
		WriteInt32(offset + 4, 0);
	}

	protected abstract void WriteBytes(long offset, ReadOnlySpan<byte> source);

	protected abstract void WriteInt32(long offset, int value);

	protected void WriteWithWrap(long baseOffset, int startPos, int capacity, ReadOnlySpan<byte> src)
	{
		var pos = startPos % capacity;
		var first = Math.Min(src.Length, capacity - pos);
		WriteBytes(baseOffset + pos, src[..first]);
		if (first < src.Length)
		{
			WriteBytes(baseOffset, src[first..]);
		}
	}

	#endregion

	#region Events

	public event EventHandler DataChanged;

	#endregion

	#region Structures

	/// <summary>
	/// 0-3    4  Write Position
	/// 4-7    4  Read Position
	/// 8-16   8
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = HeaderSizePerQueue)]
	protected struct QueueHeader
	{
		[FieldOffset(0)]
		public int WritePosition;

		[FieldOffset(4)]
		public int ReadPosition;
	}

	#endregion

	#region Records

	public record QueueState(int ReadPos, int WritePos, int Capacity, ReadOnlyMemory<byte> Data);

	#endregion
}