#region References

#if ANDROID || IOS || BROWSER
using Microsoft.Maui.Devices;
#endif
#if IOS
using Foundation;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Cornerstone.Data.Bytes;
using Cornerstone.Reflection;
using Cornerstone.Sync;
#if WINDOWS
using Cornerstone.Platforms.Windows.Internal;
#endif

#endregion

namespace Cornerstone.Runtime;

/// <summary>
/// Gets information about the current runtime.
/// </summary>
[SourceReflection]
[DependencyInjected]
[DependencyInjected(typeof(IRuntimeInformation))]
public class RuntimeInformation : CornerstoneObject, IRuntimeInformation
{
	#region Fields

	private Assembly _applicationAssembly;
	private readonly SortedDictionary<string, object> _cache;
	private static readonly SortedDictionary<string, object> _platformOverrides;
	private readonly Stopwatch _runtimeTimer;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates an instance of the runtime information.
	/// </summary>
	public RuntimeInformation()
	{
		_cache = new SortedDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		_runtimeTimer = Stopwatch.StartNew();
	}

	static RuntimeInformation()
	{
		_platformOverrides = new SortedDictionary<string, object>();
	}

	#endregion

	#region Properties

	public Bitness ApplicationBitness => GetOrCache(nameof(ApplicationBitness), GetApplicationBitness);

	public string ApplicationDataLocation => GetOrCache(nameof(ApplicationDataLocation), GetApplicationDataLocation);

	public string ApplicationFileName => GetOrCache(nameof(ApplicationFileName), GetApplicationFileName);

	public string ApplicationFilePath => GetOrCache(nameof(ApplicationFilePath), GetApplicationFilePath);

	public bool ApplicationIsDevelopmentBuild => GetOrCache(nameof(ApplicationIsDevelopmentBuild), GetApplicationIsDevelopmentBuild);

	public bool ApplicationIsElevated => GetOrCache(nameof(ApplicationIsElevated), GetApplicationIsElevated);

	public bool ApplicationIsLoaded => GetOrCache(nameof(ApplicationIsLoaded), () => false);

	public bool ApplicationIsNativeBuild => GetOrCache(nameof(ApplicationIsNativeBuild), GetApplicationIsNativeBuild);

	public bool ApplicationIsShuttingDown => GetOrCache(nameof(ApplicationIsShuttingDown), () => false);

	public string ApplicationLocation => GetOrCache(nameof(ApplicationLocation), GetApplicationLocation);

	public string ApplicationName => GetOrCache(nameof(ApplicationName), GetApplicationName);

	public TimeSpan ApplicationRuntime => _runtimeTimer.Elapsed;

	public TimeSpan ApplicationStartup => GetOrCache(nameof(ApplicationStartup), () => TimeSpan.Zero);

	public Version ApplicationVersion => GetOrCache(nameof(ApplicationVersion), GetApplicationVersion);

	public Version AvaloniaRuntimeVersion => GetOrCache(nameof(AvaloniaRuntimeVersion), GetAvaloniaRuntimeVersion);

	[Browsable(false)]
	public int Count => _cache.Count;

	public int DeviceDisplayRefreshRate => GetOrCache(nameof(DeviceDisplayRefreshRate), GetDeviceDisplayRefreshRate);

	public Size DeviceDisplaySize => GetOrCache(nameof(DeviceDisplaySize), GetDeviceDisplaySize);

	public string DeviceId => GetOrCache(nameof(DeviceId), GetDeviceId);

	public string DeviceManufacturer => GetOrCache(nameof(DeviceManufacturer), GetDeviceManufacturer);

	public ByteSize DeviceMemory => GetOrCache(nameof(DeviceMemory), GetDeviceMemory);

	public string DeviceModel => GetOrCache(nameof(DeviceModel), GetDeviceModel);

	public string DeviceName => GetOrCache(nameof(DeviceName), GetDeviceName);

	public DevicePlatform DevicePlatform => GetOrCache(nameof(DevicePlatform), GetDevicePlatform);

	public Bitness DevicePlatformBitness => GetOrCache(nameof(DevicePlatformBitness), GetDevicePlatformBitness);

	public Version DevicePlatformVersion => GetOrCache(nameof(DevicePlatformVersion), GetDevicePlatformVersion);

	public DeviceType DeviceType => GetOrCache(nameof(DeviceType), GetDeviceType);

	public Version DotNetRuntimeVersion => GetOrCache(nameof(DotNetRuntimeVersion), GetDotNetRuntimeVersion);

	public object this[string key] => _cache[key];

	[Browsable(false)]
	public IEnumerable<string> Keys => _cache.Keys;

	[Browsable(false)]
	public IReadOnlyDictionary<string, object> PlatformOverrides => new ReadOnlyDictionary<string, object>(_platformOverrides);

	[Browsable(false)]
	public IEnumerable<object> Values => _cache.Values;

	#endregion

	#region Methods

	/// <summary>
	/// Freeze <see cref="ApplicationStartup" />. Prefer <see cref="StartLifecycle" />; this remains for callers.
	/// </summary>
	public void CompleteStartup()
	{
		StartLifecycle();
	}

	public bool ContainsKey(string key)
	{
		return _cache.ContainsKey(key);
	}

	public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
	{
		return _cache.GetEnumerator();
	}

	/// <summary>
	/// Bind the application assembly and run Initialize + Load (host bootstrap convenience).
	/// </summary>
	public void Initialize(Assembly assembly)
	{
		SetApplicationAssembly(assembly);

		if (!IsLifecycleInitialized())
		{
			InitializeLifecycle();
		}

		if (!IsLifecycleLoaded())
		{
			LoadLifecycle();
		}
	}

	/// <inheritdoc />
	public override void LoadLifecycle()
	{
		// Reset first so ApplicationIsLoaded is not wiped from the cache after we set it.
		ResetCache();
		SetOverride(nameof(ApplicationIsLoaded), true);
		Refresh();
		base.LoadLifecycle();
	}

	/// <summary>
	/// Bind the entry assembly used for application name, version, and related paths.
	/// </summary>
	public void SetApplicationAssembly(Assembly assembly)
	{
		_applicationAssembly ??= assembly;
	}

	/// <inheritdoc />
	public override void StartLifecycle()
	{
		if (ApplicationStartup == TimeSpan.Zero)
		{
			// Instance cache only — not a process-wide platform override.
			SetOverride(nameof(ApplicationStartup), _runtimeTimer.Elapsed);
		}

		base.StartLifecycle();
	}

	/// <summary>
	/// Mark the application as shutting down. Prefer <see cref="StopLifecycle" />; this remains for callers.
	/// </summary>
	public void StartShutdown()
	{
		StopLifecycle();
	}

	/// <inheritdoc />
	public override void StopLifecycle()
	{
		// Instance cache only — not a process-wide platform override.
		SetOverride(nameof(ApplicationIsShuttingDown), true);
		base.StopLifecycle();
	}

	/// <inheritdoc />
	public override void UninitializeLifecycle()
	{
		_applicationAssembly = null;
		base.UninitializeLifecycle();
	}

	/// <inheritdoc />
	public override void UnloadLifecycle()
	{
		SetOverride(nameof(ApplicationIsLoaded), false);
		base.UnloadLifecycle();
	}

	/// <summary>
	/// Loads all properties.
	/// </summary>
	public virtual void Refresh()
	{
		_ = ApplicationBitness;
		_ = ApplicationDataLocation;
		_ = ApplicationFileName;
		_ = ApplicationFilePath;
		_ = ApplicationIsDevelopmentBuild;
		_ = ApplicationIsElevated;
		_ = ApplicationIsLoaded;
		_ = ApplicationIsNativeBuild;
		_ = ApplicationIsShuttingDown;
		_ = ApplicationLocation;
		_ = ApplicationName;
		_ = ApplicationVersion;
		_ = AvaloniaRuntimeVersion;
		_ = DeviceDisplayRefreshRate;
		_ = DeviceDisplaySize;
		_ = DeviceId;
		_ = DeviceManufacturer;
		_ = DeviceMemory;
		_ = DeviceModel;
		_ = DeviceName;
		_ = DevicePlatform;
		_ = DevicePlatformBitness;
		_ = DevicePlatformVersion;
		_ = DeviceType;
		_ = DotNetRuntimeVersion;
	}

	/// <summary>
	/// Reset the cache.
	/// </summary>
	public void ResetCache()
	{
		_cache.Clear();

		if (ApplicationIsDevelopmentBuild
			&& (DevicePlatform == DevicePlatform.Windows))
		{
			SetOverride(nameof(ApplicationName), ApplicationName + ".Development");
		}
	}

	/// <summary>
	/// Reset the cache.
	/// </summary>
	public void ResetCache(string name)
	{
		_cache.Remove(name, out _);
	}

	/// <summary>
	/// Set an override for the value.
	/// </summary>
	/// <returns> </returns>
	public void SetOverride<T>(string name, T value)
	{
		if (!_cache.TryAdd(name, value))
		{
			_cache[name] = value;
		}
	}

	/// <summary>
	/// Set a global override for the value.
	/// </summary>
	public void SetPlatformOverride<T>(string name, T value)
	{
		if (!_platformOverrides.TryAdd(name, value))
		{
			_platformOverrides[name] = value;
		}

		SetOverride(name, value);
	}

	public override string ToString()
	{
		var response = new StringBuilder();

		Refresh();

		foreach (var cache in _cache)
		{
			response.AppendLine($"{cache.Key}: {cache.Value}");
		}

		return response.ToString();
	}

	public bool TryGetValue(string key, out object value)
	{
		return _cache.TryGetValue(key, out value);
	}

	/// <summary>
	/// The bitness of the application.
	/// </summary>
	protected Bitness GetApplicationBitness()
	{
		return Environment.Is64BitProcess ? Bitness.X64 : Bitness.X86;
	}

	/// <summary>
	/// The data location of the application.
	/// </summary>
	protected string GetApplicationDataLocation()
	{
		var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		return DevicePlatform == DevicePlatform.Windows

			// C:\Users\[User]\AppData\Local + ApplicationName
			? Path.Combine(localAppData, ApplicationName)
			: localAppData;
	}

	/// <summary>
	/// The file name of the application.
	/// </summary>
	protected string GetApplicationFileName()
	{
		return Path.GetFileName(GetApplicationFilePath());
	}

	/// <summary>
	/// The file path of the application.
	/// </summary>
	protected string GetApplicationFilePath()
	{
		// This is to support PublishSingleFile because Location (above) will be empty
		var path = Path.Combine(AppContext.BaseDirectory, GetApplicationName() + ".exe");

		if (path.EndsWith(".dll"))
		{
			path = path.Replace(".dll", ".exe");
		}

		return path;
	}

	/// <summary>
	/// Get flag indicating if the application is a development build.
	/// </summary>
	protected bool GetApplicationIsDevelopmentBuild()
	{
		#if DEBUG
		return true;
		#else
		return false;
		#endif
	}

	/// <summary>
	/// The elevated status of an application.
	/// </summary>
	protected bool GetApplicationIsElevated()
	{
		// Only makes sense to check on Windows
		return (DevicePlatform == DevicePlatform.Windows)
			&& Environment.IsPrivilegedProcess;
	}

	/// <summary>
	/// Get flag indicating if the application is a native build.
	/// </summary>
	protected bool GetApplicationIsNativeBuild()
	{
		return !RuntimeFeature.IsDynamicCodeSupported;
	}

	/// <summary>
	/// The location of the application.
	/// </summary>
	protected string GetApplicationLocation()
	{
		// Trim trailing directory separators for cleanliness (common practice)
		var response = AppContext.BaseDirectory;
		if (response.StartsWith("file:\\"))
		{
			response = response[6..];
		}
		return response.TrimEnd(
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		);
	}

	/// <summary>
	/// The name of the application.
	/// </summary>
	protected string GetApplicationName()
	{
		return _applicationAssembly?.GetName().Name ?? string.Empty;
	}

	/// <summary>
	/// The version of the application.
	/// </summary>
	protected Version GetApplicationVersion()
	{
		return _applicationAssembly?.GetName().Version ?? new Version(1,2,3,4);
	}

	/// <summary>
	/// The version of the avalonia runtime version.
	/// </summary>
	protected virtual Version GetAvaloniaRuntimeVersion()
	{
		return new Version(1,2,3,4);
	}

	/// <summary>
	/// The ID of the device.
	/// </summary>
	protected virtual string GetDeviceId()
	{
		#if WINDOWS
		return new DeviceId()
			.AddMachineName()
			.AddUserName()
			.AddMachineGuid()
			.AddSystemUuid()
			.AddMotherboardSerialNumber()
			.AddSystemDriveSerialNumber()
			.ToString();
		#else
		return Runtime.DeviceId.VendorId
			?? new DeviceId()
				.AddMachineName()
				.AddUserName()
				.AddVendorId()
				.ToString();
		#endif
	}

	/// <summary>
	/// The name of the device.
	/// </summary>
	protected virtual string GetDeviceName()
	{
		#if ANDROID || IOS
		return DeviceInfo.Name;
		#else
		return Environment.MachineName;
		#endif
	}

	/// <summary>
	/// The name of the platform.
	/// </summary>
	protected DevicePlatform GetDevicePlatform()
	{
		#if ANDROID
		return DevicePlatform.Android;
		#elif BROWSER
		return DevicePlatform.Browser;
		#elif IOS
		return DevicePlatform.IOS;
		#elif WINDOWS
		return DevicePlatform.Windows;
		#else
		return DevicePlatform.Unknown;
		#endif
	}

	/// <summary>
	/// The bitness of the platform.
	/// </summary>
	protected Bitness GetDevicePlatformBitness()
	{
		return Environment.Is64BitOperatingSystem ? Bitness.X64 : Bitness.X86;
	}

	/// <summary>
	/// The version of the device platform version.
	/// </summary>
	protected Version GetDevicePlatformVersion()
	{
		return Environment.OSVersion.Version;
	}

	/// <summary>
	/// The type of the device.
	/// </summary>
	protected DeviceType GetDeviceType()
	{
		#if ANDROID || IOS || BROWSER
		if (DeviceInfo.Current.Idiom == DeviceIdiom.Tablet)
		{
			return DeviceType.Tablet;
		}
		if (DeviceInfo.Current.Idiom == DeviceIdiom.Phone)
		{
			return DeviceType.Phone;
		}
		if (DeviceInfo.Current.Idiom == DeviceIdiom.Watch)
		{
			return DeviceType.Watch;
		}

		return DeviceType.Desktop;
		#else
		return DeviceType.Desktop;
		#endif
	}

	/// <summary>
	/// The version of the dotnet runtime version.
	/// </summary>
	protected virtual Version GetDotNetRuntimeVersion()
	{
		return Environment.Version;
	}

	/// <summary>
	/// Get or cache the value from the factory.
	/// </summary>
	/// <typeparam name="T"> The type to process. </typeparam>
	/// <param name="name"> The name to represent the value. </param>
	/// <param name="valueFactory"> The factory to create the value. </param>
	/// <returns> The value by the name provided. </returns>
	protected T GetOrCache<T>(string name, Func<T> valueFactory)
	{
		// Check platform-specific override first (highest priority)
		if (_platformOverrides.TryGetValue(name, out var overrideValue))
		{
			// If override exists → use it, but still cache it if not already cached
			if (_cache.TryGetValue(name, out var existing))
			{
				return (T) existing; // already cached → fast path
			}

			// Cache the override value so next call is faster
			_cache[name] = overrideValue;
			return (T) overrideValue;
		}

		// No override so normal cache-or-create pattern
		if (_cache.TryGetValue(name, out var value))
		{
			return (T) value;
		}

		// Not found create, cache, return
		var newValue = valueFactory();
		_cache[name] = newValue;
		return newValue;
	}

	private int GetDeviceDisplayRefreshRate()
	{
		return 0;
	}

	private Size GetDeviceDisplaySize()
	{
		return Size.Empty;
	}

	private string GetDeviceManufacturer()
	{
		#if ANDROID || IOS
		return DeviceInfo.Manufacturer;
		#elif WINDOWS
		return new DeviceManufacturerRegistryComponent().GetValue()
			?? new DeviceManufacturerWmiComponent().GetValue();
		#else
		return string.Empty;
		#endif
	}

	private ByteSize GetDeviceMemory()
	{
		#if IOS
		return ByteSize.FromBytes((long) NSProcessInfo.ProcessInfo.PhysicalMemory);
		#else
		return ByteSize.FromBytes(0);
		#endif
	}

	private string GetDeviceModel()
	{
		#if ANDROID || IOS
		return DeviceInfo.Model;
		#elif WINDOWS
		return new DeviceModelRegistryComponent().GetValue()
			?? new DeviceModelWmiComponent().GetValue();
		#else
		return string.Empty;
		#endif
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}

/// <summary>
/// Gets information about the current runtime.
/// </summary>
public interface IRuntimeInformation : IReadOnlyDictionary<string, object>, ISyncClientDetails
{
	#region Properties

	/// <summary>
	/// The bitness of the application.
	/// </summary>
	Bitness ApplicationBitness { get; }

	/// <summary>
	/// The location of the application.
	/// </summary>
	string ApplicationDataLocation { get; }

	/// <summary>
	/// The file name of the application.
	/// </summary>
	string ApplicationFileName { get; }

	/// <summary>
	/// The file path of the application.
	/// </summary>
	string ApplicationFilePath { get; }

	/// <summary>
	/// Flag indicating if the application is a development build.
	/// </summary>
	bool ApplicationIsDevelopmentBuild { get; }

	/// <summary>
	/// Flag indicating if the application is elevated.
	/// </summary>
	bool ApplicationIsElevated { get; }

	/// <summary>
	/// The flag to track when the application has been loaded.
	/// </summary>
	bool ApplicationIsLoaded { get; }

	/// <summary>
	/// Flag indicating if the application is a native (AOT) build.
	/// </summary>
	bool ApplicationIsNativeBuild { get; }

	/// <summary>
	/// Flag indicating if the application is shutting down.
	/// </summary>
	bool ApplicationIsShuttingDown { get; }

	/// <summary>
	/// The location (directory) of the application.
	/// </summary>
	string ApplicationLocation { get; }

	/// <summary>
	/// The time it took for the application to startup.
	/// </summary>
	TimeSpan ApplicationStartup { get; }

	/// <summary>
	/// The Avalonia runtime version.
	/// </summary>
	Version AvaloniaRuntimeVersion { get; }

	/// <summary>
	/// The primary display refresh rate in hertz. Zero when unknown.
	/// </summary>
	int DeviceDisplayRefreshRate { get; }

	/// <summary>
	/// The display size of the device.
	/// </summary>
	Size DeviceDisplaySize { get; }

	/// <summary>
	/// The name of the device manufacturer.
	/// </summary>
	string DeviceManufacturer { get; }

	/// <summary>
	/// The size of the device's memory.
	/// </summary>
	ByteSize DeviceMemory { get; }

	/// <summary>
	/// The model of the device.
	/// </summary>
	string DeviceModel { get; }

	/// <summary>
	/// The bitness of the platform.
	/// </summary>
	Bitness DevicePlatformBitness { get; }

	/// <summary>
	/// The DotNet runtime version.
	/// </summary>
	Version DotNetRuntimeVersion { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Mark the runtime as startup completed.
	/// </summary>
	public void CompleteStartup();

	/// <summary>
	/// Mark the runtime as shutting down.
	/// </summary>
	public void StartShutdown();

	#endregion
}