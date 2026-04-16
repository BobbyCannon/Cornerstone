#region References

using System;
using System.Collections.Generic;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Cornerstone.Avalonia.Resources;
using Cornerstone.Avalonia.Themes;
using Cornerstone.Parsers;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class Converters
{
	#region Fields

	public static readonly FuncValueConverter<object, object, string> Format;
	public static readonly FuncValueConverter<decimal?, bool> GreaterThanZero;
	public static readonly FuncValueConverter<object, object, string> Humanize;
	public static readonly FuncValueConverter<IEnumerable<string>, string, string> Join;
	public static readonly FuncValueConverter<SyntaxColor, object, IBrush> SyntaxColorBrush;
	public static readonly FuncValueConverter<double, object, string> ToPercent;
	public static readonly FuncValueConverter<int, string> ToTokenDisplayName;
	public static readonly FuncValueConverter<string, string> ToWrappable;

	#endregion

	#region Constructors

	static Converters()
	{
		Format = new FuncValueConverter<object, object, string>((v, p) => p != null ? string.Format($"{{0:{p}}}", v) : v?.ToString());
		GreaterThanZero = new FuncValueConverter<decimal?, bool>(x => x > 0);
		Humanize = new FuncValueConverter<object, object, string>(ProcessHumanize);
		Join = new FuncValueConverter<IEnumerable<string>, string, string>((v, p) => string.Join(p ?? ", ", v ?? []));
		SyntaxColorBrush = new FuncValueConverter<SyntaxColor, object, IBrush>((c, f) => SyntaxBrushes.TryGetValue(c, out var b) ? b : ResourceService.GetColorAsBrush(f?.ToString() ?? "Foreground05"));
		ToPercent = new FuncValueConverter<double, object, string>((v, p) => p != null ? string.Format($"{{0:{p}}}%", v * 100) : (v * 100).ToString("0.00%"));
		ToTokenDisplayName = new FuncValueConverter<int, string>(TextProcessor.GetTokenizerTypeName);
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