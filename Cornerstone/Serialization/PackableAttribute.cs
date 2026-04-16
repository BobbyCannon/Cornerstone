#region References

using System;

#endregion

namespace Cornerstone.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class PackableAttribute : CornerstoneAttribute
{
	#region Constructors

	public PackableAttribute(byte version, string[] properties)
	{
		Version = version;
		Properties = properties;
	}

	#endregion

	#region Properties

	public string[] Properties { get; }

	public byte Version { get; }

	#endregion
}