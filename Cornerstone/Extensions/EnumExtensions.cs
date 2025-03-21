#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Cornerstone.Convert;
using Cornerstone.Generators;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for enumerations
/// </summary>
public static class EnumExtensions
{
	#region Fields

	private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<Enum, EnumDetails>> _cache;

	#endregion

	#region Constructors

	static EnumExtensions()
	{
		_cache = new ConcurrentDictionary<Type, IReadOnlyDictionary<Enum, EnumDetails>>();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Clear the "flagged" enum value.
	/// </summary>
	/// <typeparam name="T"> The type of the enum value. </typeparam>
	/// <param name="value"> The value to update. </param>
	/// <param name="flag"> The flag to be cleared. </param>
	/// <returns> The value with the flagged cleared. </returns>
	public static T ClearFlag<T>(this T value, T flag) where T : Enum
	{
		return value.UpdateFlag(flag, false);
	}

	/// <summary>
	/// Returns the number of values for the provided enum.
	/// </summary>
	/// <returns> The number of values in the enum. </returns>
	public static int Count<T>() where T : Enum
	{
		return GetAllEnumDetails(typeof(T)).Count;
	}

	/// <summary>
	/// Returns the number of values for the provided enum.
	/// </summary>
	/// <param name="value"> The enum value to count. </param>
	/// <returns> The number of values in the enum. </returns>
	public static int Count(this Enum value)
	{
		var valueType = value.GetType();
		var isFlaggedEnum = valueType.IsFlaggedEnum();
		var details = GetAllEnumDetails(valueType);
		return isFlaggedEnum ? details.Count(x => x.Value.IsFlaggedValue) : details.Count;
	}

	/// <summary>
	/// Gets the all details for an enum value.
	/// </summary>
	/// <returns> The all details for the enum value. </returns>
	public static IReadOnlyDictionary<T, EnumDetails> GetAllEnumDetails<T>() where T : Enum
	{
		return GetAllEnumDetails(typeof(T)).ToDictionary(x => (T) x.Key, x => x.Value);
	}

	/// <summary>
	/// Gets the all details for an enum value.
	/// </summary>
	/// <param name="type"> The type to process. </param>
	/// <returns> The all details for the enum value. </returns>
	public static IReadOnlyDictionary<Enum, EnumDetails> GetAllEnumDetails(Type type)
	{
		return _cache.GetOrAdd(type, x =>
		{
			var isFlaggedType = type.IsFlaggedEnum();
			var enumValues = Enum.GetValues(type);
			var response = new Dictionary<Enum, EnumDetails>();

			foreach (Enum enumValue in enumValues)
			{
				if (response.ContainsKey(enumValue))
				{
					continue;
				}

				var memberInfo = x.GetMember(enumValue.ToString()).FirstOrDefault();
				var descriptionAttribute = memberInfo?.GetCustomAttribute<DescriptionAttribute>();
				var displayAttribute = memberInfo?.GetCustomAttribute<DisplayAttribute>();
				var details = new EnumDetails
				{
					Description = displayAttribute?.Description
						?? descriptionAttribute?.Description
						?? enumValue.ToString(),
					DisplayOrder = displayAttribute?.GetOrder() ?? 0,
					GroupName = displayAttribute?.GetGroupName() ?? string.Empty,
					IsFlaggedValue = isFlaggedType && enumValue.IsSingleFlagEnum(),
					Name = displayAttribute?.Name ?? enumValue.ToString(),
					NumericValue = enumValue.ConvertTo(type.GetEnumUnderlyingType()),
					ShortName = displayAttribute?.ShortName ?? enumValue.ToString(),
					Value = enumValue
				};
				response.Add(enumValue, details);
			}

			return response;
		});
	}

	/// <summary>
	/// Gets the all details for an enum value except the excluded.
	/// </summary>
	/// <param name="exclusions"> The types to be excluded. </param>
	/// <returns> The all details for the enum value except the exclusions. </returns>
	public static IReadOnlyDictionary<T, EnumDetails> GetAllEnumDetailsExcept<T>(params T[] exclusions) where T : Enum
	{
		return GetAllEnumDetails(typeof(T))
			.Where(x => !exclusions.Contains((T) x.Key))
			.ToDictionary(x => (T) x.Key, x => x.Value);
	}

	/// <summary>
	/// Gets an attribute from an enum value.
	/// </summary>
	/// <param name="value"> The value to get the attribute from. </param>
	/// <typeparam name="TEnum"> The type of enum. </typeparam>
	/// <typeparam name="TAttribute"> The type of attribute. </typeparam>
	/// <returns> The attribute if found otherwise null. </returns>
	public static TAttribute GetAttribute<TEnum, TAttribute>(this TEnum value)
		where TEnum : Enum
		where TAttribute : Attribute
	{
		var memberInfo = typeof(TEnum).GetMember(value.ToString()).FirstOrDefault();
		return memberInfo.GetCustomAttribute<TAttribute>();
	}

	/// <summary>
	/// Gets the description.
	/// </summary>
	/// <param name="value"> The enum value to get the description for. </param>
	/// <returns> The description of the value. </returns>
	public static string GetDescription(this Enum value)
	{
		return GetEnumDetails(value).Description;
	}

	/// <summary>
	/// Gets the description.
	/// </summary>
	/// <param name="value"> The enum value to get the description for. </param>
	/// <returns> The description of the value. </returns>
	public static string GetDescription<T>(this T value) where T : Enum
	{
		return GetEnumDetails(value).Description;
	}

	/// <summary>
	/// Gets the name.
	/// </summary>
	/// <param name="value"> The enum value to get the name for. </param>
	/// <returns> The name of the value. </returns>
	public static string GetDisplayName(this Enum value)
	{
		return GetEnumDetails(value).Name;
	}

	/// <summary>
	/// Gets the name.
	/// </summary>
	/// <param name="value"> The enum value to get the name for. </param>
	/// <returns> The name of the value. </returns>
	public static string GetDisplayName<T>(this T value) where T : Enum
	{
		return GetEnumDetails(value).Name;
	}

	/// <summary>
	/// Gets the display names.
	/// </summary>
	/// <returns> The display names of the enum type. </returns>
	public static string[] GetDisplayNames<T>() where T : Enum
	{
		return GetAllEnumDetails<T>().Select(x => x.Value.Name).ToArray();
	}

	/// <summary>
	/// Gets the display names excluding the provided values.
	/// </summary>
	/// <param name="exclusions"> An optional set of enums to exclude. </param>
	/// <returns> The display names of the enum type. </returns>
	public static string[] GetDisplayNamesExcept<T>(params T[] exclusions) where T : Enum
	{
		return GetAllEnumDetails<T>()
			.Where(x => !exclusions.Contains(x.Key))
			.Select(x => x.Value.Name)
			.ToArray();
	}

	/// <summary>
	/// Gets the display short name.
	/// </summary>
	/// <param name="value"> The enum value to get the short name for. </param>
	/// <returns> The name of the value. </returns>
	public static string GetDisplayShortName(this Enum value)
	{
		return GetEnumDetails(value).ShortName;
	}

	/// <summary>
	/// Gets the short name.
	/// </summary>
	/// <param name="value"> The enum value to get the short name for. </param>
	/// <returns> The short name of the value. </returns>
	public static string GetDisplayShortName<T>(this T value) where T : Enum
	{
		return GetEnumDetails(value).ShortName;
	}

	/// <summary>
	/// Gets the details for an enum value.
	/// </summary>
	/// <param name="value"> The value to process. </param>
	/// <returns> The details for the enum value. </returns>
	public static EnumDetails GetEnumDetails(this Enum value)
	{
		var valueType = value.GetType();
		var allDetails = GetAllEnumDetails(valueType);
		return allDetails.TryGetValue(value, out var detail)
			? detail
			: new EnumDetails
			{
				Description = value.ToString(),
				Name = value.ToString(),
				ShortName = value.ToString(),
				Value = value,
				NumericValue = value.ConvertTo(valueType.GetEnumUnderlyingType())
			};
	}

	/// <summary>
	/// Get a name to value lookup table for an enum type.
	/// </summary>
	/// <param name="enumType"> The enum to get the lookup table for. </param>
	/// <returns> The lookup table. </returns>
	public static IReadOnlyDictionary<string, Enum> GetEnumLookupTableByName(Type enumType)
	{
		if (enumType.IsNullableType())
		{
			enumType = enumType.FromNullableType();
		}

		var allDetails = GetAllEnumDetails(enumType);
		var response = allDetails.ToDictionary(x => x.Value.Name, x => x.Value.Value, StringComparer.OrdinalIgnoreCase);

		foreach (var detail in allDetails)
		{
			response.Add(detail.Value.NumericValue.ToString(), detail.Key);
		}

		return response;
	}

	/// <summary>
	/// Get all enum values with an optional set of exclusions.
	/// </summary>
	/// <returns> The enum value except the exclusions. </returns>
	public static HashSet<object> GetEnumValues(this Type enumType)
	{
		return Enum
			.GetValues(enumType)
			.Cast<object>()
			.ToHashSet();
	}

	/// <summary>
	/// Get all enum values with an optional set of exclusions.
	/// </summary>
	/// <returns> The enum value except the exclusions. </returns>
	public static HashSet<T> GetEnumValues<T>(params T[] except) where T : Enum
	{
		return Enum.GetValues(typeof(T))
			.Cast<T>()
			.Except(except)
			.ToHashSet();
	}

	/// <summary>
	/// Gets the type array of the values flagged (set) in the enum.
	/// </summary>
	/// <typeparam name="T"> The enum type. </typeparam>
	/// <param name="value"> The enum value to get the flagged values for. </param>
	/// <returns> The individual values for the enum. </returns>
	public static T[] GetFlaggedValues<T>(this T value) where T : Enum
	{
		var values = GetFlagValues<T>();
		return values.Where(x => value?.HasFlag(x) == true).ToArray();
	}

	/// <summary>
	/// Gets the type array of the values in the enum.
	/// </summary>
	/// <typeparam name="T"> The enum type. </typeparam>
	/// <returns> The individual values for the enum. </returns>
	public static T[] GetFlagValues<T>() where T : Enum
	{
		return GetFlagValues(typeof(T))
			.Cast<T>()
			.ToArray();
	}

	/// <summary>
	/// Gets the type array of the values in the enum.
	/// </summary>
	/// <param name="enumValue"> The enum type. </param>
	/// <returns> The individual values for the enum. </returns>
	public static object[] GetFlagValues(this Type enumValue)
	{
		return Enum
			.GetValues(enumValue)
			.Cast<object>()
			.Where(v =>
			{
				// because enums can be UInt64
				var x = v.ConvertTo<ulong>();
				return (x != 0) && ((x & (x - 1)) == 0);
				// Checks whether x is a power of 2
				// Example: when x = 16, the binary values are:
				// x:         10000
				// x-1:       01111
				// x & (x-1): 00000
			})
			.ToArray();
	}

	/// <summary>
	/// Gets the type array of the values in the enum.
	/// </summary>
	/// <typeparam name="T"> The enum type. </typeparam>
	/// <returns> The individual values for the enum. </returns>
	public static T[] GetValues<T>() where T : Enum
	{
		return typeof(T).GetEnumValues().Cast<T>().ToArray();
	}

	/// <summary>
	/// Check the value to see if any flag is set.
	/// </summary>
	/// <typeparam name="T"> The type of the enum value. </typeparam>
	/// <param name="value"> The value to check. </param>
	/// <param name="flags"> The flags to check for. </param>
	/// <returns> True if the value has any flag provided otherwise false. </returns>
	public static bool HasAnyFlag<T>(this T value, params T[] flags) where T : Enum
	{
		return flags.Any(x => value.HasFlag(x));
	}

	/// <summary>
	/// Returns true if the enum is flagged and is a single value.
	/// </summary>
	/// <param name="value"> The value to test. </param>
	/// <returns> True if only one bit is flagged in the enum. </returns>
	public static bool IsSingleFlagEnum(this Enum value)
	{
		var nValue = System.Convert.ToUInt64(value);
		if (nValue == 0)
		{
			return false;
		}
		var rValue = nValue & (nValue - 1);
		return rValue == 0;
	}

	/// <summary>
	/// Set the "flagged" enum value.
	/// </summary>
	/// <typeparam name="T"> The type of the enum value. </typeparam>
	/// <param name="value"> The value to update. </param>
	/// <param name="flag"> The flag to be set. </param>
	/// <returns> The value with the flagged set. </returns>
	public static T SetFlag<T>(this T value, T flag) where T : Enum
	{
		return value.UpdateFlag(flag, true);
	}

	private static T UpdateFlag<T>(this T value, T flag, bool set) where T : Enum
	{
		var eValue = System.Convert.ToUInt64(value);
		var eFlag = System.Convert.ToUInt64(flag);
		var fValue = set ? eValue | eFlag : eValue & ~eFlag;
		return (T) Enum.ToObject(typeof(T), fValue);
	}

	#endregion

	#region Structures

	/// <summary>
	/// Represents the details for an enum value.
	/// </summary>
	public struct EnumDetails : ICodeStringProvider
	{
		#region Properties

		/// <summary>
		/// The description of the enum value.
		/// </summary>
		/// <remarks>
		/// Priority is [DisplayAttribute].Description, [DescriptionAttribute].Description, enum.ToString()
		/// </remarks>
		public string Description { get; set; }

		/// <summary>
		/// The display order of the values.
		/// </summary>
		public int DisplayOrder { get; set; }

		/// <summary>
		/// An optional group name.
		/// </summary>
		public string GroupName { get; set; }

		/// <summary>
		/// Is a single bit flagged enum value.
		/// </summary>
		public bool IsFlaggedValue { get; set; }

		/// <summary>
		/// The name of the enum value.
		/// </summary>
		/// <remarks>
		/// Priority is [DisplayAttribute].Name, enum.ToString()
		/// </remarks>
		public string Name { get; set; }

		/// <summary>
		/// Gets the numeric value of the enum. (int, uint, ulong, etc)
		/// </summary>
		public object NumericValue { get; set; }

		/// <summary>
		/// The short name of the enum value.
		/// </summary>
		/// <remarks>
		/// Priority is [DisplayAttribute].Short, enum.ToString()
		/// </remarks>
		public string ShortName { get; set; }

		/// <summary>
		/// The enum value.
		/// </summary>
		public Enum Value { get; set; }

		#endregion

		#region Methods

		/// <inheritdoc />
		public TextBuilder ToCodeString(bool asNullable = false, TextBuilder builder = null, CodeLanguage language = CodeLanguage.CSharp, CodeWriterSettings? settings = null)
		{
			builder ??= new TextBuilder();

			if (asNullable)
			{
				builder.Append("(EnumDetails?) ");
			}

			builder.Append("new EnumDetails { ");
			builder.Append($"{nameof(Value)} = {CSharpCodeWriter.GenerateCode(Value)}, ");
			builder.Append($"{nameof(Name)} = \"{Name}\", ");
			builder.Append($"{nameof(ShortName)} = \"{ShortName}\", ");
			builder.Append($"{nameof(Description)} = \"{Description}\" ");
			builder.Append($"{nameof(NumericValue)} = \"{NumericValue}\" ");
			builder.Append("}");
			return builder;
		}

		#endregion
	}

	#endregion
}