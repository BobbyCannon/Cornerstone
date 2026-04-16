#region References

using Cornerstone.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Convert.Converters;

public class CollectionConverter : BaseConverter
{
	#region Constructors

	public CollectionConverter() : base(new Guid("3681DE88-6E3D-4424-AFF3-5FC8EE4A4B76"))
	{
	}

	#endregion

	#region Methods

	public override bool CanConvert(Type fromType, Type toType)
	{
		var sourceIsCollection = typeof(IEnumerable).IsAssignableFrom(fromType)
			&& (fromType != typeof(string));

		var targetIsNotCollection = typeof(IEnumerable).IsAssignableFrom(fromType)
			&& (fromType != typeof(string));

		return sourceIsCollection
			&& targetIsNotCollection;
	}

	public override bool TryConvertTo(object from, Type fromType, Type toType, out object value, IConverterSettings settings = null)
	{
		if (from is not IEnumerable enumerable)
		{
			value = null;
			return false;
		}

		var items = enumerable.Cast<object>().ToList();

		return TryConvertToUsingConstructor(items, toType, out value, settings)
			|| TryConvertToUsingProperties(items, toType, out value, settings)
			|| base.TryConvertTo(from, fromType, toType, out value, settings);
	}

	private bool TryConvertToUsingConstructor(List<object> items, Type toType, out object value, IConverterSettings settings)
	{
		var sourceType = SourceReflector.GetRequiredSourceType(toType);
		var constructor = sourceType.DeclaredConstructors
			.FirstOrDefault(c => c.Parameters.Length == items.Count);

		if (constructor == null)
		{
			value = null;
			return false;
		}

		// Check parameter names match indices? No — we match by position only.
		// Optionally: you could enhance this to match by name if parameters have names like "item0", "item1", etc.

		var arguments = new object[constructor.Parameters.Length];

		for (var i = 0; i < constructor.Parameters.Length; i++)
		{
			var paramType = constructor.Parameters[i].ParameterType;
			var item = i < items.Count ? items[i] : null;

			if (item == null)
			{
				arguments[i] = paramType.IsValueType
					? SourceReflector.CreateInstance(paramType)
					: null;
			}
			else
			{
				arguments[i] = item.ConvertTo(paramType, settings);
			}
		}

		value = constructor.Invoke(arguments);
		return true;
	}

	private bool TryConvertToUsingProperties(List<object> items, Type toType, out object value, IConverterSettings settings)
	{
		var sourceType = SourceReflector.GetRequiredSourceType(toType);

		// Special case: Target is an array (e.g. int[], string[])
		if (toType.IsArray)
		{
			if (toType.GetArrayRank() != 1)
			{
				// Only support single-dimensional arrays for now
				value = null;
				return false;
			}

			var elementType = toType.GetElementType();
			var array = Array.CreateInstance(elementType, items.Count);

			for (var i = 0; i < items.Count; i++)
			{
				var item = items[i];
				var converted = item == null
					? elementType.IsValueType
						? SourceReflector.CreateInstance(elementType)
						: null
					: item.ConvertTo(elementType, settings);

				array.SetValue(converted, i);
			}

			value = array;
			return true;
		}

		var properties = sourceType
			.GetProperties()
			.Where(p => p.CanWrite)
			.OrderBy(p => p.PropertyInfo.GetCustomAttribute<UpdateableActionAttribute>()?.Order ?? int.MaxValue)
			.ThenBy(p => p.Name)
			.ToList();

		var primaryConstructor = sourceType
			.GetConstructors()
			.FirstOrDefault(c =>
				(c.Parameters.Length == properties.Count)
				&& c.Parameters
					.Select(p => p.Name)
					.SequenceEqual(properties.Select(p => p.Name), StringComparer.OrdinalIgnoreCase)
			);

		if (primaryConstructor != null)
		{
			var args = new object[primaryConstructor.Parameters.Length];

			for (var i = 0; i < primaryConstructor.Parameters.Length; i++)
			{
				var param = primaryConstructor.Parameters[i];
				var sourceValue = i < items.Count ? items[i] : null;

				args[i] = sourceValue == null
					? param.ParameterType.IsValueType
						? SourceReflector.CreateInstance(param.ParameterType)
						: null
					: sourceValue.ConvertTo(param.ParameterType, settings);
			}

			value = primaryConstructor.Invoke(args);
			return true;
		}

		if (sourceType.Type.HasDefaultConstructor())
		{
			value = SourceReflector.CreateInstance(sourceType);

			for (var i = 0; (i < properties.Count) && (i < items.Count); i++)
			{
				var property = properties[i];
				var sourceValue = items[i];

				var converted = sourceValue.ConvertTo(property.PropertyInfo.PropertyType, settings);
				property.SetValue(value, converted);
			}

			return true;
		}

		value = null;
		return false;
	}

	#endregion
}