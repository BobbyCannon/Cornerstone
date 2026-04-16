#region References

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Convert.Converters;

public class DictionaryConverter : BaseConverter
{
	#region Constructors

	public DictionaryConverter() : base(new Guid("B1E4DC54-073E-49E5-BF41-82CDD35A3E68"))
	{
	}

	#endregion

	#region Methods

	public override bool CanConvert(Type fromType, Type toType)
	{
		return fromType.ImplementsType<IDictionary>()
			&& !toType.ImplementsType<IDictionary>();
	}

	public override bool TryConvertTo(object from, Type fromType, Type toType, out object value, IConverterSettings settings = null)
	{
		if (from is not IDictionary dictionary)
		{
			value = null;
			return false;
		}

		if (SourceReflector.HasDefaultConstructor(toType))
		{
			return TryConvertToUsingProperties(dictionary, toType, out value, settings);
		}

		return TryConvertToUsingConstructor(dictionary, toType, out value, settings)
			|| base.TryConvertTo(from, fromType, toType, out value, settings);
	}

	private bool TryConvertToUsingConstructor(IDictionary dictionary, Type toType, out object value, IConverterSettings settings)
	{
		var sourceType = SourceReflector.GetRequiredSourceType(toType);
		var constructor = sourceType.DeclaredConstructors.FirstOrDefault(x => x.Parameters.Length == dictionary.Keys.Count);
		if (constructor == null)
		{
			value = null;
			return false;
		}

		var keys = dictionary.Keys.IterateList();
		if (constructor.Parameters.Any(x => !keys.Contains(x.Name)))
		{
			value = null;
			return false;
		}

		var values = new object[constructor.Parameters.Length];

		for (var i = 0; i < constructor.Parameters.Length; i++)
		{
			var parameter = constructor.Parameters[i];
			values[i] = dictionary[parameter.Name].ConvertTo(parameter.ParameterType);
		}

		value = constructor.Invoke(values);
		return true;
	}

	private bool TryConvertToUsingProperties(IDictionary dictionary, Type toType, out object value, IConverterSettings settings)
	{
		var sourceType = SourceReflector.GetRequiredSourceType(toType);
		var properties = sourceType
			.GetProperties()
			.Where(x => x.CanWrite)
			.OrderBy(x => x.PropertyInfo.GetCustomAttribute<UpdateableActionAttribute>()?.Order ?? int.MaxValue)
			.ThenBy(x => x.Name)
			.ToList();

		// Check if the type is a record
		var constructor = sourceType.GetConstructors()
			.FirstOrDefault(c =>
				(c.Parameters.Length == properties.Count) &&
				c.Parameters.Select(p => p.Name).SequenceEqual(properties.Select(p => p.Name), StringComparer.OrdinalIgnoreCase));

		if (constructor != null)
		{
			// For records, use the constructor with matching parameters
			var parameters = constructor
				.Parameters
				.Select(p => dictionary.Contains(p.Name)
					? dictionary[p.Name].ConvertTo(p.ParameterType, settings)
					: p.ParameterType.IsValueType
						? SourceReflector.CreateInstance(p.ParameterType)
						: null)
				.ToArray();

			value = constructor.Invoke(parameters);
		}
		else
		{
			// Fallback to original property-based instantiation
			var response = SourceReflector.CreateInstance(sourceType);
			foreach (var p in properties)
			{
				if (dictionary.Contains(p.Name))
				{
					p.SetValue(response, dictionary[p.Name].ConvertTo(p.PropertyInfo.PropertyType, settings));
				}
			}
			value = response;
		}

		return true;
	}

	#endregion
}