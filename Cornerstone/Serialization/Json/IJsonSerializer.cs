#region References

using System;

#endregion

namespace Cornerstone.Serialization.Json;

/// <summary>
/// Represents a serializer for the JSON format.
/// </summary>
public interface IJsonSerializer
{
	#region Methods

	/// <summary>
	/// Convert the string into an object.
	/// </summary>
	/// <typeparam name="T"> The type to convert into. </typeparam>
	/// <param name="value"> The JSON data to deserialize. </param>
	/// <param name="settings"> The settings to be used. </param>
	/// <returns> The deserialized object. </returns>
	T FromJson<T>(string value, ISerializationOptions settings = null);

	/// <summary>
	/// Convert the string into an object.
	/// </summary>
	/// <param name="value"> The JSON data to deserialize. </param>
	/// <param name="type"> The type to convert into. </param>
	/// <param name="settings"> The settings to be used. </param>
	/// <returns> The deserialized object. </returns>
	object FromJson(string value, Type type, ISerializationOptions settings = null);

	/// <summary>
	/// Serialize an object into a JSON string.
	/// </summary>
	/// <typeparam name="T"> The type of the object to serialize. </typeparam>
	/// <param name="value"> The object to serialize. </param>
	/// <param name="settings"> The settings for the serializer. </param>
	/// <returns> The JSON string of the serialized object. </returns>
	string ToJson<T>(T value, ISerializationOptions settings = null);

	/// <summary>
	/// Serialize an object into a JSON string.
	/// </summary>
	/// <param name="value"> The object to serialize. </param>
	/// <param name="type"> The type of the object to serialize. </param>
	/// <param name="settings"> The settings for the serializer. </param>
	/// <returns> The JSON string of the serialized object. </returns>
	string ToJson(object value, Type type, ISerializationOptions settings = null);

	#endregion
}