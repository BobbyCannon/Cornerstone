#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cornerstone.Extensions;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Convert.Converters;

/// <summary>
/// Convert char to many different types.
/// </summary>
public class CharConverter : BaseConverter
{
	#region Fields

	private static readonly Dictionary<string, Func<string, object>> _parsers;

	#endregion

	#region Constructors

	public CharConverter() : base(
		new Guid("3A4CB4C9-72E7-44FA-944D-B2467D92205B"),
		SourceTypes.CharTypes,
		ArrayExtensions.CombineArrays(
			SourceTypes.StringTypes,
			SourceTypes.CharTypes,
			[typeof(byte)]
		))
	{
	}

	static CharConverter()
	{
		_parsers = new Dictionary<string, Func<string, object>>
		{
			{ typeof(string).FullName, x => x },
			{ typeof(StringBuilder).FullName, x => new StringBuilder(x) },
			{ typeof(char?).FullName, x => x.FirstOrDefault() },
			{ typeof(char).FullName, x => x.FirstOrDefault() },
			{ typeof(byte?).FullName, x => (byte) x.FirstOrDefault() },
			{ typeof(byte).FullName, x => (byte) x.FirstOrDefault() }
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

		var response = _parsers.TryGetValue(toType.FullName, out var parser);
		value = parser?.Invoke(from.ToString());
		return response;
	}

	#endregion
}