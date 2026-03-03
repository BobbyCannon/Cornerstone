#region References

using System;

#endregion

namespace Cornerstone.Serialization;

/// <summary>
/// A binary serializer.
/// </summary>
public class SpeedyPack
{
	#region Methods

	public static byte[] Pack(IPackable value)
	{
		return SpeedyPacket.Pack(value).ToArray();
	}

	public static T Unpack<T>(byte[] pack)
	{
		return (T) Unpack(pack, typeof(T));
	}

	public static object Unpack(byte[] pack, Type type)
	{
		return SpeedyPacket.Unpack(pack, type);
	}

	#endregion
}