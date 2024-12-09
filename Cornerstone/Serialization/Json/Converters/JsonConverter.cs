#region References

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