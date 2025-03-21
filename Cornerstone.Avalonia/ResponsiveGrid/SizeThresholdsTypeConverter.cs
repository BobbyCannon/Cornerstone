#region References

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

#endregion

namespace Cornerstone.Avalonia.ResponsiveGrid;

public class SizeThresholdsTypeConverter : TypeConverter
{
	#region Methods

	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
	{
		//return base.CanConvertFrom(context, sourceType);
		return sourceType == typeof(string);
	}

	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		var text = (string) value;
		var list = text.Split(',')
			.Select(o => o.Trim())
			.Select(o => int.Parse(o))
			.ToList();

		if (list.Count() != 3)
		{
			throw new ArgumentException($"'{value}' Invalid value. BreakPoints must contains 3 items.");
		}

		return new SizeThresholds { XSmallToSmall = list[0], SmallToMedium = list[1], MediumToLarge = list[2] };
	}

	#endregion
}