#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cornerstone.Collections;
using Cornerstone.Convert;
using Cornerstone.Data.TypeActivators;
using Cornerstone.Extensions;
using Cornerstone.Protocols.Osc;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Text;

#endregion

namespace Cornerstone;

/// <summary>
/// Activator help when creating new instances of types.
/// </summary>
public static class Activator
{
	#region Fields

	private static readonly ConcurrentDictionary<Type, TypeActivator> _builtInActivators;
	private static readonly ConcurrentDictionary<Type, TypeActivator> _customActivators;

	#endregion

	#region Constructors

	static Activator()
	{
		NonDecimalNumberTypes =
		[
			typeof(byte), typeof(byte?),
			typeof(sbyte), typeof(sbyte?),
			typeof(short), typeof(short?),
			typeof(ushort), typeof(ushort?),
			typeof(int), typeof(int?),
			typeof(uint), typeof(uint?),
			typeof(long), typeof(long?),
			typeof(ulong), typeof(ulong?),
			typeof(nint), typeof(nint?),
			typeof(nuint), typeof(nuint?),
			#if NET7_0_OR_GREATER
			typeof(Int128), typeof(Int128?),
			typeof(UInt128), typeof(UInt128?),
			#endif
		];

		DecimalNumberTypes =
		[
			typeof(decimal), typeof(decimal?),
			typeof(double), typeof(double?),
			typeof(float), typeof(float?)
		];

		BooleanTypes = [typeof(bool), typeof(bool?)];
		CharTypes = [typeof(char), typeof(char?)];
		DateTypes =
		[
			#if !NETSTANDARD
			typeof(DateOnly), typeof(DateOnly?),
			#endif
			typeof(DateTime), typeof(DateTime?),
			typeof(DateTimeOffset), typeof(DateTimeOffset?),
			typeof(IsoDateTime), typeof(IsoDateTime?),
			typeof(OscTimeTag), typeof(OscTimeTag?)
		];
		GuidTypes =
		[
			typeof(Guid), typeof(Guid?),
			typeof(ShortGuid), typeof(ShortGuid?)
		];
		NumberTypes = NonDecimalNumberTypes.Combine(DecimalNumberTypes);
		StringTypes =
		[
			typeof(string),
			typeof(StringBuilder),
			typeof(TextBuilder),
			typeof(GapBuffer<char>),
			typeof(RopeBuffer<char>),
			typeof(JsonString)
		];
		TimeTypes =
		[
			#if !NETSTANDARD
			typeof(TimeOnly), typeof(TimeOnly?),
			#endif
			typeof(TimeSpan), typeof(TimeSpan?)
		];
		JsonValueTypes =
		[
			typeof(JsonArray), typeof(JsonBoolean), typeof(JsonNull),
			typeof(JsonNumber), typeof(JsonObject), typeof(JsonString)
		];

		AllTypes = ArrayExtensions.CombineArrays(
			BooleanTypes, NumberTypes, CharTypes,
			StringTypes, DateTypes, TimeTypes, GuidTypes
		);

		_customActivators = new();
		_builtInActivators = new();

		RegisterBuiltInActivator(
			new StringTypeActivator()
		);
	}

	#endregion

	#region Properties

	/// <summary>
	/// All types known by the Activator.
	/// </summary>
	public static Type[] AllTypes { get; }

	/// <summary>
	/// All types for Boolean. Types supported are the following: bool, bool?
	/// </summary>
	public static Type[] BooleanTypes { get; }

	/// <summary>
	/// All types for Char. Types supported are the following: char, char?
	/// </summary>
	public static Type[] CharTypes { get; }

	/// <summary>
	/// All types for dates (time). Types supported are the following:
	/// DateTime, DateTimeOffset
	/// </summary>
	public static Type[] DateTypes { get; }

	/// <summary>
	/// All types for decimal numbers. Types supported are the following:
	/// decimal, float, double
	/// </summary>
	public static Type[] DecimalNumberTypes { get; }

	/// <summary>
	/// All types for Guid. Types supported are the following:
	/// Guid, ShortGuid
	/// </summary>
	public static Type[] GuidTypes { get; }

	/// <summary>
	/// All types for JSON parser values.
	/// </summary>
	public static Type[] JsonValueTypes { get; }

	/// <summary>
	/// All types for non-decimal numbers. Types supported are the following:
	/// byte, short, ushort, int, uint, long, ulong
	/// </summary>
	public static Type[] NonDecimalNumberTypes { get; }

	/// <summary>
	/// All types for numbers. Types supported are the following:
	/// byte, sbyte, short, ushort, int, uint, long, ulong, decimal, float, double
	/// </summary>
	public static Type[] NumberTypes { get; }

	/// <summary>
	/// All types for strings. Types supported are the following: string, StringBuilder, TextBuilder
	/// </summary>
	public static Type[] StringTypes { get; }

	/// <summary>
	/// All types for dates (time). Types supported are the following:
	/// TimeOnly, TimeSpan
	/// </summary>
	public static Type[] TimeTypes { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Create an instance for a given Type.
	/// </summary>
	/// <param name="arguments"> The value of the arguments. </param>
	/// <returns> The new instances of the type. </returns>
	public static T CreateInstance<T>(params object[] arguments)
	{
		return (T) CreateInstance(typeof(T), arguments);
	}

	/// <summary>
	/// Create an instance for a given Type.
	/// </summary>
	/// <param name="type"> The Type for which to get an instance of. </param>
	/// <param name="arguments"> The value of the arguments. </param>
	/// <returns> The new instances of the type. </returns>
	public static object CreateInstance(this Type type, params object[] arguments)
	{
		return CreateInstanceInternal(true, type, arguments);
	}

	/// <summary>
	/// Create an instance for a given Type.
	/// </summary>
	/// <param name="type"> The Type for which to get an instance of. </param>
	/// <param name="generics"> The Types the generic is for. </param>
	/// <param name="arguments"> The value of the arguments. </param>
	/// <returns> The new instances of the type. </returns>
	public static object CreateInstanceOfGeneric(this Type type, Type[] generics, params object[] arguments)
	{
		var genericType = type.GetCachedMakeGenericType(generics);
		return CreateInstanceOfGeneric(genericType, arguments);
	}

	/// <summary>
	/// Create an instance for a given Type.
	/// </summary>
	/// <param name="type"> The Type for which to get an instance of. </param>
	/// <param name="arguments"> The value of the arguments. </param>
	/// <returns> The new instances of the type. </returns>
	public static object CreateInstanceOfGeneric(this Type type, params object[] arguments)
	{
		if (!type.IsGenericType || type.IsGenericTypeDefinition)
		{
			throw new ArgumentException("The type provided is not a generic type or is a generic type definition.");
		}

		//
		// todo: move these to activators?
		//
		if ((type.GetGenericTypeDefinition() == typeof(ICollection<>))
			|| (type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
			|| (type.GetGenericTypeDefinition() == typeof(IList<>)))
		{
			type = typeof(List<>).GetCachedMakeGenericType(type.GenericTypeArguments);
		}
		if (type.GetGenericTypeDefinition() == typeof(ISet<>))
		{
			type = typeof(HashSet<>).GetCachedMakeGenericType(type.GenericTypeArguments);
		}
		#if !NETSTANDARD
		else if (type.GetGenericTypeDefinition() == typeof(IReadOnlySet<>))
		{
			type = typeof(ReadOnlySet<>).GetCachedMakeGenericType(type.GenericTypeArguments);
		}
		#endif
		else if (type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
		{
			type = typeof(Dictionary<,>).GetCachedMakeGenericType(type.GenericTypeArguments);
		}
		else if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			type = typeof(Nullable<>).GetCachedMakeGenericType(type.GenericTypeArguments);
		}

		return arguments is { Length: > 0 }
			? System.Activator.CreateInstance(type, arguments)
			: System.Activator.CreateInstance(type);
	}

	/// <summary>
	/// </summary>
	/// <param name="type"> </param>
	/// <returns> </returns>
	public static bool IsNet7OrGreater(Type type)
	{
		var types = new List<Type>();

		#if NET6_0_OR_GREATER
		types.AddRange(
			typeof(DateOnly),
			typeof(DateOnly?),
			typeof(TimeOnly),
			typeof(TimeOnly?)
		);
		#endif

		#if NET7_0_OR_GREATER
		types.AddRange(
			typeof(Int128),
			typeof(Int128?),
			typeof(UInt128),
			typeof(UInt128?)
		);
		#endif

		return types.Contains(type);
	}

	/// <summary>
	/// Register a custom type activator.
	/// </summary>
	/// <typeparam name="T"> The type the factory is for. </typeparam>
	/// <param name="factory"> The factory to create the type. </param>
	public static void RegisterType<T>(Func<object[], T> factory)
	{
		_customActivators.AddOrUpdate(typeof(T), new TypeActivator<T>(factory));
	}

	/// <summary>
	/// Register a type activator.
	/// </summary>
	/// <param name="activators"> The activators to provide type creation. </param>
	public static void RegisterTypeActivator(params TypeActivator[] activators)
	{
		foreach (var activator in activators)
		{
			_customActivators.GetOrAdd(activator.Type, _ => activator);
		}
	}

	/// <summary>
	/// Reset the type activators.
	/// </summary>
	public static void ResetTypeActivators()
	{
		_customActivators.Clear();
	}

	/// <summary>
	/// Create an instance for a given Type.
	/// </summary>
	/// <param name="useCustomActivators"> True if we can use custom activators. </param>
	/// <param name="type"> The Type for which to get an instance of. </param>
	/// <param name="arguments"> The value of the arguments. </param>
	/// <returns> The new instances of the type. </returns>
	internal static object CreateInstanceInternal(bool useCustomActivators, Type type, params object[] arguments)
	{
		object response;

		try
		{
			if (useCustomActivators && _customActivators.TryGetValue(type, out var activator))
			{
				response = activator.CreateInstanceObject(arguments);
			}
			else if (_builtInActivators.TryGetValue(type, out activator))
			{
				response = activator.CreateInstanceObject(arguments);
			}
			else if (type.IsArray)
			{
				var count = (arguments?.FirstOrDefault() ?? 0).ConvertTo<int>();
				response = Array.CreateInstance(type.GetElementType() ?? type, count);
			}
			// todo: when would you want to do this?
			//else if (type.IsGenericTypeDefinition)
			//{
			//	response = CreateInstanceOfGeneric(type, type.GenericTypeArguments, arguments);
			//}
			else if (type.IsGenericType)
			{
				response = CreateInstanceOfGeneric(type, arguments);
			}
			else
			{
				response = arguments is { Length: > 0 }
					? System.Activator.CreateInstance(type, arguments)
					: System.Activator.CreateInstance(type);
			}
		}
		catch (MissingMethodException ex)
		{
			// Add a bit more information.
			throw new MissingMethodException($"{type.FullName} missing requested constructor.", ex);
		}

		return response;
	}

	/// <summary>
	/// Register a built-in (Cornerstone) type activator.
	/// </summary>
	/// <param name="activators"> The activators to provide type creation. </param>
	private static void RegisterBuiltInActivator(params TypeActivator[] activators)
	{
		foreach (var activator in activators)
		{
			_builtInActivators.GetOrAdd(activator.Type, _ => activator);
		}
	}

	#endregion
}