#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Serialization.Json.Converters;
using Cornerstone.Serialization.Json.Values;
using TupleExtensions = Cornerstone.Extensions.TupleExtensions;

#endregion

namespace Cornerstone.Convert.Converters;

/// <summary>
/// Convert JsonValues to primitive types.
/// </summary>
public class JsonValueConverter : BaseConverter
{
	#region Constructors

	/// <inheritdoc />
	public JsonValueConverter() : base(
		new Guid("A8D02A54-2F8D-412C-945F-046EB8D57AAB"),
		Activator.JsonValueTypes,
		ArrayExtensions.CombineArrays(
			Activator.JsonValueTypes,
			Activator.StringTypes
		))
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override bool CanConvert(Type fromType, Type toType)
	{
		// We only check the [from] type.
		return FromTypes.Contains(fromType);
	}

	/// <inheritdoc />
	public override bool TryConvertTo(object from, Type fromType, Type toType, out object value, IConverterSettings settings = null)
	{
		if (from == null)
		{
			value = null;
			return toType.IsNullable();
		}

		switch (from)
		{
			case JsonBoolean jValue:
			{
				return jValue.Value.TryConvertTo(toType, out value);
			}
			case JsonNumber jValue:
			{
				return jValue.Value.TryConvertTo(toType, out value);
			}
			case JsonString jValue:
			{
				return jValue.Value.TryConvertTo(toType, out value);
			}
			case JsonArray jValue:
			{
				return
					DataTableConverter.ParseAsTable(toType, out value, jValue)
					|| ParseAsArray(toType, out value, jValue)
					|| ParseAsList(toType, out value, jValue);
			}
			case JsonObject jValue:
			{
				return
					ParseAsTuple(toType, out value, jValue)
					|| ParseAsDictionary(toType, out value, jValue)
					|| ParseAsPartialUpdate(toType, out value, jValue)
					|| ParseAsObject(toType, out value, jValue);
			}
		}

		return base.TryConvertTo(from, fromType, toType, out value, settings);
	}

	private static bool ParseAsArray(Type toType, out object value, JsonArray jValue)
	{
		if (!toType.IsArray)
		{
			value = null;
			return false;
		}

		var array = (Array) toType.CreateInstance(jValue.Count);
		var elementType = toType.GetElementType();

		for (var i = 0; i < jValue.Count; i++)
		{
			var iValue = jValue[i].ConvertTo(elementType);
			array.SetValue(iValue, i);
		}
		value = array;
		return true;
	}

	private static bool ParseAsDictionary(Type toType, out object value, JsonObject jValue)
	{
		if (!toType.ImplementsType<IDictionary>())
		{
			value = null;
			return false;
		}

		var response = (IDictionary) toType.CreateInstance();
		var arguments = toType.GetGenericArguments();

		foreach (var key in jValue.Keys)
		{
			var kValue = key.ConvertTo(arguments[0]);
			var vValue = jValue[key].ConvertTo(arguments[1]);

			response.Add(kValue, vValue);
		}
		value = response;
		return true;
	}

	private static bool ParseAsList(Type toType, out object value, JsonArray jValue)
	{
		if (toType.IsGenericTypeDefinition)
		{
			if (toType == typeof(IList<>))
			{
				toType = typeof(List<object>);
			}
			else if (toType.GetGenericTypeDefinition() == typeof(IList<>))
			{
				toType = typeof(List<>).GetCachedMakeGenericType(toType.GenericTypeArguments);
			}
		}
		else if (toType.ImplementsType(typeof(IList<>)) && (toType.GenericTypeArguments.Length > 0))
		{
			toType = typeof(List<>).GetCachedMakeGenericType(toType.GenericTypeArguments);
		}

		if (!toType.ImplementsType<IList>())
		{
			value = null;
			return false;
		}

		var list = (IList) toType.CreateInstance();
		var elementType = toType.GetGenericArgumentsRecursive()?.FirstOrDefault() ?? typeof(object);
		for (var i = 0; i < jValue.Count; i++)
		{
			var iValue = jValue[i].ConvertTo(elementType);
			list.Add(iValue);
		}
		value = list;
		return true;
	}

	private bool ParseAsObject(Type toType, out object value, JsonObject jValue)
	{
		var response = toType.CreateInstance();
		var properties = toType.GetCachedProperties();

		foreach (var key in jValue.Keys)
		{
			var property = properties.FirstOrDefault(x => x.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
			if ((property == null) || !property.CanWrite)
			{
				continue;
			}

			var pValue = jValue[key];
			property.SetValue(response, pValue.ConvertTo(property.PropertyType));
		}

		value = response;
		return true;
	}

	private bool ParseAsPartialUpdate(Type toType, out object value, JsonObject jValue)
	{
		if ((toType != typeof(PartialUpdate))
			&& !toType.ImplementsType<PartialUpdate>())
		{
			value = null;
			return false;
		}

		var response = (PartialUpdate) toType.CreateInstance();
		var properties = toType.GetProperties();

		foreach (var key in jValue.Keys)
		{
			var propertyInfo = properties.FirstOrDefault(x => string.Equals(x.Name, key, StringComparison.OrdinalIgnoreCase));
			if (propertyInfo == null)
			{
				response.AddOrUpdate(key, jValue[key]);
				continue;
			}

			var jKeyValue = jValue[key];
			if (!jKeyValue.TryConvertTo(propertyInfo.PropertyType, out value))
			{
				continue;
			}

			if (propertyInfo.CanWrite)
			{
				propertyInfo.SetValue(response, value);
				continue;
			}

			var propertyValue = propertyInfo.GetValue(response);
			if (propertyValue is IList list && value is IEnumerable enumerable)
			{
				list.Reconcile(enumerable);
				
				continue;
			}

			if (propertyValue is IUpdateable updateable)
			{
				updateable.UpdateWith(value);
				continue;
			}

			response.AddOrUpdate(propertyInfo.Name, value);
		}

		value = response;
		return true;
	}

	private static bool ParseAsTuple(Type toType, out object value, JsonObject jValue)
	{
		if (!toType.IsTuple() && !toType.IsValueTuple())
		{
			value = null;
			return false;
		}

		var types = toType.GetTupleItemTypes();
		var jValues = jValue.Values.ToList();
		var values = jValues
			.Select((t, i) => t.ConvertTo(types[i]))
			.ToArray();

		value = toType.IsTuple()
			? TupleExtensions.CreateTuple(values)
			: TupleExtensions.CreateValueTuple(values);
		return true;
	}

	#endregion
}