#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Cornerstone.Serialization.Json;

#endregion

namespace Cornerstone.Serialization;

public static class Serializer
{
	#region Fields

	private static readonly Type _enumerableType;

	#endregion

	#region Constructors

	static Serializer()
	{
		_enumerableType = typeof(IEnumerable<>);

		SerializationOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
			NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
			WriteIndented = false,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
			IgnoreReadOnlyFields = true,
			IgnoreReadOnlyProperties = true,
			TypeInfoResolver = JsonTypeInfoResolver.Combine(
				CornerstoneJsonSerializerContext.Default,
				new DefaultJsonTypeInfoResolver()
			)
		};
		SerializationOptions.Converters.Add(new JsonConverterFactoryPartialUpdate());
		SerializationOptions.Converters.Add(new JsonConverterFactoryPresentationList());
	}

	#endregion

	#region Properties

	public static JsonSerializerOptions SerializationOptions { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Adds one or more additional TypeInfoResolvers to the chain.
	/// They will be queried after the existing ones.
	/// </summary>
	public static void AddTypeInfoResolvers(params IJsonTypeInfoResolver[] resolvers)
	{
		if ((resolvers == null) || (resolvers.Length == 0))
		{
			return;
		}

		// TypeInfoResolverChain is a live IList that stays in sync with TypeInfoResolver
		foreach (var resolver in resolvers)
		{
			if (resolver != null)
			{
				SerializationOptions.TypeInfoResolverChain.Add(resolver);
			}
		}
	}

	public static T FromJson<T>(this string value)
	{
		return JsonSerializer.Deserialize<T>(value, SerializationOptions);
	}

	public static void Lock()
	{
		SerializationOptions.MakeReadOnly();
	}

	public static string ToJson<T>(this T value)
	{
		return JsonSerializer.Serialize(value, SerializationOptions);
	}

	internal static Type GetArrayType(Type type)
	{
		if (type.IsArray)
		{
			return type.GetElementType();
		}

		if (type.IsGenericType)
		{
			return type.GetGenericArguments()[0];
		}

		var enumerableType = type
			.GetInterfaces().FirstOrDefault(i => i.IsGenericType
				&& (i.GetGenericTypeDefinition() == _enumerableType)
			);

		return enumerableType?.GetGenericArguments()[0];
	}

	#endregion
}