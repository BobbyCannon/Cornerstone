#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Protocols.Osc;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Text;

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

	/// <inheritdoc />
	public CharConverter() : base(
		new Guid("3A4CB4C9-72E7-44FA-944D-B2467D92205B"),
		Activator.CharTypes,
		ArrayExtensions.CombineArrays(
			Activator.StringTypes,
			Activator.CharTypes
		))
	{
	}

	static CharConverter()
	{
		_parsers = new Dictionary<string, Func<string, object>>
		{
			{ typeof(string).FullName, x => x },
			{ typeof(StringBuilder).FullName, x => new StringBuilder(x) },
			{ typeof(TextBuilder).FullName, x => new TextBuilder(x) },
			{ typeof(GapBuffer<char>).FullName, x => new GapBuffer<char>(x) },
			{ typeof(RopeBuffer<char>).FullName, x => new RopeBuffer<char>(x) },
			{ typeof(JsonString).FullName, x => new JsonString(x) },
			{ typeof(char?).FullName, x => x.FirstOrDefault() },
			{ typeof(char).FullName, x => x.FirstOrDefault() },
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

		var response = _parsers.TryGetValue(toType.FullName, out var parser);
		value = parser?.Invoke(from.ToString());
		return response;
	}

	#endregion
}