#region References

using Cornerstone.Text.Parsing;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Controls.Text;
using Cornerstone.Presentation;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Presentation.Theme.Converters;

public static class Converters
{
	#region Fields

	public static readonly FuncValueConverter<object, object, string> Format;
	public static readonly FuncValueConverter<object, object, string> Humanize;
	public static readonly FuncValueConverter<object, bool> IsGreaterThanZero;
	public static readonly FuncValueConverter<string, bool> IsNotNullOrWhitespace;
	public static readonly FuncValueConverter<object, bool> IsZero;
	public static readonly FuncValueConverter<System.Collections.Generic.IEnumerable<string>, string, string> Join;
	public static readonly FuncValueConverter<SyntaxKind, object, IBrush> SyntaxKindBrush;
	public static readonly FuncValueConverter<Token, object, IBrush> TokenBrush;
	public static readonly FuncValueConverter<double, object, string> ToPercent;
	public static readonly FuncValueConverter<int, string> ToTokenDisplayName;
	public static readonly FuncValueConverter<string, string> ToWrappable;

	#endregion

	#region Constructors

	static Converters()
	{
		Format = ValueConverters.Format;
		Humanize = ValueConverters.Humanize;
		IsGreaterThanZero = ValueConverters.IsGreaterThanZero;
		IsNotNullOrWhitespace = ValueConverters.IsNotNullOrWhitespace;
		IsZero = ValueConverters.IsZero;
		Join = ValueConverters.Join;
		ToPercent = ValueConverters.ToPercent;
		ToWrappable = ValueConverters.ToWrappable;
		SyntaxKindBrush = new FuncValueConverter<SyntaxKind, object, IBrush>((c, f) => SyntaxBrushes.TryGetValue(c, out var b) ? b : ResourceService.GetColorAsBrush(f?.ToString() ?? "Foreground05"));
		TokenBrush = new FuncValueConverter<Token, object, IBrush>((t, f) => t?.Foreground != null ? ColorExtensions.GetBrush((uint) t.Foreground) : ResourceService.GetColorAsBrush(f?.ToString() ?? "Foreground05"));
		ToTokenDisplayName = new FuncValueConverter<int, string>(TextProcessor.GetTokenizerTypeName);
	}

	#endregion
}
