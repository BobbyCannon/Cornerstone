#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Serialization;

#endregion

namespace Cornerstone.Generators.UnitTests.Sample;

[SourceReflection]
public partial class PackableTest : Notifiable
{
	#region Properties

	[Notify]
	[Pack(1, 1)]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool Boolean { get; set; }

	[Notify]
	[Pack(1, 2)]
	[UpdateableAction(UpdateableAction.All)]
	public partial char Character { get; set; }

	[Notify]
	[Pack(1, 3)]
	[UpdateableAction(UpdateableAction.All)]
	public partial DateOnly DateOnly { get; set; }

	[Notify]
	[Pack(1, 4)]
	[UpdateableAction(UpdateableAction.All)]
	public partial DateTime DateTime { get; set; }

	[Notify]
	[Pack(1, 5)]
	[UpdateableAction(UpdateableAction.All)]
	public partial DateTimeOffset DateTimeOffset { get; set; }

	[Notify]
	[Pack(1, 6)]
	[UpdateableAction(UpdateableAction.All)]
	public partial decimal Decimal { get; set; }

	[Notify]
	[Pack(1, 7)]
	[UpdateableAction(UpdateableAction.All)]
	public partial double Double { get; set; }

	[Notify]
	[Pack(1, 8)]
	[UpdateableAction(UpdateableAction.All)]
	public partial float Float { get; set; }

	[Notify]
	[Pack(1, 9)]
	[UpdateableAction(UpdateableAction.All)]
	public partial Guid Guid { get; set; }

	[Notify]
	[Pack(1, 10)]
	[UpdateableAction(UpdateableAction.All)]
	public partial Int128 Int128 { get; set; }

	[Notify]
	[Pack(1, 11)]
	[UpdateableAction(UpdateableAction.All)]
	public partial short Int16 { get; set; }

	[Notify]
	[Pack(1, 12)]
	[UpdateableAction(UpdateableAction.All)]
	public partial int Int32 { get; set; }

	[Notify]
	[Pack(1, 13)]
	[UpdateableAction(UpdateableAction.All)]
	public partial long Int64 { get; set; }

	[Notify]
	[Pack(1, 14)]
	[UpdateableAction(UpdateableAction.All)]
	public partial sbyte Int8 { get; set; }

	[Notify]
	[Pack(1, 15)]
	[UpdateableAction(UpdateableAction.All)]
	public partial nint IntPtr { get; set; }

	[Notify]
	[Pack(1, 16)]
	[UpdateableAction(UpdateableAction.All)]
	public partial string String { get; set; }

	[Notify]
	[Pack(1, 17)]
	[UpdateableAction(UpdateableAction.All)]
	public partial TimeOnly TimeOnly { get; set; }

	[Notify]
	[Pack(1, 18)]
	[UpdateableAction(UpdateableAction.All)]
	public partial TimeSpan TimeSpan { get; set; }

	[Notify]
	[Pack(1, 19)]
	[UpdateableAction(UpdateableAction.All)]
	public partial UInt128 UInt128 { get; set; }

	[Notify]
	[Pack(1, 20)]
	[UpdateableAction(UpdateableAction.All)]
	public partial ushort UInt16 { get; set; }

	[Notify]
	[Pack(1, 21)]
	[UpdateableAction(UpdateableAction.All)]
	public partial uint UInt32 { get; set; }

	[Notify]
	[Pack(1, 22)]
	[UpdateableAction(UpdateableAction.All)]
	public partial ulong UInt64 { get; set; }

	[Notify]
	[Pack(1, 23)]
	[UpdateableAction(UpdateableAction.All)]
	public partial byte UInt8 { get; set; }

	[Notify]
	[Pack(1, 24)]
	[UpdateableAction(UpdateableAction.All)]
	public partial nuint UIntPtr { get; set; }

	[Notify]
	[Pack(1, 25)]
	[UpdateableAction(UpdateableAction.All)]
	public partial Version Version { get; set; }

	#endregion
}