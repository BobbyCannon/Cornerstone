#region References

using System.Diagnostics;
using System.IO;
using System.Text;
using Cornerstone.Convert;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Serialization;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Settings;

/// <summary>
/// Represents a set of settings to load and save to a file in the JSON format.
/// </summary>
public abstract class SettingsFile<T> : Settings<T>
{
	#region Fields

	private readonly string _directory;
	private readonly string _fileName;
	private readonly SerializationSettings _serializationSettings;

	#endregion

	#region Constructors

	/// <summary>
	/// For serialization, do not use.
	/// </summary>
	protected SettingsFile() : this(string.Empty, string.Empty, null)
	{
	}

	/// <summary>
	/// Create an instance of the settings file.
	/// </summary>
	/// <param name="fileName"> The name of the file for the settings. </param>
	/// <param name="runtimeInformation"> The runtime information. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	protected SettingsFile(string fileName, IRuntimeInformation runtimeInformation, IDispatcher dispatcher)
		: this(fileName, runtimeInformation.ApplicationDataLocation, dispatcher)
	{
	}

	/// <summary>
	/// Create an instance of the settings file.
	/// </summary>
	/// <param name="fileName"> The name of the file for the settings. </param>
	/// <param name="directory"> The directory to store the file in. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	protected SettingsFile(string fileName, string directory, IDispatcher dispatcher) : base(dispatcher)
	{
		_fileName = fileName;
		_directory = directory;
		_serializationSettings = new SerializationSettings
		{
			EnumFormat = EnumFormat.Value,
			IgnoreDefaultValues = true,
			IgnoreNullValues = true,
			IgnoreReadOnly = true,
			MaxDepth = int.MaxValue,
			NamingConvention = NamingConvention.PascalCase,
			TextFormat = TextFormat.None,
			UpdateableAction = UpdateableAction.PartialUpdate
		};
	}

	#endregion

	#region Methods

	/// <summary>
	/// Loads the settings from a json file in the application data location.
	/// </summary>
	public void Load()
	{
		var filePath = Path.Combine(_directory, _fileName);
		if (File.Exists(filePath))
		{
			var json = File.ReadAllText(filePath);
			Load(json);
			return;
		}
		FinalizeLoad();
		ResetHasChanges();
	}

	/// <summary>
	/// Loads the settings from a json string.
	/// </summary>
	public void Load(string json)
	{
		try
		{
			var instance = json.FromJson<T>(_serializationSettings);
			UpdateWith(instance);
		}
		catch
		{
			// todo: what should we do? Json failed?
			Debugger.Break();
		}
		FinalizeLoad();
		ResetHasChanges();
	}

	/// <summary>
	/// Save the settings to a json file in the application data location.
	/// </summary>
	public void Save(bool force = false)
	{
		if (!force && !HasChanges())
		{
			return;
		}

		var filePath = Path.Combine(_directory, _fileName);
		var json = ToJson();

		new DirectoryInfo(_directory).SafeCreate();
		File.WriteAllText(filePath, json, Encoding.UTF8);

		ResetHasChanges();
	}

	#endregion
}