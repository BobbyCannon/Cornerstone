#region References

using System;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Cornerstone.Avalonia.TextEditor.Utils;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

/// <summary>
/// <see cref="TextRunProperties" /> implementation that allows changing the properties.
/// A <see cref="VisualLineElementTextRunProperties" /> instance usually is assigned to a single
/// <see cref="VisualLineElement" />.
/// </summary>
public class VisualLineElementTextRunProperties : TextRunProperties, ICloneable
{
	#region Fields

	private IBrush _backgroundBrush;
	private BaselineAlignment _baselineAlignment;

	private CultureInfo _cultureInfo;

	//double fontHintingEmSize;
	private double _fontRenderingEmSize;
	private IBrush _foregroundBrush;

	private TextDecorationCollection _textDecorations;
	private Typeface _typeface;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new VisualLineElementTextRunProperties instance that copies its values
	/// from the specified <paramref name="textRunProperties" />.
	/// For the <see cref="TextDecorations" /> and TextEffects collections, deep copies
	/// are created if those collections are not frozen.
	/// </summary>
	public VisualLineElementTextRunProperties(TextRunProperties textRunProperties)
	{
		if (textRunProperties == null)
		{
			throw new ArgumentNullException(nameof(textRunProperties));
		}

		_backgroundBrush = textRunProperties.BackgroundBrush;
		_baselineAlignment = textRunProperties.BaselineAlignment;
		_cultureInfo = textRunProperties.CultureInfo;
		//fontHintingEmSize = textRunProperties.FontHintingEmSize;
		_fontRenderingEmSize = textRunProperties.FontRenderingEmSize;
		_foregroundBrush = textRunProperties.ForegroundBrush;
		_typeface = textRunProperties.Typeface;
		_textDecorations = textRunProperties.TextDecorations;

		/*if (textDecorations != null && !textDecorations.IsFrozen) {
			textDecorations = textDecorations.Clone();
		}*/
		/*textEffects = textRunProperties.TextEffects;
		if (textEffects != null && !textEffects.IsFrozen) {
			textEffects = textEffects.Clone();
		}
		typographyProperties = textRunProperties.TypographyProperties;
		numberSubstitution = textRunProperties.NumberSubstitution;*/
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override IBrush BackgroundBrush => _backgroundBrush;

	/// <inheritdoc />
	public override BaselineAlignment BaselineAlignment => _baselineAlignment;

	/// <inheritdoc />
	public override CultureInfo CultureInfo => _cultureInfo;

	/*public override double FontHintingEmSize {
		get { return fontHintingEmSize; }
	}

	/// <summary>
	/// Sets the <see cref="FontHintingEmSize"/>.
	/// </summary>
	public void SetFontHintingEmSize(double value)
	{
		fontHintingEmSize = value;
	}*/

	/// <inheritdoc />
	public override double FontRenderingEmSize => _fontRenderingEmSize;

	/// <inheritdoc />
	public override IBrush ForegroundBrush => _foregroundBrush;

	/// <summary>
	/// Gets the text decorations. The value may be null, a frozen <see cref="TextDecorationCollection" />
	/// or an unfrozen <see cref="TextDecorationCollection" />.
	/// If the value is an unfrozen <see cref="TextDecorationCollection" />, you may assume that the
	/// collection instance is only used for this <see cref="TextRunProperties" /> instance and it is safe
	/// to add <see cref="TextDecoration" />s.
	/// </summary>
	public override TextDecorationCollection TextDecorations => _textDecorations;

	/// <inheritdoc />
	public override Typeface Typeface => _typeface;

	#endregion

	#region Methods

	/// <summary>
	/// Creates a copy of this instance.
	/// </summary>
	public virtual VisualLineElementTextRunProperties Clone()
	{
		return new VisualLineElementTextRunProperties(this);
	}

	/// <summary>
	/// Sets the <see cref="BackgroundBrush" />.
	/// </summary>
	public void SetBackgroundBrush(IBrush value)
	{
		_backgroundBrush = value?.ToImmutable();
	}

	/// <summary>
	/// Sets the <see cref="BaselineAlignment" />.
	/// </summary>
	public void SetBaselineAlignment(BaselineAlignment value)
	{
		_baselineAlignment = value;
	}

	/// <summary>
	/// Sets the <see cref="CultureInfo" />.
	/// </summary>
	public void SetCultureInfo(CultureInfo value)
	{
		_cultureInfo = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Sets the <see cref="FontRenderingEmSize" />.
	/// </summary>
	public void SetFontRenderingEmSize(double value)
	{
		_fontRenderingEmSize = value;
	}

	/// <summary>
	/// Sets the <see cref="ForegroundBrush" />.
	/// </summary>
	public void SetForegroundBrush(IBrush value)
	{
		_foregroundBrush = value?.ToImmutable();
	}

	/// <summary>
	/// Sets the <see cref="TextDecorations" />.
	/// </summary>
	public void SetTextDecorations(TextDecorationCollection value)
	{
		ExtensionMethods.CheckIsFrozen(value);
		if (_textDecorations == null)
		{
			_textDecorations = value;
		}
		else
		{
			_textDecorations = new TextDecorationCollection(_textDecorations.Union(value));
		}
	}

	/// <summary>
	/// Sets the <see cref="Typeface" />.
	/// </summary>
	public void SetTypeface(Typeface value)
	{
		_typeface = value;
	}

	object ICloneable.Clone()
	{
		return Clone();
	}

	#endregion

	/*
	/// <summary>
	/// Gets the text effects. The value may be null, a frozen <see cref="TextEffectCollection"/>
	/// or an unfrozen <see cref="TextEffectCollection"/>.
	/// If the value is an unfrozen <see cref="TextEffectCollection"/>, you may assume that the
	/// collection instance is only used for this <see cref="TextRunProperties"/> instance and it is safe
	/// to add <see cref="TextEffect"/>s.
	/// </summary>
	public override TextEffectCollection TextEffects {
		get { return textEffects; }
	}

	/// <summary>
	/// Sets the <see cref="TextEffects"/>.
	/// </summary>
	public void SetTextEffects(TextEffectCollection value)
	{
		ExtensionMethods.CheckIsFrozen(value);
		textEffects = value;
	}

	/// <summary>
	/// Gets the typography properties for the text run.
	/// </summary>
	public override TextRunTypographyProperties TypographyProperties {
		get { return typographyProperties; }
	}

	/// <summary>
	/// Sets the <see cref="TypographyProperties"/>.
	/// </summary>
	public void SetTypographyProperties(TextRunTypographyProperties value)
	{
		typographyProperties = value;
	}

	/// <summary>
	/// Gets the number substitution settings for the text run.
	/// </summary>
	public override NumberSubstitution NumberSubstitution {
		get { return numberSubstitution; }
	}

	/// <summary>
	/// Sets the <see cref="NumberSubstitution"/>.
	/// </summary>
	public void SetNumberSubstitution(NumberSubstitution value)
	{
		numberSubstitution = value;
	}
	*/
}