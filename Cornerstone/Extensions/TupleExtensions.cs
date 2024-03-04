#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for Tuple.
/// </summary>
public static class TupleExtensions
{
	#region Fields

	private static readonly HashSet<Type> _tupleTypes;
	private static readonly HashSet<Type> _valueTupleTypes;

	#endregion

	#region Constructors

	static TupleExtensions()
	{
		_tupleTypes =
		[
			typeof(Tuple<>),
			typeof(Tuple<,>),
			typeof(Tuple<,,>),
			typeof(Tuple<,,,>),
			typeof(Tuple<,,,,>),
			typeof(Tuple<,,,,,>),
			typeof(Tuple<,,,,,,>),
			typeof(Tuple<,,,,,,,>)
		];

		_valueTupleTypes =
		[
			typeof(ValueTuple<>),
			typeof(ValueTuple<,>),
			typeof(ValueTuple<,,>),
			typeof(ValueTuple<,,,>),
			typeof(ValueTuple<,,,,>),
			typeof(ValueTuple<,,,,,>),
			typeof(ValueTuple<,,,,,,>),
			typeof(ValueTuple<,,,,,,,>)
		];
	}

	#endregion

	#region Methods

	/// <summary>
	/// Create a tuple from the provided values.
	/// </summary>
	/// <param name="values"> The values for the tuple. </param>
	/// <returns> The tuple. </returns>
	public static object CreateTuple(object[] values)
	{
		if (values.Length < 1)
		{
			throw new ArgumentException("Not enough values to create a tuple.");
		}
		if (values.Length > 8)
		{
			throw new ArgumentException("Too many values to create a tuple.");
		}

		var types = values.Select(x => x.GetType()).ToArray();

		return values.Length switch
		{
			1 => typeof(Tuple<>).CreateInstanceOfGeneric(types, values),
			2 => typeof(Tuple<,>).CreateInstanceOfGeneric(types, values),
			3 => typeof(Tuple<,,>).CreateInstanceOfGeneric(types, values),
			4 => typeof(Tuple<,,,>).CreateInstanceOfGeneric(types, values),
			5 => typeof(Tuple<,,,,>).CreateInstanceOfGeneric(types, values),
			6 => typeof(Tuple<,,,,,>).CreateInstanceOfGeneric(types, values),
			7 => typeof(Tuple<,,,,,,>).CreateInstanceOfGeneric(types, values),
			8 => typeof(Tuple<,,,,,,,>).CreateInstanceOfGeneric(types, values),
			_ => throw new ArgumentException("Too many values to create a tuple.")
		};
	}

	/// <summary>
	/// Create a value tuple from the provided values.
	/// </summary>
	/// <param name="values"> The values for the tuple. </param>
	/// <returns> The value tuple. </returns>
	public static object CreateValueTuple(object[] values)
	{
		if (values.Length < 1)
		{
			throw new ArgumentException("Not enough values to create a tuple.");
		}
		if (values.Length > 8)
		{
			throw new ArgumentException("Too many values to create a tuple.");
		}

		var types = values.Select(x => x.GetType()).ToArray();

		return values.Length switch
		{
			1 => typeof(ValueTuple<,>).CreateInstanceOfGeneric(types, values),
			2 => typeof(ValueTuple<,>).CreateInstanceOfGeneric(types, values),
			3 => typeof(ValueTuple<,,>).CreateInstanceOfGeneric(types, values),
			4 => typeof(ValueTuple<,,,>).CreateInstanceOfGeneric(types, values),
			5 => typeof(ValueTuple<,,,,>).CreateInstanceOfGeneric(types, values),
			6 => typeof(ValueTuple<,,,,,>).CreateInstanceOfGeneric(types, values),
			7 => typeof(ValueTuple<,,,,,,>).CreateInstanceOfGeneric(types, values),
			8 => typeof(ValueTuple<,,,,,,,>).CreateInstanceOfGeneric(types, values),
			_ => throw new ArgumentException("Too many values to create a tuple.")
		};
	}

	/// <summary>
	/// Get the values from the tuple.
	/// </summary>
	/// <param name="tuple"> The tuple to get values from. </param>
	/// <returns> The tuple values. </returns>
	public static List<object> GetTupleItemObjects(this object tuple)
	{
		if (tuple.IsValueTuple())
		{
			return GetValueTupleItemFields(tuple.GetType())
				.Select(x => x.GetValue(tuple))
				.ToList();
		}

		return GetTupleItemProperties(tuple.GetType())
			.Select(x => x.GetValue(tuple))
			.ToList();
	}

	/// <summary>
	/// Get the values from the tuple.
	/// </summary>
	/// <param name="type"> The type to get values from. </param>
	/// <returns> The tuple values. </returns>
	public static List<Type> GetTupleItemTypes(this Type type)
	{
		if (type.IsValueTuple())
		{
			return GetValueTupleItemFields(type)
				.Select(x => x.FieldType)
				.ToList();
		}

		return GetTupleItemProperties(type)
			.Select(x => x.PropertyType)
			.ToList();
	}

	/// <summary>
	/// Get the values from the tuple.
	/// </summary>
	/// <param name="tuple"> The tuple to get values from. </param>
	/// <returns> The tuple values. </returns>
	public static Dictionary<string, object> GetValueTupleItemDictionary(this object tuple)
	{
		var item = 1;
		var values = GetTupleItemObjects(tuple);
		return values.ToDictionary(_ => $"Item{item++}", x => x);
	}

	/// <summary>
	/// Determine if the type is a tuple
	/// </summary>
	/// <param name="obj"> The object to test. </param>
	/// <returns> True if the type is a Tuple type. </returns>
	public static bool IsTuple(this object obj)
	{
		return IsTuple(obj?.GetType());
	}

	/// <summary>
	/// Determine if the type is a tuple.
	/// </summary>
	/// <param name="type"> The type to test. </param>
	/// <returns> True if the type is a Tuple type. </returns>
	public static bool IsTuple(this Type type)
	{
		return (type != null)
			&& type.GetTypeInfo().IsGenericType
			&& _tupleTypes.Contains(type.GetGenericTypeDefinition());
	}

	/// <summary>
	/// Determine if the type is a tuple
	/// </summary>
	/// <param name="obj"> The object to test. </param>
	/// <returns> True if the type is a Tuple type. </returns>
	public static bool IsValueTuple(this object obj)
	{
		return IsValueTuple(obj?.GetType());
	}

	/// <summary>
	/// Determine if the type is a value tuple.
	/// </summary>
	/// <param name="type"> The type to test. </param>
	/// <returns> True if the type is a ValueTuple type. </returns>
	public static bool IsValueTuple(this Type type)
	{
		return (type != null)
			&& type.GetTypeInfo().IsGenericType
			&& _valueTupleTypes.Contains(type.GetGenericTypeDefinition());
	}

	private static IEnumerable<PropertyInfo> GetTupleItemProperties(this Type type)
	{
		return type.GetCachedProperties();
	}

	private static IEnumerable<FieldInfo> GetValueTupleItemFields(this Type type)
	{
		FieldInfo field;

		var items = new List<FieldInfo>();
		var nth = 1;

		while ((field = type.GetRuntimeField($"Item{nth}")) != null)
		{
			nth++;
			items.Add(field);
		}

		return items;
	}

	#endregion
}