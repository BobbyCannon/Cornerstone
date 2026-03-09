#region References

using System;
using System.Runtime.InteropServices;
using System.Threading;

#endregion

namespace Cornerstone.Communications;

public class MemoryChannel : Channel
{
	#region Constructors

	public MemoryChannel() : this(new byte[128])
	{
	}

	public MemoryChannel(byte[] buffer)
		: base(buffer.Length)
	{
		Buffer = buffer;
		IsServer = true;
		ClientToServer = new(false);
		ServerToClient = new(false);
	}

	public MemoryChannel(MemoryChannel server)
		: base(server.Buffer.Length)
	{
		Buffer = server.Buffer;
		IsServer = false;
		ClientToServer = server.ClientToServer;
		ServerToClient = server.ServerToClient;
	}

	#endregion

	#region Properties

	public byte[] Buffer { get; }

	public AutoResetEvent ClientToServer { get; }

	public AutoResetEvent ServerToClient { get; }

	#endregion

	#region Methods

	public override void Connect()
	{
		SendEvent = ClientToServer;
		ReceiveEvent = ServerToClient;
		base.Connect();
	}

	public override void Start()
	{
		SendEvent = ServerToClient;
		ReceiveEvent = ClientToServer;
		base.Start();
	}

	protected override void Dispose(bool disposing)
	{
		if (IsServer)
		{
			ServerToClient.Dispose();
			ClientToServer.Dispose();
		}
		base.Dispose(disposing);
	}

	protected override void ReadBytes(long offset, Span<byte> destination)
	{
		Buffer.AsSpan((int) offset, destination.Length).CopyTo(destination);
	}

	protected override int ReadInt32(long offset)
	{
		return MemoryMarshal.Read<int>(Buffer.AsSpan((int) offset));
	}

	protected override void WriteBytes(long offset, ReadOnlySpan<byte> source)
	{
		source.CopyTo(Buffer.AsSpan((int) offset));
	}

	protected override void WriteInt32(long offset, int value)
	{
		MemoryMarshal.Write(Buffer.AsSpan((int) offset), value);
	}

	#endregion
}