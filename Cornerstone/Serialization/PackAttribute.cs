#region References

using System;

#endregion

namespace Cornerstone.Serialization;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class PackAttribute : CornerstoneAttribute
{
	#region Constructors

	public PackAttribute(byte version, byte offset)
	{
		Version = version;
		Offset = offset;
	}

	#endregion

	#region Properties

	public byte Offset { get; }

	public byte Version { get; }

	#endregion
}