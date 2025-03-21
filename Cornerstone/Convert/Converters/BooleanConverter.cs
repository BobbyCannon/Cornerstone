#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Convert.Converters;

/// <inheritdoc />
public class BooleanConverter : BaseConverter
{
	#region Fields

	private static readonly Dictionary<string, Func<string, object>> _parsers;

	#endregion

	#region Constructors

	/// <inheritdoc />
	public BooleanConverter() : base(
		new Guid("942463B3-B95D-4D4B-8378-2B190452CBDC"),
		Activator.BooleanTypes,
		ArrayExtensions.CombineArrays(
			Activator.BooleanTypes,
			Activator.StringTypes
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

	/// <inheritdoc />
	public override bool TryConvertTo(object from, Type fromType, Type toType, out object value, IConverterSettings settings = null)
	{
		if (from == null)
		{
			value = null;
			return toType.IsNullable();
		}

		if (Activator.BooleanTypes.Contains(fromType)
			&& Activator.BooleanTypes.Contains(toType))
		{
			value = from;
			return true;
		}

		if (Activator.StringTypes.Contains(fromType))
		{
			var response = _parsers.TryGetValue(toType.FullName, out var parser);
			value = parser?.Invoke(from.ToString());
			return response;
		}
		
		if (Activator.StringTypes.Contains(toType))
		{
			if (toType == typeof(string))
			{
				value = from?.ToString();
				return true;
			}

			if ((toType == typeof(TextBuilder))
				|| (toType == typeof(StringBuilder))
				|| (toType == typeof(GapBuffer<char>))
				|| (toType == typeof(JsonString))
				)
			{
				value = from == null 
					? toType.CreateInstance()
					: toType.CreateInstance(from.ToString());
				return true;
			}
		}

		return base.TryConvertTo(from, fromType, toType, out value, settings);
	}

	#endregion
}