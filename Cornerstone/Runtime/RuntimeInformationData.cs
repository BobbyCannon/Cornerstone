#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Cornerstone.Data.Bytes;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Runtime;

/// <inheritdoc cref="IRuntimeInformation" />
public struct RuntimeInformationData : IRuntimeInformation
{
	#region Properties

	/// <inheritdoc />
	public Bitness ApplicationBitness { get; set; }

	/// <inheritdoc />
	public string ApplicationDataLocation { get; set; }

	/// <inheritdoc />
	public string ApplicationFileName { get; set; }

	/// <inheritdoc />
	public string ApplicationFilePath { get; set; }

	/// <inheritdoc />
	public bool ApplicationIsDevelopmentBuild { get; set; }

	/// <inheritdoc />
	public bool ApplicationIsElevated { get; set; }

	/// <inheritdoc />
	public string ApplicationLocation { get; set; }

	/// <inheritdoc />
	public Size DeviceDisplaySize { get; set;  }

	/// <inheritdoc />
	public string ApplicationName { get; set; }

	/// <inheritdoc />
	public Version ApplicationVersion { get; set; }

	/// <inheritdoc />
	public int Count => Keys.Count();

	/// <inheritdoc />
	public string DeviceId { get; set; }

	/// <inheritdoc />
	public string DeviceManufacturer { get; set; }

	/// <inheritdoc />
	public ByteSize DeviceMemory { get; set; }

	/// <inheritdoc />
	public string DeviceModel { get; set; }

	/// <inheritdoc />
	public string DeviceName { get; set; }

	/// <inheritdoc />
	public DevicePlatform DevicePlatform { get; set; }

	/// <inheritdoc />
	public Bitness DevicePlatformBitness { get; set; }

	/// <inheritdoc />
	public Version DevicePlatformVersion { get; set; }

	/// <inheritdoc />
	public DeviceType DeviceType { get; set; }

	/// <inheritdoc />
	public Version DotNetRuntimeVersion { get; set; }

	/// <inheritdoc />
	public bool IsLoaded { get; set; }

	/// <inheritdoc />
	public bool IsShuttingDown { get; set; }

	/// <inheritdoc />
	public object this[string key] => throw new NotImplementedException();

	/// <inheritdoc />
	public IEnumerable<string> Keys => this.Select(x => x.Key);

	/// <inheritdoc />
	public IEnumerable<object> Values => this.Select(x => x.Value);

	#endregion

	#region Methods

	/// <inheritdoc />
	public bool ContainsKey(string key)
	{
		var properties = GetType().GetCachedProperties();
		return properties.Any(x => x.Name == key);
	}

	/// <inheritdoc />
	public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
	{
		var properties = typeof(IRuntimeInformation).GetCachedProperties();
		foreach (var property in properties)
		{
			yield return new KeyValuePair<string, object>(property.Name, property.GetValue(this));
		}
	}

	/// <summary>
	/// Return an IRuntimeInformation sample.
	/// </summary>
	/// <returns> The sample data. </returns>
	public static IRuntimeInformation GetSample()
	{
		return new RuntimeInformationData
		{
			ApplicationBitness = Bitness.X86,
			ApplicationDataLocation = @"C:\The\Quick\BrownFox\Jumped\Over\The\Lazy\DogsBack",
			ApplicationFileName = "App.exe",
			ApplicationFilePath = @"\\Server\Foo\Bar\Hello\World\App.exe",
			ApplicationIsDevelopmentBuild = true,
			ApplicationIsElevated = true,
			ApplicationLocation = @"\\Server\Foo\Bar\Hello\World",
			ApplicationName = "Cornerstone",
			ApplicationVersion = new Version(1, 2, 3, 4),
			DeviceDisplaySize = new Size(123, 456),
			DeviceId = "DEV-123",
			DeviceManufacturer = "Asus",
			DeviceMemory = ByteSize.FromGigabytes(24),
			DeviceModel = "Rog Super Computer X",
			DeviceName = "rog-super-rig",
			DevicePlatform = DevicePlatform.Windows,
			DevicePlatformBitness = Bitness.X64,
			DevicePlatformVersion = new Version(9, 8, 7, 6),
			DeviceType = DeviceType.Desktop,
			DotNetRuntimeVersion = new Version(8, 0, 0, 0),
			IsLoaded = true,
			IsShuttingDown = false,
		};
	}

	/// <inheritdoc />
	public bool TryGetValue(string key, out object value)
	{
		var properties = typeof(IRuntimeInformation).GetCachedProperties();
		var property = properties.FirstOrDefault(x => x.Name == key);
		if (property == null)
		{
			value = null;
			return false;
		}

		value = property.GetValue(this);
		return true;
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}