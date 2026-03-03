#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Cornerstone.Data;
#if ANDROID || IOS || BROWSER
using Microsoft.Maui.Devices;
#endif

#endregion

namespace Cornerstone.Runtime;

/// <summary>
/// Gets information about the current runtime.
/// </summary>
public class RuntimeInformation : Notifiable, IRuntimeInformation
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
		_cache = new SortedDictionary<string, object>();
		_runtimeTimer = Stopwatch.StartNew();
	}

	static RuntimeInformation()
	{
		_platformOverrides = new SortedDictionary<string, object>();
	}

	#endregion

	#region Properties

	public string ApplicationDataLocation => GetOrCache(nameof(ApplicationDataLocation), GetApplicationDataLocation);

	public bool ApplicationIsDevelopmentBuild => GetOrCache(nameof(ApplicationIsDevelopmentBuild), GetApplicationIsDevelopmentBuild);

	public bool ApplicationIsElevated => GetOrCache(nameof(ApplicationIsElevated), GetApplicationIsElevated);

	public bool ApplicationIsLoaded { get; private set; }

	public bool ApplicationIsNativeBuild => GetOrCache(nameof(ApplicationIsNativeBuild), GetApplicationIsNativeBuild);

	public bool ApplicationIsShuttingDown => GetOrCache(nameof(ApplicationIsShuttingDown), () => false);

	public string ApplicationLocation => GetOrCache(nameof(ApplicationLocation), GetApplicationLocation);

	public string ApplicationName => GetOrCache(nameof(ApplicationName), GetApplicationName);

	public TimeSpan ApplicationRuntime => _runtimeTimer.Elapsed;

	public TimeSpan ApplicationStartup => GetOrCache(nameof(ApplicationStartup), () => TimeSpan.Zero);

	public Version ApplicationVersion => GetOrCache(nameof(ApplicationVersion), GetApplicationVersion);

	[Browsable(false)]
	public int Count => _cache.Count;

	public string DeviceName => GetOrCache(nameof(DeviceName), GetDeviceName);

	public DevicePlatform DevicePlatform => GetOrCache(nameof(DevicePlatform), GetDevicePlatform);

	public DeviceType DeviceType => GetOrCache(nameof(DeviceType), GetDeviceType);
	public object this[string key] => _cache[key];

	[Browsable(false)]
	public IEnumerable<string> Keys => _cache.Keys;

	[Browsable(false)]
	public IReadOnlyDictionary<string, object> PlatformOverrides => new ReadOnlyDictionary<string, object>(_platformOverrides);

	[Browsable(false)]
	public IEnumerable<object> Values => _cache.Values;

	#endregion

	#region Methods

	public void CompleteStartup()
	{
		if (ApplicationStartup == TimeSpan.Zero)
		{
			SetPlatformOverride(nameof(ApplicationStartup), _runtimeTimer.Elapsed);
		}
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
	/// Initialize the runtime information state. Ex. Ensure the paths exists.
	/// </summary>
	public void Initialize(Assembly assembly)
	{
		if (_applicationAssembly != null)
		{
			return;
		}

		_applicationAssembly = assembly;
		ApplicationIsLoaded = true;

		ResetCache();
		Refresh();
	}

	/// <summary>
	/// Loads all properties.
	/// </summary>
	public virtual void Refresh()
	{
		//_ = ApplicationBitness;
		_ = ApplicationDataLocation;

		//_ = ApplicationFileName;
		//_ = ApplicationFilePath;
		_ = ApplicationIsDevelopmentBuild;
		_ = ApplicationIsElevated;

		//_ = ApplicationIsLoaded;
		_ = ApplicationIsNativeBuild;

		//_ = ApplicationIsShuttingDown;
		_ = ApplicationLocation;
		_ = ApplicationName;
		_ = ApplicationVersion;

		//_ = AvaloniaRuntimeVersion;
		//_ = DeviceDisplaySize;
		//_ = DeviceId;
		//_ = DeviceManufacturer;
		//_ = DeviceMemory;
		//_ = DeviceModel;
		_ = DeviceName;
		_ = DevicePlatform;

		//_ = DevicePlatformBitness;
		//_ = DevicePlatformVersion;
		_ = DeviceType;

		//_ = DotNetRuntimeVersion;
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

	public void StartShutdown()
	{
		SetPlatformOverride(nameof(ApplicationIsShuttingDown), true);
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
		return _applicationAssembly?.GetName().Version ?? new Version();
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
		#if (ANDROID)
		return DevicePlatform.Android;
		#elif (BROWSER)
		return DevicePlatform.Browser;
		#elif (IOS)
		return DevicePlatform.IOS;
		#elif (WINDOWS)
		return DevicePlatform.Windows;
		#else
		return DevicePlatform.Unknown;
		#endif
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

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}

/// <summary>
/// Gets information about the current runtime.
/// </summary>
public interface IRuntimeInformation : IReadOnlyDictionary<string, object>
{
	#region Properties

	/// <summary>
	/// The location of the application.
	/// </summary>
	string ApplicationDataLocation { get; }

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
	/// The name of the application.
	/// </summary>
	string ApplicationName { get; }

	/// <summary>
	/// The time it took for the application to startup.
	/// </summary>
	TimeSpan ApplicationStartup { get; }

	/// <summary>
	/// The version of the application.
	/// </summary>
	Version ApplicationVersion { get; }

	/// <summary>
	/// The platform of the device.
	/// </summary>
	DevicePlatform DevicePlatform { get; }

	/// <summary>
	/// The type of the device.
	/// </summary>
	DeviceType DeviceType { get; }

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