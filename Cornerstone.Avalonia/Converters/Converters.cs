#region References

using System;
using System.Collections.Generic;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Cornerstone.Avalonia.Extensions;
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
	public static readonly FuncValueConverter<object, object, string> Humanize;
	public static readonly FuncValueConverter<object, bool> IsGreaterThanZero;
	public static readonly FuncValueConverter<string, bool> IsNotNullOrWhitespace;
	public static readonly FuncValueConverter<object, bool> IsZero;
	public static readonly FuncValueConverter<IEnumerable<string>, string, string> Join;
	public static readonly FuncValueConverter<SyntaxKind, object, IBrush> SyntaxKindBrush;
	public static readonly FuncValueConverter<Token, object, IBrush> TokenBrush;
	public static readonly FuncValueConverter<double, object, string> ToPercent;
	public static readonly FuncValueConverter<int, string> ToTokenDisplayName;
	public static readonly FuncValueConverter<string, string> ToWrappable;

	#endregion

	#region Constructors

	static Converters()
	{
		Format = new FuncValueConverter<object, object, string>((v, p) => p != null ? string.Format($"{{0:{p}}}", v) : v?.ToString());
		Humanize = new FuncValueConverter<object, object, string>(ProcessHumanize);
		IsGreaterThanZero = new FuncValueConverter<object, bool>(ProcessIsGreaterThanZero);
		IsNotNullOrWhitespace = new(x => !string.IsNullOrWhiteSpace(x));
		IsZero = new FuncValueConverter<object, bool>(ProcessIsZero);
		Join = new FuncValueConverter<IEnumerable<string>, string, string>((v, p) => string.Join(p ?? ", ", v ?? []));
		SyntaxKindBrush = new FuncValueConverter<SyntaxKind, object, IBrush>((c, f) => SyntaxBrushes.TryGetValue(c, out var b) ? b : ResourceService.GetColorAsBrush(f?.ToString() ?? "Foreground05"));
		TokenBrush = new FuncValueConverter<Token, object, IBrush>((t, f) => t?.Foreground != null ? ColorExtensions.GetBrush((uint) t.Foreground) : ResourceService.GetColorAsBrush(f?.ToString() ?? "Foreground05"));
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

	private static bool ProcessIsGreaterThanZero(object o)
	{
		return o switch
		{
			decimal and > 0 => true,
			double and > 0 => true,
			float and > 0 => true,
			uint and > 0 => true,
			> 0 => true,
			_ => false
		};
	}

	private static bool ProcessIsZero(object o)
	{
		return o switch
		{
			decimal and 0 => true,
			double and 0 => true,
			float and 0 => true,
			uint and 0 => true,
			0 => true,
			_ => false
		};
	}

	#endregion
}