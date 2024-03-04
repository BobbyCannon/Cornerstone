#region References

using System;
using System.Linq;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Convert.Converters;

/// <inheritdoc />
public class EnumConverter : BaseConverter
{
	#region Constructors

	/// <inheritdoc />
	public EnumConverter() : base(new Guid("648ACB39-AAF1-40BC-A60F-460FF829C10A"))
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override bool CanConvert(Type fromType, Type toType)
	{
		if (fromType.IsNullableType())
		{
			fromType = fromType.FromNullableType();
		}

		if (toType.IsNullableType())
		{
			toType = toType.FromNullableType();
		}

		return (fromType.IsEnum && toType.IsEnum)
			|| (fromType.IsEnum && Activator.NumberTypes.Contains(toType))
			|| (toType.IsEnum && Activator.NumberTypes.Contains(fromType))
			|| (fromType.IsEnum && Activator.StringTypes.Contains(toType))
			|| (toType.IsEnum && Activator.StringTypes.Contains(fromType));
	}

	/// <inheritdoc />
	public override bool TryConvertTo(object from, Type fromType, Type toType, out object value, IConverterOptions options = null)
	{
		if (from == null)
		{
			value = null;
			return toType.IsNullable();
		}

		if (fromType.IsNullableType())
		{
			fromType = fromType.FromNullableType();
		}

		if (toType.IsNullableType())
		{
			toType = toType.FromNullableType();
		}

		// From Enum to other types
		if (fromType.IsEnum)
		{
			if (Activator.StringTypes.Contains(toType))
			{
				var enumString = options?.EnumFormat == EnumFormat.Value
					? ((Enum) from).ToString("D")
					: ((Enum) from).ToString();
				value = toType.CreateInstance(enumString);
				return true;
			}

			value = ((Enum) from).ToString("D").ConvertTo(toType);
			return true;
		}

		// Some other types to enum...

		if (Activator.NumberTypes.Contains(fromType))
		{
			#if (NETSTANDARD)
			try
			{
				value = Enum.Parse(toType, from.ToString());
				return true;
			}
			catch
			{
				value = null;
				return false;
			}
			#else
			return Enum.TryParse(toType, from.ToString(), out value);
			#endif
		}
		if (Activator.StringTypes.Contains(fromType))
		{
			var fromString = from.ToString();
			var details = EnumExtensions.GetEnumLookupTableByName(toType);
			if (details.TryGetValue(fromString, out var eValue))
			{
				value = eValue;
				return true;
			}
		}

		return base.TryConvertTo(from, fromType, toType, out value, options);
	}

	#endregion
}