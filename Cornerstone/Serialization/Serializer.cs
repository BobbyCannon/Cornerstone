#region References

using System;
using Cornerstone.Convert;
using Cornerstone.Serialization.Json;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Serialization;

/// <inheritdoc />
public class Serializer : ISerializer
{
	#region Fields

	private IJsonSerializer _jsonSerializer;
	private bool _lockDefaultSettings;

	#endregion

	#region Constructors

	static Serializer()
	{
		DefaultSettings = new SerializationOptions
		{
			EnumFormat = EnumFormat.Value,
			IgnoreDefaultValues = false,
			IgnoreNullValues = false,
			IgnoreReadOnly = false,
			MaxDepth = int.MaxValue,
			NamingConvention = NamingConvention.PascalCase,
			TextFormat = TextFormat.None
		};
		Instance = new Serializer();
	}

	private Serializer()
	{
		// Set default serializers
		SetJsonSerializer(new JsonSerializer());
	}

	#endregion

	#region Properties

	/// <summary>
	/// The default settings for the serializer.
	/// </summary>
	public static ISerializationOptions DefaultSettings { get; private set; }

	/// <summary>
	/// The static instance of the serializer.
	/// </summary>
	public static ISerializer Instance { get; private set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public T FromJson<T>(string value, ISerializationOptions settings = null)
	{
		return _jsonSerializer.FromJson<T>(value, settings ?? DefaultSettings);
	}

	/// <inheritdoc />
	public object FromJson(string value, Type type, ISerializationOptions settings = null)
	{
		return _jsonSerializer.FromJson(value, type, settings ?? DefaultSettings);
	}

	/// <inheritdoc />
	public void LockDefaultSettings()
	{
		_lockDefaultSettings = true;
	}

	/// <inheritdoc />
	public void SetJsonSerializer(IJsonSerializer serializer)
	{
		_jsonSerializer = serializer;
	}

	/// <inheritdoc />
	public string ToJson<T>(T value, ISerializationOptions settings = null)
	{
		return _jsonSerializer.ToJson(value, settings ?? DefaultSettings);
	}

	/// <inheritdoc />
	public string ToJson(object value, Type type, ISerializationOptions settings = null)
	{
		return _jsonSerializer.ToJson(value, type, settings);
	}

	/// <inheritdoc />
	public void UpdateDefaultSettings(ISerializationOptions settings)
	{
		if (_lockDefaultSettings)
		{
			// todo: should we throw exception?
			return;
		}

		DefaultSettings = settings;
	}

	#endregion
}

/// <summary>
/// Represents all serializers.
/// </summary>
public interface ISerializer : IJsonSerializer
{
	#region Methods

	/// <summary>
	/// Lock the ability to update the default settings.
	/// </summary>
	void LockDefaultSettings();

	/// <summary>
	/// Set the serializer for JSON.
	/// </summary>
	/// <param name="serializer"> The serializer. </param>
	void SetJsonSerializer(IJsonSerializer serializer);

	/// <summary>
	/// Update the default settings.
	/// </summary>
	/// <param name="settings"> The new settings. </param>
	void UpdateDefaultSettings(ISerializationOptions settings);

	#endregion
}