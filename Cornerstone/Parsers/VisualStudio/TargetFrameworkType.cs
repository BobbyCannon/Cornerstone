#region References

using Cornerstone.Attributes;

#endregion

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Cornerstone.Parsers.VisualStudio;

/// <summary>
/// Represents the project target type.
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/en-us/dotNet/standard/frameworks
/// </remarks>
public enum TargetFrameworkType
{
	[TargetFramework(Moniker = "Unknown")]
	Unknown,

	[TargetFramework(Moniker = "netstandard1.0", Version = "1.0", IsNetStandard = true, Parent = NetStandard11)]
	NetStandard10,

	[TargetFramework(Moniker = "netstandard1.1", Version = "1.1", IsNetStandard = true, Parent = NetStandard12)]
	NetStandard11,

	[TargetFramework(Moniker = "netstandard1.2", Version = "1.2", IsNetStandard = true, Parent = NetStandard13)]
	NetStandard12,

	[TargetFramework(Moniker = "netstandard1.3", Version = "1.3", IsNetStandard = true, Parent = NetStandard14)]
	NetStandard13,

	[TargetFramework(Moniker = "netstandard1.4", Version = "1.4", IsNetStandard = true, Parent = NetStandard15)]
	NetStandard14,

	[TargetFramework(Moniker = "netstandard1.5", Version = "1.5", IsNetStandard = true, Parent = NetStandard16)]
	NetStandard15,

	[TargetFramework(Moniker = "netstandard1.6", Version = "1.6", IsNetStandard = true, Parent = NetStandard20)]
	NetStandard16,

	[TargetFramework(Moniker = "netstandard2.0", Version = "2.0", IsNetStandard = true, Parent = NetStandard21)]
	NetStandard20,

	[TargetFramework(Moniker = "net11", Version = "1.1", NetStandard = NetStandard16, IsNetFramework = true)]
	Net11,

	[TargetFramework(Moniker = "net20", Version = "2.0", NetStandard = NetStandard21, IsNetFramework = true)]
	Net20,

	[TargetFramework(Moniker = "net35", Version = "3.5", NetStandard = NetStandard21, IsNetFramework = true)]
	Net35,

	[TargetFramework(Moniker = "net40", Version = "4.0", NetStandard = NetStandard21, IsNetFramework = true)]
	Net40,

	[TargetFramework(Moniker = "net403", Version = "4.0.3", NetStandard = NetStandard21, IsNetFramework = true)]
	Net403,

	[TargetFramework(Moniker = "net45", Version = "4.5", NetStandard = NetStandard12, IsNetFramework = true)]
	Net45,

	[TargetFramework(Moniker = "net451", Version = "4.5.1", NetStandard = NetStandard12, IsNetFramework = true)]
	Net451,

	[TargetFramework(Moniker = "net452", Version = "4.5.2", NetStandard = NetStandard12, IsNetFramework = true)]
	Net452,

	[TargetFramework(Moniker = "net46", Version = "4.6", NetStandard = NetStandard13, IsNetFramework = true)]
	Net46,

	[TargetFramework(Moniker = "net461", Version = "4.6.1", NetStandard = NetStandard20, IsNetFramework = true)]
	Net461,

	[TargetFramework(Moniker = "net462", Version = "4.6.2", NetStandard = NetStandard20, IsNetFramework = true)]
	Net462,

	[TargetFramework(Moniker = "net463", Version = "4.6.3", NetStandard = NetStandard20, IsNetFramework = true)]
	Net463,

	[TargetFramework(Moniker = "net47", Version = "4.7", NetStandard = NetStandard20, IsNetFramework = true)]
	Net47,

	[TargetFramework(Moniker = "net471", Version = "4.7.1", NetStandard = NetStandard20, IsNetFramework = true)]
	Net471,

	[TargetFramework(Moniker = "net472", Version = "4.7.2", NetStandard = NetStandard20, IsNetFramework = true)]
	Net472,

	[TargetFramework(Moniker = "net48", Version = "4.8", NetStandard = NetStandard20, IsNetFramework = true)]
	Net48,

	[TargetFramework(Moniker = "uap", Version = "10.0", NetStandard = NetStandard20, IsUniversal = true)]
	Uap,

	[TargetFramework(Moniker = "uap10.0", Version = "10.0", NetStandard = NetStandard20, IsUniversal = true)]
	Uap10,

	[TargetFramework(Moniker = "netcore", Version = "1.0", NetStandard = NetStandard16, IsUniversal = true)]
	NetCore,

	[TargetFramework(Moniker = "netcore45", Version = "4.5", NetStandard = NetStandard11, IsUniversal = true)]
	NetCore45,

	[TargetFramework(Moniker = "netcore451", Version = "4.5.1", NetStandard = NetStandard12, IsUniversal = true)]
	NetCore451,

	[TargetFramework(Moniker = "netcore50", Version = "5.0", NetStandard = NetStandard14, IsUniversal = true, Parent = Uap10)]
	NetCore50,

	[TargetFramework(Moniker = "netcoreapp1.0", Version = "1.0", NetStandard = NetStandard16)]
	NetCoreApp10,

	[TargetFramework(Moniker = "netcoreapp1.1", Version = "1.1", NetStandard = NetStandard16)]
	NetCoreApp11,

	[TargetFramework(Moniker = "netcoreapp2.0", Version = "2.0", NetStandard = NetStandard20)]
	NetCoreApp20,

	[TargetFramework(Moniker = "netcoreapp2.1", Version = "2.1", NetStandard = NetStandard20)]
	NetCoreApp21,

	[TargetFramework(Moniker = "netcoreapp2.2", Version = "2.2", NetStandard = NetStandard20)]
	NetCoreApp22,

	[TargetFramework(Moniker = "netstandard2.1", Version = "2.1", IsNetStandard = true)]
	NetStandard21,

	[TargetFramework(Moniker = "netcoreapp3.0", Version = "3.0", NetStandard = NetStandard21)]
	NetCoreApp30,

	[TargetFramework(Moniker = "netcoreapp3.1", Version = "3.1", NetStandard = NetStandard21)]
	NetCoreApp31,

	[TargetFramework(Moniker = "net5.0", Version = "5.0", NetStandard = NetStandard21, Platform = Net5, Parent = Net6)]
	Net5,

	[TargetFramework(Moniker = "net5.0-windows", Version = "5.0", OperatingSystem = "windows", NetStandard = NetStandard21, Platform = Net5, Parent = Net6Windows)]
	Net5Windows,

	[TargetFramework(Moniker = "net6.0", Version = "6.0", NetStandard = NetStandard21, Platform = Net6, Parent = Net7)]
	Net6,

	[TargetFramework(Moniker = "net6.0-android", Version = "6.0", OperatingSystem = "android", NetStandard = NetStandard21, Platform = Net6, Parent = Net7Android)]
	Net6Android,

	[TargetFramework(Moniker = "net6.0-ios", Version = "6.0", OperatingSystem = "ios", NetStandard = NetStandard21, Platform = Net6, Parent = Net7Ios)]
	Net6Ios,

	[TargetFramework(Moniker = "net6.0-maccatalyst", Version = "6.0", OperatingSystem = "maccatalyst", NetStandard = NetStandard21, Platform = Net6, Parent = Net7MacCatalyst)]
	Net6MacCatalyst,

	[TargetFramework(Moniker = "net6.0-macos", Version = "6.0", OperatingSystem = "macos", NetStandard = NetStandard21, Platform = Net6, Parent = Net7MacOs)]
	Net6MacOs,

	[TargetFramework(Moniker = "net6.0-tvos", Version = "6.0", OperatingSystem = "tvos", NetStandard = NetStandard21, Platform = Net6, Parent = Net7TvOs)]
	Net6TvOs,

	[TargetFramework(Moniker = "net6.0-windows", Version = "6.0", OperatingSystem = "windows", NetStandard = NetStandard21, Platform = Net6, Parent = Net7Windows)]
	Net6Windows,

	[TargetFramework(Moniker = "net7.0", Version = "7.0", NetStandard = NetStandard21, Platform = Net7, Parent = Net8)]
	Net7,

	[TargetFramework(Moniker = "net7.0-android", Version = "7.0", OperatingSystem = "android", NetStandard = NetStandard21, Platform = Net7, Parent = Net8Android)]
	Net7Android,

	[TargetFramework(Moniker = "net7.0-ios", Version = "7.0", OperatingSystem = "ios", NetStandard = NetStandard21, Platform = Net7, Parent = Net8Ios)]
	Net7Ios,

	[TargetFramework(Moniker = "net7.0-maccatalyst", Version = "7.0", OperatingSystem = "maccatalyst", NetStandard = NetStandard21, Platform = Net7, Parent = Net8MacCatalyst)]
	Net7MacCatalyst,

	[TargetFramework(Moniker = "net7.0-macos", Version = "7.0", OperatingSystem = "macos", NetStandard = NetStandard21, Platform = Net7, Parent = Net8MacOs)]
	Net7MacOs,

	[TargetFramework(Moniker = "net7.0-tvos", Version = "7.0", OperatingSystem = "tvos", NetStandard = NetStandard21, Platform = Net7, Parent = Net8TvOs)]
	Net7TvOs,

	[TargetFramework(Moniker = "net7.0-windows", Version = "7.0", OperatingSystem = "windows", NetStandard = NetStandard21, Platform = Net7, Parent = Net8Windows)]
	Net7Windows,

	[TargetFramework(Moniker = "net8.0", Version = "8.0", NetStandard = NetStandard21, Platform = Net8)]
	Net8,

	[TargetFramework(Moniker = "net8.0-android", Version = "8.0", OperatingSystem = "android", NetStandard = NetStandard21, Platform = Net8)]
	Net8Android,

	[TargetFramework(Moniker = "net8.0-browser", Version = "8.0", OperatingSystem = "browser", NetStandard = NetStandard21, Platform = Net8)]
	Net8Browser,

	[TargetFramework(Moniker = "net8.0-ios", Version = "8.0", OperatingSystem = "ios", NetStandard = NetStandard21, Platform = Net8)]
	Net8Ios,

	[TargetFramework(Moniker = "net8.0-maccatalyst", Version = "8.0", OperatingSystem = "maccatalyst", NetStandard = NetStandard21, Platform = Net8)]
	Net8MacCatalyst,

	[TargetFramework(Moniker = "net8.0-macos", Version = "8.0", OperatingSystem = "macos", NetStandard = NetStandard21, Platform = Net8)]
	Net8MacOs,

	[TargetFramework(Moniker = "net8.0-tvos", Version = "8.0", OperatingSystem = "tvos", NetStandard = NetStandard21, Platform = Net8)]
	Net8TvOs,

	[TargetFramework(Moniker = "net8.0-windows", Version = "8.0", OperatingSystem = "windows", NetStandard = NetStandard21, Platform = Net8)]
	Net8Windows,

	[TargetFramework(Moniker = "MonoAndroid", Version = "MonoAndroid", OperatingSystem = "Android", NetStandard = NetStandard20)]
	MonoAndroid,

	[TargetFramework(Moniker = "Xamarin.iOS", Version = "Xamarin.iOS", OperatingSystem = "iOS", NetStandard = NetStandard20)]
	XamarinIos,

	[TargetFramework(Moniker = "Xamarin.Mac", Version = "Xamarin.Mac", OperatingSystem = "Mac", NetStandard = NetStandard20)]
	XamarinMac,

	[TargetFramework(Moniker = "Xamarin.TVOS", Version = "Xamarin.TVOS", OperatingSystem = "TVOS", NetStandard = NetStandard20)]
	XamarinTvOs,

	[TargetFramework(Moniker = "Xamarin.WatchOS", Version = "Xamarin.WatchOS", OperatingSystem = "WatchOS", NetStandard = NetStandard20)]
	XamarinWatchOs
}