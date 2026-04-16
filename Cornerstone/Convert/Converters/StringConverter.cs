#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Convert.Converters;

/// <summary>
/// Convert strings to many different types.
/// </summary>
public class StringConverter : BaseConverter
{
	#region Fields

	private static readonly Dictionary<string, Func<string, object>> _parsers;

	#endregion

	#region Constructors

	public StringConverter() : base(
		new Guid("8D865571-6C0F-419D-9220-2AB4BBCD6DC0"),
		SourceTypes.StringTypes,
		ArrayExtensions.CombineArrays(
			SourceTypes.StringTypes,
			SourceTypes.CharTypes,
			SourceTypes.DateTypes,
			SourceTypes.TimeTypes,
			SourceTypes.NumberTypes,
			SourceTypes.GuidTypes,
			SourceTypes.BooleanTypes,
			[typeof(Version)]
		))
	{
	}

	static StringConverter()
	{
		_parsers = new Dictionary<string, Func<string, object>>
		{
			{ typeof(string).FullName, x => x },
			{ typeof(StringBuilder).FullName, x => new StringBuilder(x) },
			{ typeof(GapBuffer<char>).FullName, x => new GapBuffer<char>(x.ToCharArray()) },
			{ typeof(char?).FullName, x => x.FirstOrDefault() },
			{ typeof(char).FullName, x => x.FirstOrDefault() },
			{ typeof(DateTimeOffset).FullName, x => DateTimeOffset.Parse(x) },
			{ typeof(TimeSpan).FullName, x => TimeSpan.Parse(x) },
			{ typeof(bool?).FullName, x => bool.Parse(x) },
			{ typeof(bool).FullName, x => bool.Parse(x) },
			{ typeof(byte?).FullName, x => byte.Parse(x) },
			{ typeof(byte).FullName, x => byte.Parse(x) },
			{ typeof(sbyte?).FullName, x => sbyte.Parse(x) },
			{ typeof(sbyte).FullName, x => sbyte.Parse(x) },
			{ typeof(short?).FullName, x => short.Parse(x) },
			{ typeof(short).FullName, x => short.Parse(x) },
			{ typeof(ushort?).FullName, x => ushort.Parse(x) },
			{ typeof(ushort).FullName, x => ushort.Parse(x) },
			{ typeof(int?).FullName, x => int.Parse(x) },
			{ typeof(int).FullName, x => int.Parse(x) },
			{ typeof(uint?).FullName, x => uint.Parse(x) },
			{ typeof(uint).FullName, x => uint.Parse(x) },
			{ typeof(long?).FullName, x => long.Parse(x) },
			{ typeof(long).FullName, x => long.Parse(x) },
			{ typeof(ulong?).FullName, x => ulong.Parse(x) },
			{ typeof(ulong).FullName, x => ulong.Parse(x) },
			{ typeof(Int128?).FullName, x => Int128.Parse(x) },
			{ typeof(Int128).FullName, x => Int128.Parse(x) },
			{ typeof(UInt128?).FullName, x => UInt128.Parse(x) },
			{ typeof(UInt128).FullName, x => UInt128.Parse(x) },
			{ typeof(IntPtr?).FullName, x => IntPtr.Parse(x) },
			{ typeof(IntPtr).FullName, x => IntPtr.Parse(x) },
			{ typeof(UIntPtr?).FullName, x => UIntPtr.Parse(x) },
			{ typeof(UIntPtr).FullName, x => UIntPtr.Parse(x) },
			{ typeof(decimal?).FullName, x => decimal.Parse(x) },
			{ typeof(decimal).FullName, x => decimal.Parse(x) },
			{ typeof(double?).FullName, x => double.Parse(x) },
			{ typeof(double).FullName, x => double.Parse(x) },
			{ typeof(float?).FullName, x => float.Parse(x) },
			{ typeof(float).FullName, x => float.Parse(x) },
			{ typeof(Guid?).FullName, x => Guid.Parse(x) },
			{ typeof(Guid).FullName, x => Guid.Parse(x) },
			{ typeof(ShortGuid?).FullName, x => ShortGuid.Parse(x) },
			{ typeof(ShortGuid).FullName, x => ShortGuid.Parse(x) },
			{ typeof(TimeOnly?).FullName, x => TimeOnly.Parse(x) },
			{ typeof(TimeOnly).FullName, x => TimeOnly.Parse(x) },
			{ typeof(Version).FullName, Version.Parse }
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