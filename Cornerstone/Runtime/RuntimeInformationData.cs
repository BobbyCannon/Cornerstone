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
	public bool ApplicationIsLoaded { get; set; }

	/// <inheritdoc />
	public bool ApplicationIsShuttingDown { get; set; }

	/// <inheritdoc />
	public string ApplicationLocation { get; set; }

	/// <inheritdoc />
	public string ApplicationName { get; set; }

	/// <inheritdoc />
	public Version ApplicationVersion { get; set; }

	/// <inheritdoc />
	public int Count => Keys.Count();

	/// <inheritdoc />
	public Size DeviceDisplaySize { get; set; }

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
	public object this[string key]
	{
		get => this.GetCachedProperty(key).GetValue(this);
		set => throw new NotSupportedException();
	}

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
		var ignore = typeof(IReadOnlyDictionary<,>)
			.GetCachedProperties()
			.Select(x => x.Name)
			.ToHashSet()
			.AddRange(nameof(Count));

		var properties = typeof(RuntimeInformationData)
			.GetCachedProperties()
			.Where(x =>
				!ignore.Contains(x.Name)
				&& !x.IsIndexer()
			)
			.ToList();

		foreach (var property in properties)
		{
			yield return new KeyValuePair<string, object>(property.Name, property.GetValue(this));
		}
	}

	/// <summary>
	/// Return an IRuntimeInformation sample.
	/// </summary>
	/// <returns> The sample data. </returns>
	public static RuntimeInformationData GetSample()
	{
		return new RuntimeInformationData
		{
			ApplicationBitness = Bitness.X64,
			ApplicationDataLocation = "C:\\Users\\Public\\Documents",
			ApplicationFileName = "Sample.exe",
			ApplicationFilePath = "C:\\Users\\Public\\Documents\\Sample.exe",
			ApplicationIsDevelopmentBuild = false,
			ApplicationIsElevated = true,
			ApplicationLocation = "C:\\Users\\Public\\Documents\\",
			ApplicationName = "Sample",
			ApplicationVersion = new Version(2, 16, 1, 109),
			DeviceDisplaySize = new Size(3440, 1440),
			DeviceId = "WPGR602V4CZBT6BM82BPNYXMM9N8T0FK1K3G4KR3BXGB97AKYR23",
			DeviceManufacturer = "ASUS",
			DeviceMemory = ByteSize.FromGigabytes(128),
			DeviceModel = "ROG STRIX Z690-F",
			DeviceName = "Sample-RIG",
			DevicePlatform = DevicePlatform.Windows,
			DevicePlatformBitness = Bitness.X64,
			DevicePlatformVersion = new Version(10, 0, 26100, 0),
			DeviceType = DeviceType.Desktop,
			DotNetRuntimeVersion = new Version(9, 0, 0)
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