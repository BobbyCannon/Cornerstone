﻿#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Exceptions;
using Cornerstone.Extensions;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Values;

#endregion

namespace Cornerstone.Serialization.Json.Converters;

/// <inheritdoc />
public abstract class JsonConverter : IJsonConverter
{
	#region Fields

	private readonly ReadOnlySet<Type> _types;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize the converter with types it can convert for.
	/// </summary>
	/// <param name="types"> The types this converter is for. </param>
	protected JsonConverter(params Type[] types)
	{
		var allTypes = new HashSet<Type>(types);

		foreach (var type in types.Where(x => !x.IsNullable()))
		{
			var nullableType = type.ToNullableType();
			allTypes.Add(nullableType);
		}

		_types = new ReadOnlySet<Type>(allTypes);
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public abstract void Append(object value, Type valueType, IObjectConsumer consumer, ISerializationSettings settings);

	/// <inheritdoc />
	public virtual bool CanConvert(Type type)
	{
		return _types.Contains(type);
	}

	/// <inheritdoc />
	public abstract object ConvertTo(Type type, JsonValue jsonValue);

	/// <inheritdoc />
	public abstract string GetJsonString(object value, ISerializationSettings settings);

	/// <inheritdoc />
	public Type[] GetSupportedTypes()
	{
		return _types.ToArray();
	}

	/// <summary>
	/// Handles the JSON null value type.
	/// </summary>
	/// <param name="type"> The type to be tested. </param>
	/// <returns> Returns null unless the type is nullable otherwise throw exception. </returns>
	protected object HandleJsonNull(Type type)
	{
		if (type.IsNullable())
		{
			return null;
		}

		throw new CornerstoneException("The type is non-nullable but the json was null.");
	}

	#endregion
}

/// <summary>
/// Interface for converting .NET objects to JSON and back.
/// </summary>
public interface IJsonConverter
{
	#region Methods

	/// <summary>
	/// Writes data of the .NET object to the given <see cref="IObjectConsumer" />
	/// in order to convert it to a JSON string or to any other representation of
	/// JSON data.
	/// </summary>
	/// <param name="value">
	/// The .NET object to write. Implementors may assume that the type of the incoming
	/// value can be cast to the target type of this converter, however, it may be null.
	/// </param>
	/// <param name="valueType"> The type of the value. </param>
	/// <param name="consumer">
	/// The JSON consumer which must be used to write the data to according to the
	/// contents of the given value.
	/// </param>
	/// <param name="settings"> </param>
	void Append(object value, Type valueType, IObjectConsumer consumer, ISerializationSettings settings);

	/// <summary>
	/// Determine if the converter can covert the provided type.
	/// </summary>
	/// <param name="type"> The type to check. </param>
	/// <returns> True if the type is supported otherwise false. </returns>
	bool CanConvert(Type type);

	/// <summary>
	/// Convert a JSON value to an object of the provided type.
	/// </summary>
	/// <param name="type"> The type to convert to. </param>
	/// <param name="jsonValue"> The value to convert. </param>
	/// <returns> The instance of the object. </returns>
	object ConvertTo(Type type, JsonValue jsonValue);

	/// <summary>
	/// Writes data of the .NET object to the given <see cref="IObjectConsumer" />
	/// in order to convert it to a JSON string or to any other representation of
	/// JSON data.
	/// </summary>
	/// <param name="value">
	/// The .NET object to write. Implementors may assume that the type of the incoming
	/// value can be cast to the target type of this converter, however, it may be null.
	/// </param>
	/// <param name="settings"> The settings for serializing. </param>
	/// <returns> The JSON for the provided value. </returns>
	string GetJsonString(object value, ISerializationSettings settings);

	/// <summary>
	/// Return the types the value converter supports.
	/// </summary>
	/// <returns> The types this converter supports. </returns>
	Type[] GetSupportedTypes();

	#endregion
}