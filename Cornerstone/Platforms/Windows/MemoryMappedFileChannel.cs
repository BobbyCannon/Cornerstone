#region References

using Cornerstone.Communications;
using System;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.Platforms.Windows;

public class MemoryMappedFileChannel : Channel
{
	#region Fields

	private MemoryMappedViewAccessor _accessor;
	private MemoryMappedFile _mmf;

	#endregion

	#region Constructors

	public MemoryMappedFileChannel(string name, int totalCapacity)
		: base(totalCapacity)
	{
		Name = name;
	}

	#endregion

	#region Properties

	public string ClientToServerEvent => $"LocalSession/{Name}/ClientToServerReady";
	public string MemoryMapName => $"LocalSession/{Name}";
	public string Name { get; }
	public string ServerToClientEvent => $"LocalSession/{Name}/ServerToClientReady";

	#endregion

	#region Methods

	public override void Connect()
	{
		_mmf = MemoryMappedFile.OpenExisting(MemoryMapName);
		_accessor = _mmf.CreateViewAccessor();

		SendEvent = EventWaitHandle.OpenExisting(ClientToServerEvent);
		ReceiveEvent = EventWaitHandle.OpenExisting(ServerToClientEvent);

		base.Connect();
	}

	public override void Start()
	{
		_mmf = MemoryMappedFile.CreateOrOpen(MemoryMapName, TotalCapacity, MemoryMappedFileAccess.ReadWrite);
		_accessor = _mmf.CreateViewAccessor();

		SendEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ServerToClientEvent);
		ReceiveEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ClientToServerEvent);

		base.Start();
	}

	protected override void Dispose(bool disposing)
	{
		_accessor?.Dispose();
		_mmf?.Dispose();
		base.Dispose(disposing);
	}

	protected override void ReadBytes(long offset, Span<byte> destination)
	{
		var arr = new byte[destination.Length];
		_accessor!.ReadArray(offset, arr, 0, arr.Length);
		arr.CopyTo(destination);
	}

	protected override int ReadInt32(long offset)
	{
		_accessor!.Read(offset, out int value);
		return value;
	}

	protected override void WriteBytes(long offset, ReadOnlySpan<byte> source)
	{
		// tiny overhead – messages ≤ 1 MB
		var arr = source.ToArray();
		_accessor!.WriteArray(offset, arr, 0, arr.Length);
	}

	protected override void WriteInt32(long offset, int value)
	{
		_accessor!.Write(offset, ref value);
	}

	#endregion
}