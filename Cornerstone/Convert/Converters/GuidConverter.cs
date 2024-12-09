#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Convert.Converters;

/// <inheritdoc />
public class GuidConverter : BaseConverter
{
	#region Fields

	private static readonly Dictionary<string, Func<string, object>> _parsers;

	#endregion

	#region Constructors

	/// <inheritdoc />
	public GuidConverter() : base(
		new Guid("8EF8C526-2E42-42B5-AD08-B4C1C96D1586"),
		Activator.GuidTypes,
		ArrayExtensions.CombineArrays(
			Activator.GuidTypes,
			Activator.StringTypes
		))
	{
	}

	static GuidConverter()
	{
		_parsers = new Dictionary<string, Func<string, object>>
		{
			{ typeof(Guid?).FullName, x => ToGuid(x) },
			{ typeof(Guid).FullName, x => ToGuid(x) },
			{ typeof(ShortGuid?).FullName, x => ToShortGuid(x) },
			{ typeof(ShortGuid).FullName, x => ToShortGuid(x) }
		};
	}

	#endregion

	#region Methods

	/// <summary>
	/// Convert to Guid.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The converted value. </returns>
	public static Guid ToGuid(string value)
	{
		return Guid.TryParse(value, out var guid) ? guid : default;
	}

	/// <summary>
	/// Convert to ShortGuid.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <returns> The converted value. </returns>
	public static ShortGuid ToShortGuid(string value)
	{
		return ShortGuid.TryParse(value, out var guid) ? guid : default;
	}

	/// <inheritdoc />
	public override bool TryConvertTo(object from, Type fromType, Type toType, out object value, IConverterSettings settings = null)
	{
		if (from == null)
		{
			value = null;
			return toType.IsNullable();
		}

		if (Activator.StringTypes.Contains(fromType))
		{
			var response = _parsers.TryGetValue(toType.FullName, out var parser);
			value = parser?.Invoke(from.ToString());
			return response;
		}

		Guid? fromGuid = from switch
		{
			ShortGuid sGuid => sGuid.Guid,
			Guid guid => guid,
			_ => null
		};

		if (fromGuid == null)
		{
			// from value was not a Guid
			return base.TryConvertTo(from, fromType, toType, out value, settings);
		}

		if ((toType == typeof(Guid)) || (toType == typeof(Guid?)))
		{
			value = fromGuid;
			return true;
		}

		if ((toType == typeof(ShortGuid)) || (toType == typeof(ShortGuid?)))
		{
			value = new ShortGuid(fromGuid.Value);
			return true;
		}

		if (Activator.StringTypes.Contains(toType))
		{
			value = toType.CreateInstance(fromGuid.ToString());
			return true;
		}

		return base.TryConvertTo(from, fromType, toType, out value, settings);
	}

	/// <summary>
	/// Try to convert the value to a Guid.
	/// </summary>
	/// <param name="value"> The value to convert. </param>
	/// <param name="guid"> The value if successful. </param>
	/// <returns> True if successful otherwise false. </returns>
	public static bool TryToGuid(object value, out Guid guid)
	{
		return Guid.TryParse(value?.ToString(), out guid);
	}

	#endregion
}