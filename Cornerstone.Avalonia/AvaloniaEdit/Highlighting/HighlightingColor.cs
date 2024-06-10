#region References

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Avalonia.Media;
using Cornerstone.Avalonia.AvaloniaEdit.Utils;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Highlighting;

/// <summary>
/// A highlighting color is a set of font properties and foreground and background color.
/// </summary>
public class HighlightingColor : IFreezable, ICloneable, IEquatable<HighlightingColor>
{
	#region Fields

	internal static readonly HighlightingColor Empty = FreezableHelper.FreezeAndReturn(new HighlightingColor());
	private HighlightingBrush _background;
	private FontFamily _fontFamily;
	private int? _fontSize;
	private FontStyle? _fontStyle;
	private FontWeight? _fontWeight;
	private HighlightingBrush _foreground;

	private string _name;
	private bool? _strikethrough;
	private bool? _underline;

	#endregion

	#region Properties

	/// <summary>
	/// Gets/sets the background color applied by the highlighting.
	/// </summary>
	public HighlightingBrush Background
	{
		get => _background;
		set
		{
			if (IsFrozen)
			{
				throw new InvalidOperationException();
			}
			_background = value;
		}
	}

	/// <summary>
	/// Gets/sets the font family. Null if the highlighting color does not change the font style.
	/// </summary>
	public FontFamily FontFamily
	{
		get => _fontFamily;
		set
		{
			if (IsFrozen)
			{
				throw new InvalidOperationException();
			}
			_fontFamily = value;
		}
	}

	/// <summary>
	/// Gets/sets the font size. Null if the highlighting color does not change the font style.
	/// </summary>
	public int? FontSize
	{
		get => _fontSize;
		set
		{
			if (IsFrozen)
			{
				throw new InvalidOperationException();
			}
			_fontSize = value;
		}
	}

	/// <summary>
	/// Gets/sets the font style. Null if the highlighting color does not change the font style.
	/// </summary>
	public FontStyle? FontStyle
	{
		get => _fontStyle;
		set
		{
			if (IsFrozen)
			{
				throw new InvalidOperationException();
			}
			_fontStyle = value;
		}
	}

	/// <summary>
	/// Gets/sets the font weight. Null if the highlighting color does not change the font weight.
	/// </summary>
	public FontWeight? FontWeight
	{
		get => _fontWeight;
		set
		{
			if (IsFrozen)
			{
				throw new InvalidOperationException();
			}
			_fontWeight = value;
		}
	}

	/// <summary>
	/// Gets/sets the foreground color applied by the highlighting.
	/// </summary>
	public HighlightingBrush Foreground
	{
		get => _foreground;
		set
		{
			if (IsFrozen)
			{
				throw new InvalidOperationException();
			}
			_foreground = value;
		}
	}

	/// <summary>
	/// Gets whether this HighlightingColor instance is frozen.
	/// </summary>
	public bool IsFrozen { get; private set; }

	/// <summary>
	/// Gets/Sets the name of the color.
	/// </summary>
	public string Name
	{
		get => _name;
		set
		{
			if (IsFrozen)
			{
				throw new InvalidOperationException();
			}
			_name = value;
		}
	}

	/// <summary>
	/// Gets/sets the strikethrough flag
	/// </summary>
	public bool? Strikethrough
	{
		get => _strikethrough;
		set
		{
			if (IsFrozen)
			{
				throw new InvalidOperationException();
			}
			_strikethrough = value;
		}
	}

	/// <summary>
	/// Gets/sets the underline flag. Null if the underline status does not change the font style.
	/// </summary>
	public bool? Underline
	{
		get => _underline;
		set
		{
			if (IsFrozen)
			{
				throw new InvalidOperationException();
			}
			_underline = value;
		}
	}

	internal bool IsEmptyForMerge =>
		(_fontWeight == null) && (_fontStyle == null) && (_underline == null)
		&& (_strikethrough == null) && (_foreground == null) && (_background == null)
		&& (_fontFamily == null) && (_fontSize == null);

	#endregion

	#region Methods

	/// <summary>
	/// Clones this highlighting color.
	/// If this color is frozen, the clone will be unfrozen.
	/// </summary>
	public virtual HighlightingColor Clone()
	{
		var c = (HighlightingColor) MemberwiseClone();
		c.IsFrozen = false;
		return c;
	}

	/// <inheritdoc />
	public sealed override bool Equals(object obj)
	{
		return Equals(obj as HighlightingColor);
	}

	/// <inheritdoc />
	public virtual bool Equals(HighlightingColor other)
	{
		if (other == null)
		{
			return false;
		}
		return (_name == other._name) && (_fontWeight == other._fontWeight)
			&& (_fontStyle == other._fontStyle) && (_underline == other._underline) && (_strikethrough == other._strikethrough)
			&& Equals(_foreground, other._foreground) && Equals(_background, other._background)
			&& Equals(_fontFamily, other._fontFamily) && Equals(_fontSize, other._fontSize);
	}

	/// <summary>
	/// Prevent further changes to this highlighting color.
	/// </summary>
	public virtual void Freeze()
	{
		IsFrozen = true;
	}

	/// <inheritdoc />
	[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
	public override int GetHashCode()
	{
		var hashCode = 0;
		unchecked
		{
			if (_name != null)
			{
				hashCode += 1000000007 * _name.GetHashCode();
			}
			hashCode += 1000000009 * _fontWeight.GetHashCode();
			hashCode += 1000000021 * _fontStyle.GetHashCode();
			if (_foreground != null)
			{
				hashCode += 1000000033 * _foreground.GetHashCode();
			}
			if (_background != null)
			{
				hashCode += 1000000087 * _background.GetHashCode();
			}
			if (_fontFamily != null)
			{
				hashCode += 1000000123 * _fontFamily.GetHashCode();
			}
			if (_fontSize != null)
			{
				hashCode += 1000000167 * _fontSize.GetHashCode();
			}
		}
		return hashCode;
	}

	/// <summary>
	/// Overwrites the properties in this HighlightingColor with those from the given color;
	/// but maintains the current values where the properties of the given color return <c> null </c>.
	/// </summary>
	public void MergeWith(HighlightingColor color)
	{
		FreezableHelper.ThrowIfFrozen(this);
		if (color._fontFamily != null)
		{
			_fontFamily = color._fontFamily;
		}
		if (color._fontSize != null)
		{
			_fontSize = color._fontSize;
		}
		if (color._fontWeight != null)
		{
			_fontWeight = color._fontWeight;
		}
		if (color._fontStyle != null)
		{
			_fontStyle = color._fontStyle;
		}
		if (color._foreground != null)
		{
			_foreground = color._foreground;
		}
		if (color._background != null)
		{
			_background = color._background;
		}
		if (color._underline != null)
		{
			_underline = color._underline;
		}
		if (color._strikethrough != null)
		{
			_strikethrough = color._strikethrough;
		}
	}

	///// <summary>
	///// Serializes this HighlightingColor instance.
	///// </summary>
	//public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	//{
	//	if (info == null)
	//		throw new ArgumentNullException("info");
	//	info.AddValue("Name", this.Name);
	//	info.AddValue("HasWeight", this.FontWeight.HasValue);
	//	if (this.FontWeight.HasValue)
	//		info.AddValue("Weight", this.FontWeight.Value.ToOpenTypeWeight());
	//	info.AddValue("HasStyle", this.FontStyle.HasValue);
	//	if (this.FontStyle.HasValue)
	//		info.AddValue("Style", this.FontStyle.Value.ToString());
	//	info.AddValue("HasUnderline", this.Underline.HasValue);
	//	if (this.Underline.HasValue)
	//		info.AddValue("Underline", this.Underline.Value);
	//	info.AddValue("Foreground", this.Foreground);
	//	info.AddValue("Background", this.Background);
	//}

	/// <summary>
	/// Gets CSS code for the color.
	/// </summary>
	[SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "CSS usually uses lowercase, and all possible values are English-only")]
	public virtual string ToCss()
	{
		var b = new StringBuilder();
		var c = Foreground?.GetColor(null);
		if (c != null)
		{
			b.AppendFormat(CultureInfo.InvariantCulture, "color: #{0:x2}{1:x2}{2:x2}; ", c.Value.R, c.Value.G, c.Value.B);
		}
		if (FontFamily != null)
		{
			b.Append("font-family: ");
			b.Append(FontFamily.Name.ToLowerInvariant());
			b.Append("; ");
		}
		if (FontSize != null)
		{
			b.Append("font-size: ");
			b.Append(FontSize.Value.ToString());
			b.Append("; ");
		}
		if (FontWeight != null)
		{
			b.Append("font-weight: ");
			b.Append(FontWeight.Value.ToString().ToLowerInvariant());
			b.Append("; ");
		}
		if (FontStyle != null)
		{
			b.Append("font-style: ");
			b.Append(FontStyle.Value.ToString().ToLowerInvariant());
			b.Append("; ");
		}
		if (Underline != null)
		{
			b.Append("text-decoration: ");
			b.Append(Underline.Value ? "underline" : "none");
			b.Append("; ");
		}
		if (Strikethrough != null)
		{
			if (Underline == null)
			{
				b.Append("text-decoration:  ");
			}

			b.Remove(b.Length - 1, 1);
			b.Append(Strikethrough.Value ? " line-through" : " none");
			b.Append("; ");
		}
		return b.ToString();
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return "[" + GetType().Name + " " + (string.IsNullOrEmpty(Name) ? ToCss() : Name) + "]";
	}

	object ICloneable.Clone()
	{
		return Clone();
	}

	#endregion
}