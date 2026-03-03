#region References

using System;
using System.Collections.Generic;
using Avalonia.Data.Converters;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class ConvertersForString
{
	#region Fields

	public static readonly FuncValueConverter<object, object, string> Format;
	public static readonly FuncValueConverter<object, object, string> Humanize;
	public static readonly FuncValueConverter<IEnumerable<string>, string, string> Join;
	public static readonly FuncValueConverter<string, string> ToWrappable;

	#endregion

	#region Constructors

	static ConvertersForString()
	{
		Format = new FuncValueConverter<object, object, string>((v, p) => p != null ? string.Format($"{{0:{p}}}", v) : v?.ToString());
		Humanize = new FuncValueConverter<object, object, string>(ProcessHumanize);
		Join = new FuncValueConverter<IEnumerable<string>, string, string>((v, p) => string.Join(p ?? ", ", v ?? []));
		ToWrappable = new FuncValueConverter<string, string>(v => v.Replace("\\", "\u200b\\"));
	}

	#endregion

	#region Methods

	private static string ProcessHumanize(object value, object parameter)
	{
		return value switch
		{
			TimeSpan s => s.Humanize(),
			_ => value.ToString()
		};
	}

	#endregion
}