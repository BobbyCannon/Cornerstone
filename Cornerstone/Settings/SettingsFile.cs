#region References

using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using Cornerstone.Serialization;

#endregion

namespace Cornerstone.Settings;

/// <summary>
/// Represents a set of settings to load and save to a file in the binary or json format.
/// </summary>
public abstract class SettingsFile<T>
	: Notifiable, IPackable
	where T : IPackable, new()
{
	#region Fields

	private readonly string _directory;
	private readonly string _fileName;

	#endregion

	#region Constructors

	/// <summary>
	/// For serialization, do not use.
	/// </summary>
	protected SettingsFile() : this(string.Empty, string.Empty)
	{
	}

	/// <summary>
	/// Create an instance of the settings file.
	/// </summary>
	/// <param name="fileName"> The name of the file for the settings. </param>
	/// <param name="runtimeInformation"> The runtime information. </param>
	protected SettingsFile(string fileName, IRuntimeInformation runtimeInformation)
		: this(fileName, runtimeInformation?.ApplicationDataLocation)
	{
	}

	/// <summary>
	/// Create an instance of the settings file.
	/// </summary>
	/// <param name="fileName"> The name of the file for the settings. </param>
	/// <param name="directory"> The directory to store the file in. </param>
	protected SettingsFile(string fileName, string directory)
	{
		_fileName = fileName;
		_directory = directory;
	}

	#endregion

	#region Methods

	public virtual void FromSpeedyPacket(SpeedyPacket values)
	{
	}

	public virtual JsonSerializerOptions GetSerializationSettings()
	{
		return new JsonSerializerOptions();
	}

	/// <summary>
	/// Loads the settings from a json file in the application data location.
	/// </summary>
	public void Load()
	{
		var filePath = Path.Combine(_directory, _fileName);
		var extension = Path.GetExtension(filePath);

		if (File.Exists(filePath))
		{
			switch (extension)
			{
				case ".bson":
				{
					var data = File.ReadAllBytes(filePath);
					Load(data);
					return;
				}
				case ".json":
				{
					var data = File.ReadAllText(filePath);
					Load(data);
					return;
				}
			}
		}
		FinalizeLoad();
		ResetHasChanges();
	}

	/// <summary>
	/// Loads the settings from a byte[].
	/// </summary>
	public void Load(byte[] data)
	{
		try
		{
			var instance = SpeedyPack.Unpack<T>(data);
			UpdateWith(instance);
		}
		catch
		{
			// todo: what should we do? Unpacked failed?
			#if DEBUG
			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}
			#endif
		}
		FinalizeLoad();
		ResetHasChanges();
	}

	/// <summary>
	/// Loads the settings from a string.
	/// </summary>
	public void Load(string data)
	{
		try
		{
			var instance = JsonSerializer.Deserialize<T>(data, GetSerializationSettings());
			UpdateWith(instance);
		}
		catch
		{
			// todo: what should we do? Json failed?
			#if DEBUG
			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}
			#endif
		}
		FinalizeLoad();
		ResetHasChanges();
	}

	/// <summary>
	/// Save the settings to a json file in the application data location.
	/// </summary>
	public void Save(bool force = false)
	{
		if (!force && !HasNotifiableChanges())
		{
			return;
		}

		var filePath = Path.Combine(_directory, _fileName);
		var extension = Path.GetExtension(filePath);

		switch (extension)
		{
			case ".bson":
			{
				var bson = SpeedyPack.Pack(this);
				new DirectoryInfo(_directory).SafeCreate();
				File.WriteAllBytes(filePath, bson);
				break;
			}
			case ".json":
			{
				var json = JsonSerializer.Serialize(this, typeof(T), GetSerializationSettings());
				new DirectoryInfo(_directory).SafeCreate();
				File.WriteAllText(filePath, json);
				break;
			}
			default:
			{
				throw new NotSupportedException();
			}
		}

		ResetHasChanges();
	}

	public virtual SpeedyPacket ToSpeedyPacket()
	{
		return new SpeedyPacket();
	}

	/// <summary>
	/// Finalize the load.
	/// </summary>
	protected virtual void FinalizeLoad()
	{
	}

	#endregion
}