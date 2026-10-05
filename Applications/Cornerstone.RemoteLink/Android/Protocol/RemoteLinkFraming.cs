#region References

using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.RemoteLink.Android.Protocol;

public readonly struct RemoteLinkMessage
{
	#region Constructors

	public RemoteLinkMessage(RemoteLinkProtocol.MessageType type, byte[] payload)
	{
		Type = type;
		Payload = payload ?? [];
	}

	#endregion

	#region Properties

	public byte[] Payload { get; }

	public RemoteLinkProtocol.MessageType Type { get; }

	#endregion
}

/// <summary>
/// Big-endian RLNK frames. Same layout as Protocol.java.
/// </summary>
public static class RemoteLinkFraming
{
	#region Constants

	public const int HeaderLength = 11;

	#endregion

	#region Methods

	public static byte[] Encode(RemoteLinkProtocol.MessageType type, byte[] payload)
	{
		payload ??= [];
		var buffer = new byte[HeaderLength + payload.Length];
		RemoteLinkProtocol.Magic.CopyTo(buffer, 0);
		BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(4, 2), RemoteLinkProtocol.Version);
		buffer[6] = (byte) type;
		BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(7, 4), payload.Length);
		if (payload.Length > 0)
		{
			payload.CopyTo(buffer, HeaderLength);
		}

		return buffer;
	}

	public static byte[] EncodeHello(int width, int height, ushort flags)
	{
		var payload = new byte[6];
		BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(0, 2), (ushort) width);
		BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(2, 2), (ushort) height);
		BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(4, 2), flags);
		return Encode(RemoteLinkProtocol.MessageType.Hello, payload);
	}

	public static byte[] EncodeControl(RemoteLinkProtocol.ControlType control, int x, int y)
	{
		var payload = new byte[5];
		payload[0] = (byte) control;
		BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(1, 2), (ushort) x);
		BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(3, 2), (ushort) y);
		return Encode(RemoteLinkProtocol.MessageType.Control, payload);
	}

	public static byte[] EncodeControlText(string text)
	{
		var utf8 = Encoding.UTF8.GetBytes(text ?? string.Empty);
		var payload = new byte[1 + utf8.Length];
		payload[0] = (byte) RemoteLinkProtocol.ControlType.Text;
		utf8.CopyTo(payload, 1);
		return Encode(RemoteLinkProtocol.MessageType.Control, payload);
	}

	public static async Task<RemoteLinkMessage> ReadAsync(Stream stream, CancellationToken cancellationToken)
	{
		var header = new byte[HeaderLength];
		await ReadExactAsync(stream, header, cancellationToken).ConfigureAwait(false);
		for (var i = 0; i < RemoteLinkProtocol.Magic.Length; i++)
		{
			if (header[i] != RemoteLinkProtocol.Magic[i])
			{
				throw new InvalidDataException("Not a RemoteLink frame.");
			}
		}

		var version = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(4, 2));
		if (version != RemoteLinkProtocol.Version)
		{
			throw new InvalidDataException($"RemoteLink version {version} is not supported.");
		}

		var type = (RemoteLinkProtocol.MessageType) header[6];
		var length = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(7, 4));
		if ((length < 0) || (length > 8 * 1024 * 1024))
		{
			throw new InvalidDataException("RemoteLink payload is too large.");
		}

		var payload = length == 0 ? [] : new byte[length];
		if (length > 0)
		{
			await ReadExactAsync(stream, payload, cancellationToken).ConfigureAwait(false);
		}

		return new RemoteLinkMessage(type, payload);
	}

	public static bool TryReadHello(byte[] payload, out int width, out int height, out ushort flags)
	{
		width = 0;
		height = 0;
		flags = 0;
		if ((payload == null) || (payload.Length < 6))
		{
			return false;
		}

		width = BinaryPrimitives.ReadUInt16BigEndian(payload.AsSpan(0, 2));
		height = BinaryPrimitives.ReadUInt16BigEndian(payload.AsSpan(2, 2));
		flags = BinaryPrimitives.ReadUInt16BigEndian(payload.AsSpan(4, 2));
		return true;
	}

	private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
	{
		var offset = 0;
		while (offset < buffer.Length)
		{
			var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken).ConfigureAwait(false);
			if (read == 0)
			{
				throw new EndOfStreamException();
			}

			offset += read;
		}
	}

	#endregion
}
