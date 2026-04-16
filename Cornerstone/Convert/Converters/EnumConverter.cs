#region References

using System;
using Cornerstone.Extensions;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Convert.Converters;

public class EnumConverter : BaseConverter
{
	#region Constructors

	public EnumConverter() : base(new Guid("648ACB39-AAF1-40BC-A60F-460FF829C10A"))
	{
	}

	#endregion

	#region Methods

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
			|| (fromType.IsEnum && SourceTypes.NumberTypes.Contains(toType))
			|| (toType.IsEnum && SourceTypes.NumberTypes.Contains(fromType))
			|| (fromType.IsEnum && SourceTypes.StringTypes.Contains(toType))
			|| (toType.IsEnum && SourceTypes.StringTypes.Contains(fromType));
	}

	public override bool TryConvertTo(object from, Type fromType, Type toType, out object value, IConverterSettings settings = null)
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
			if (SourceTypes.StringTypes.Contains(toType))
			{
				var enumString = settings?.EnumFormat == EnumFormat.Value
					? ((Enum) from).ToString("D")
					: ((Enum) from).ToString();
				value = SourceReflector.CreateInstance(toType, enumString);
				return true;
			}

			value = ((Enum) from).ToString("D").ConvertTo(toType);
			return true;
		}

		// Some other types to enum...

		if (SourceTypes.NumberTypes.Contains(fromType)
			|| SourceTypes.StringTypes.Contains(fromType))
		{
			return Enum.TryParse(toType, from.ToString(), out value);
		}

		return base.TryConvertTo(from, fromType, toType, out value, settings);
	}

	#endregion
}