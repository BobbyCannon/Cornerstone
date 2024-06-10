#nullable enable

#region References

using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Rendering;

internal sealed class GlobalTextRunProperties : TextRunProperties
{
	#region Fields

	internal CultureInfo? cultureInfo;
	internal double fontRenderingEmSize;
	internal IBrush? foregroundBrush;
	internal Typeface typeface;

	#endregion

	#region Properties

	public override IBrush? BackgroundBrush => null;

	public override CultureInfo? CultureInfo => cultureInfo;

	public override double FontRenderingEmSize => fontRenderingEmSize;
	public override IBrush? ForegroundBrush => foregroundBrush;

	//public override double FontHintingEmSize { get { return fontRenderingEmSize; } }
	public override TextDecorationCollection? TextDecorations => null;

	public override Typeface Typeface => typeface;

	#endregion

	//public override TextEffectCollection TextEffects { get { return null; } }
}