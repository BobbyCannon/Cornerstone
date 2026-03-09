#region References

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for enumerations
/// </summary>
public static class EnumExtensions
{
	#region Methods

	/// <summary>
	/// Clear the "flagged" enum value.
	/// </summary>
	/// <typeparam name="T"> The type of the enum value. </typeparam>
	/// <param name="value"> The value to update. </param>
	/// <param name="flag"> The flag to be cleared. </param>
	/// <returns> The value with the flagged cleared. </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T ClearFlag<T>(this T value, T flag) where T : Enum
	{
		return value.UpdateFlag(flag, false);
	}

	/// <summary>
	/// Gets the type array of the values flagged (set) in the enum.
	/// </summary>
	/// <typeparam name="T"> The enum type. </typeparam>
	/// <param name="value"> The enum value to get the flagged values for. </param>
	/// <returns> The individual values for the enum. </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T[] GetFlagValues<T>(this T value) where T : Enum
	{
		var values = GetFlagValues<T>();
		return values.Where(x => value?.HasFlag(x) == true).ToArray();
	}

	/// <summary>
	/// Gets the type array of the values in the enum.
	/// </summary>
	/// <typeparam name="T"> The enum type. </typeparam>
	/// <returns> The individual values for the enum. </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T[] GetFlagValues<T>() where T : Enum
	{
		return typeof(T).GetFlagValues().Cast<T>().ToArray();
	}

	/// <summary>
	/// Gets the type array of the values in the enum.
	/// </summary>
	/// <param name="type"> The enum type. </param>
	/// <returns> The individual values for the enum. </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static object[] GetFlagValues(this Type type)
	{
		return SourceReflector
			.GetEnumValues(type).Where(x => ((Enum) x).IsSingleFlag()).ToArray();
	}

	/// <summary>
	/// Check the value to see if all flags are set.
	/// </summary>
	/// <typeparam name="T"> The type of the enum value. </typeparam>
	/// <param name="value"> The value to check. </param>
	/// <param name="flags"> The flags to check for. </param>
	/// <returns> True if the value has all flag provided otherwise false. </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool HasAllFlags<T>(this T value, params T[] flags) where T : Enum
	{
		return flags.All(x => value.HasFlag(x));
	}

	/// <summary>
	/// Check the value to see if any flag is set.
	/// </summary>
	/// <typeparam name="T"> The type of the enum value. </typeparam>
	/// <param name="value"> The value to check. </param>
	/// <param name="flags"> The flags to check for. </param>
	/// <returns> True if the value has any flag provided otherwise false. </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool HasAnyFlag<T>(this T value, params T[] flags) where T : Enum
	{
		return flags.Any(x => value.HasFlag(x));
	}

	/// <summary>
	/// Returns true if the enum is flagged and is a single value.
	/// </summary>
	/// <param name="value"> The value to test. </param>
	/// <returns> True if only one bit is flagged in the enum. </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsSingleFlag(this Enum value)
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
	/// Set the flagged enum value.
	/// </summary>
	/// <typeparam name="T"> The type of the enum value. </typeparam>
	/// <param name="value"> The value to update. </param>
	/// <param name="flag"> The flag to be set. </param>
	/// <returns> The value with the flagged set. </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T SetFlag<T>(this T value, T flag) where T : Enum
	{
		return value.UpdateFlag(flag, true);
	}

	/// <summary>
	/// Toggle the flagged enum value.
	/// </summary>
	/// <typeparam name="T"> The type of the enum value. </typeparam>
	/// <param name="value"> The value to update. </param>
	/// <param name="flags"> The flags to be set. </param>
	/// <returns> The value with the flagged set. </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T ToggleFlag<T>(this T value, T flags) where T : Enum
	{
		return ToEnum<T>(ToUInt64(value) ^ ToUInt64(flags));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T ToEnum<T>(ulong value) where T : Enum
	{
		return (T) Enum.ToObject(typeof(T), value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ulong ToUInt64<T>(T value)
	{
		return System.Convert.ToUInt64(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T UpdateFlag<T>(this T value, T flag, bool set) where T : Enum
	{
		return set
			? ToEnum<T>(ToUInt64(value) | ToUInt64(flag))
			: ToEnum<T>(ToUInt64(value) & ~ToUInt64(flag));
	}

	#endregion
}