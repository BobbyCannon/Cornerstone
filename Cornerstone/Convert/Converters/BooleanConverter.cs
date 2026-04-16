#region References

using System;
using System.Collections.Generic;
using System.Text;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Convert.Converters;

public class BooleanConverter : BaseConverter
{
	#region Fields

	private static readonly Dictionary<string, Func<string, object>> _parsers;

	#endregion

	#region Constructors

	public BooleanConverter() : base(
		new Guid("942463B3-B95D-4D4B-8378-2B190452CBDC"),
		SourceTypes.BooleanTypes,
		ArrayExtensions.CombineArrays(
			SourceTypes.BooleanTypes,
			SourceTypes.StringTypes
		))
	{
	}

	static BooleanConverter()
	{
		_parsers = new Dictionary<string, Func<string, object>>
		{
			{ typeof(bool?).FullName, x => bool.Parse(x) },
			{ typeof(bool).FullName, x => bool.Parse(x) }
		};
	}

	#endregion

	#region Methods

	public override bool TryConvertTo(object from, Type fromType, Type toType, out object value, IConverterSettings settings = null)
	{
		if (from == null)
		{
			value = null;
			return toType.IsNullable();
		}

		if (SourceTypes.BooleanTypes.Contains(fromType)
			&& SourceTypes.BooleanTypes.Contains(toType))
		{
			value = from;
			return true;
		}

		if (SourceTypes.StringTypes.Contains(fromType))
		{
			var response = _parsers.TryGetValue(toType.FullName, out var parser);
			value = parser?.Invoke(from.ToString());
			return response;
		}

		if (SourceTypes.StringTypes.Contains(toType))
		{
			if (toType == typeof(string))
			{
				value = from.ToString();
				return true;
			}

			if ((toType == typeof(TextService))
				|| (toType == typeof(StringBuilder))
				|| (toType == typeof(GapBuffer<char>)))
			{
				value = SourceReflector.CreateInstance(toType, from.ToString());
				return true;
			}
		}

		return base.TryConvertTo(from, fromType, toType, out value, settings);
	}

	#endregion
}