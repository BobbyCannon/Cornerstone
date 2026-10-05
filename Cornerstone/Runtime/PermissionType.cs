#region References

using System.ComponentModel.DataAnnotations;

#endregion

namespace Cornerstone.Runtime;

public enum PermissionType
{
	Unknown = 0,

	[Display(Order = 1)]
	Accelerometer = 1,

	[Display(Order = 2)]
	Camera = 2,

	[Display(Name = "Clipboard Read", Order = 3)]
	ClipboardRead = 3,

	[Display(Name = "Clipboard Write", Order = 4)]
	ClipboardWrite = 4,

	[Display(Order = 5)]
	Gyroscope = 5,

	[Display(Order = 6)]
	Location = 6,

	[Display(Order = 7)]
	Magnetometer = 7,

	[Display(Order = 8)]
	Microphone = 8,

	[Display(Name = "Near Field Communications", ShortName = "NFC", Order = 9)]
	NearFieldCommunications = 12,

	[Display(Order = 10)]
	Notifications = 9,

	[Display(Order = 11)]
	Storage = 10,

	[Display(Order = 12)]
	Video = 11
}