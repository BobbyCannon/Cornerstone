#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Cornerstone.Serialization.Json;
using Cornerstone.Sync;

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
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
			DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			IgnoreReadOnlyFields = true,
			IgnoreReadOnlyProperties = true,
			NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			PropertyNameCaseInsensitive = true,
			WriteIndented = false
		};
		SerializationOptions.Converters.Add(new JsonConverterFactoryPartialUpdate());
		SerializationOptions.Converters.Add(new JsonConverterFactoryPresentationList());
		SerializationOptions.TypeInfoResolverChain.Add(CornerstoneJsonSerializerContext.Default);
		SerializationOptions.TypeInfoResolverChain.Add(SyncSerializerContext.Default);
		SerializationOptions.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());
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
	/// <remarks>
	/// Call during app startup <b> before </b> the first serialize/deserialize and before
	/// caching results of <see cref="CreateOptions" /> so forked options include the same resolvers.
	/// After options have been used (or <see cref="Lock" />), the chain cannot be modified.
	/// </remarks>
	public static void AddTypeInfoResolvers(params IJsonTypeInfoResolver[] resolvers)
	{
		if ((resolvers == null) || (resolvers.Length == 0))
		{
			return;
		}

		if (SerializationOptions.IsReadOnly)
		{
			throw new InvalidOperationException(
				"SerializationOptions is already read-only or has been used. " +
				"Call AddTypeInfoResolvers during startup before the first ToJson/FromJson.");
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

	/// <summary>
	/// Copies global serializer settings onto an existing options instance
	/// (e.g. ASP.NET Core <c> AddJsonOptions </c>).
	/// </summary>
	/// <remarks>
	/// Prefer <see cref="CreateOptions" /> when you need a forked bag you control.
	/// </remarks>
	public static void ApplyOptions(JsonSerializerOptions options)
	{
		if (options == null)
		{
			return;
		}

		// Copy constructor covers current and future JsonSerializerOptions properties;
		// we cannot replace the caller's instance, so assign each member from a clone.
		var source = new JsonSerializerOptions(SerializationOptions);

		options.AllowOutOfOrderMetadataProperties = source.AllowOutOfOrderMetadataProperties;
		options.DefaultIgnoreCondition = source.DefaultIgnoreCondition;
		options.DictionaryKeyPolicy = source.DictionaryKeyPolicy;
		options.Encoder = source.Encoder;
		options.IgnoreReadOnlyFields = source.IgnoreReadOnlyFields;
		options.IgnoreReadOnlyProperties = source.IgnoreReadOnlyProperties;
		options.MaxDepth = source.MaxDepth;
		options.NumberHandling = source.NumberHandling;
		options.PropertyNameCaseInsensitive = source.PropertyNameCaseInsensitive;
		options.PropertyNamingPolicy = source.PropertyNamingPolicy;
		options.ReadCommentHandling = source.ReadCommentHandling;
		options.ReferenceHandler = source.ReferenceHandler;
		options.UnknownTypeHandling = source.UnknownTypeHandling;
		options.WriteIndented = source.WriteIndented;
		options.PreferredObjectCreationHandling = source.PreferredObjectCreationHandling;
		options.TypeInfoResolver = source.TypeInfoResolver;

		options.Converters.Clear();
		foreach (var converter in source.Converters)
		{
			options.Converters.Add(converter);
		}
	}

	/// <summary>
	/// Creates a fork of <see cref="SerializationOptions" />. Mutate only the settings you need.
	/// </summary>
	/// <param name="configure"> Optional deltas (indent, naming policy, converters, etc). </param>
	/// <returns>
	/// A new options instance. Cache it in a static field when used on a hot path, do not call
	/// this on every serialize if the shape is stable.
	/// </returns>
	/// <remarks>
	/// <para>
	/// System.Text.Json caches contract metadata per options instance. Cloning once per
	/// configuration shape is cheap; cloning per call is not.
	/// </para>
	/// <para>
	/// Call after app <see cref="AddTypeInfoResolvers" /> so the clone includes those resolvers.
	/// Do not mutate <see cref="SerializationOptions" /> for one-offs.
	/// </para>
	/// <para>
	/// Source-generated <see cref="JsonSerializerContext" /> entries may bake property names
	/// at compile time; runtime <see cref="JsonSerializerOptions.PropertyNamingPolicy" /> may not
	/// rename those types. Prefer a plain DTO / reflection resolver or a dedicated context
	/// when you need a specific casing.
	/// </para>
	/// </remarks>
	public static JsonSerializerOptions CreateOptions(Action<JsonSerializerOptions> configure = null)
	{
		var options = new JsonSerializerOptions(SerializationOptions);
		configure?.Invoke(options);
		return options;
	}

	public static T FromJson<T>(this string value)
	{
		return JsonSerializer.Deserialize<T>(value, SerializationOptions);
	}

	public static T FromJson<T>(this string value, JsonSerializerOptions options)
	{
		return JsonSerializer.Deserialize<T>(value, options ?? SerializationOptions);
	}

	public static object FromJson(this string value, Type type)
	{
		return JsonSerializer.Deserialize(value, type, SerializationOptions);
	}

	public static object FromJson(this string value, Type type, JsonSerializerOptions options)
	{
		return JsonSerializer.Deserialize(value, type, options ?? SerializationOptions);
	}

	public static void Lock()
	{
		SerializationOptions.MakeReadOnly();
	}

	public static string ToJson<T>(this T value)
	{
		return JsonSerializer.Serialize(value, SerializationOptions);
	}

	/// <summary>
	/// Serialize using a specific options bag (from <see cref="CreateOptions" /> or custom).
	/// </summary>
	public static string ToJson<T>(this T value, JsonSerializerOptions options)
	{
		return JsonSerializer.Serialize(value, options ?? SerializationOptions);
	}

	/// <summary>
	/// Serialize directly to a file (UTF-8), avoiding a large intermediate string.
	/// </summary>
	/// <param name="path"> Destination path. </param>
	/// <param name="value"> Value to serialize. </param>
	/// <param name="options"> Options bag; null uses <see cref="SerializationOptions" />. </param>
	/// <param name="indented">
	/// When set, controls indentation on the writer (overrides <see cref="JsonSerializerOptions.WriteIndented" />
	/// for this write only). When null, uses <c> options.WriteIndented </c>.
	/// </param>
	public static void ToJsonFile<T>(string path, T value, JsonSerializerOptions options = null, bool? indented = null)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentException("Path cannot be empty.", nameof(path));
		}

		options ??= SerializationOptions;

		using var stream = File.Create(path);
		using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
		{
			Indented = indented ?? options.WriteIndented,
			Encoder = options.Encoder
		});
		JsonSerializer.Serialize(writer, value, options);
	}

	public static string ToRawJson<T>(this T value)
	{
		var bytes = JsonSerializer.SerializeToUtf8Bytes(value, SerializationOptions);
		return Encoding.UTF8.GetString(bytes);
	}

	public static string ToRawJson<T>(this T value, JsonSerializerOptions options)
	{
		var bytes = JsonSerializer.SerializeToUtf8Bytes(value, options ?? SerializationOptions);
		return Encoding.UTF8.GetString(bytes);
	}

	public static bool TryFromJson<T>(this string value, out T typeValue)
	{
		if (TryFromJson(value, typeof(T), out var valueObject))
		{
			typeValue = (T) valueObject;
			return true;
		}

		typeValue = default;
		return false;
	}

	public static bool TryFromJson(this string value, Type type, out object typeValue)
	{
		try
		{
			typeValue = JsonSerializer.Deserialize(value, type, SerializationOptions);
			return true;
		}
		catch
		{
			typeValue = null;
			return false;
		}
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