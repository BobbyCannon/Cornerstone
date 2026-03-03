#region References

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;

#endregion

namespace Cornerstone.Avalonia.Drawing;

public class DrawingContextHelper : IDisposable
{
	#region Constants

	/// <summary>
	/// 126 - 32 + 1 = 95 -> 32..126
	/// </summary>
	private const int CharCount = 95;

	private const int FirstChar = 32;

	#endregion

	#region Fields

	private readonly Rect[] _charSourceRects;
	private readonly FrozenDictionary<char, int> _charToIndex;
	private readonly double[] _charWidths;
	private static readonly string[] _extraChars;
	private readonly TemplatedControl _owner;
	private readonly string[] _spriteCharacters;
	private double _spriteHeight;
	private RenderTargetBitmap _spriteSheet;

	#endregion

	#region Constructors

	public DrawingContextHelper(TemplatedControl control)
	{
		_owner = control ?? throw new ArgumentNullException(nameof(control));

		var totalCount = CharCount + _extraChars.Length;

		_spriteCharacters = new string[totalCount];
		_charSourceRects = new Rect[totalCount];
		_charWidths = new double[totalCount];

		var charToIndex = new Dictionary<char, int>(totalCount);

		// 1. ASCII range (fast path)
		for (var i = 0; i < CharCount; i++)
		{
			var c = (char) (FirstChar + i);
			_spriteCharacters[i] = c.ToString();
			charToIndex[c] = i;
		}

		// 2. Extra visual characters
		for (var i = 0; i < _extraChars.Length; i++)
		{
			var s = _extraChars[i];
			var c = s[0];
			var index = CharCount + i;

			_spriteCharacters[index] = s;
			charToIndex[c] = index;
		}

		_charToIndex = charToIndex.ToFrozenDictionary();

		_owner.PropertyChanged += OnOwnerPropertyChanged;

		RefreshSprintSheet();
	}

	static DrawingContextHelper()
	{
		_extraChars =
		[
			"°", // U+00B0 degree sign
			"±", // U+00B1 plus-minus
			"×", // U+00D7 multiplication sign
			"÷", // U+00F7 division sign
			"µ", // U+00B5 MICRO SIGN   ← what you asked for
			"μ", // U+03BC GREEK SMALL LETTER MU  (many fonts render identically)
			"²", "³", // superscripts
			"¹", "⁴", "⁵", "⁶", "⁷", "⁸", "⁹", "⁰", // if you need more
			"€", "£", "¥", "¢", // common currencies
			"©", "®", "™", // legal / trademark
			"–", "—", "…", // dashes & ellipsis
			"≤", "≥", "≈", "≠", // inequalities
			"√", "∞", "π", "∑", "∂", // math
			"←", "→", "↑", "↓", "↔", // arrows
			"•", "·", // bullets
			"α", "β", "γ", "Δ", "θ" // greek letters often used in science
		];
	}

	#endregion

	#region Properties

	public Func<double, string> ValueFormatter { get; set; }

	#endregion

	#region Methods

	public void Dispose()
	{
		_spriteSheet?.Dispose();
		_spriteSheet = null;
		_owner.PropertyChanged -= OnOwnerPropertyChanged;
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Draws numeric value using pre-rendered sprites (zero heap allocations in hot path)
	/// </summary>
	public void Draw(DrawingContext context, double value, ref double visualX, ref double visualY, int precision = 2)
	{
		if (_spriteSheet == null)
		{
			return;
		}

		// Handle negative sign
		var negative = value < 0;
		if (negative)
		{
			value = -value;
			var idx = GetSpriteIndex('-');
			if (idx >= 0)
			{
				DrawSprite(context, idx, ref visualX, visualY);
			}
		}

		if (value == 0)
		{
			var zeroIdx = GetSpriteIndex('0');
			if (zeroIdx >= 0)
			{
				DrawSprite(context, zeroIdx, ref visualX, visualY);
			}
			return;
		}

		// Scale and round
		var multiplier = Math.Pow(10, precision);
		var scaled = (long) Math.Round(value * multiplier);

		// Extract digits right → left
		const int MaxDigits = 24;
		Span<int> digits = stackalloc int[MaxDigits];
		var count = 0;

		do
		{
			digits[count++] = (int) (scaled % 10);
			scaled /= 10;
		} while ((scaled > 0) || (count < precision));

		// Find last non-zero decimal digit (for smart precision)
		var lastSignificantDecimal = precision - 1;
		while (lastSignificantDecimal >= 0)
		{
			var pos = count - 1 - lastSignificantDecimal; // position in digits[]
			if ((pos < 0) || (digits[pos] != 0))
			{
				break;
			}
			lastSignificantDecimal--;
		}

		// Effective decimal places to draw
		var decimalsToDraw = lastSignificantDecimal >= 0 ? lastSignificantDecimal + 1 : 0;

		// Position where decimal point goes (counting from right)
		var decimalIndex = decimalsToDraw + 1;

		// Draw left → right
		var integerStarted = false;
		var decimalDrawn = false;

		for (var i = count - 1; i >= 0; i--)
		{
			var digit = digits[i];

			// Are we still in the integer part?
			var isDecimalPart = i < decimalIndex;

			// Skip leading zeros in integer part
			if (!integerStarted && (digit == 0) && !isDecimalPart)
			{
				continue;
			}

			integerStarted = true;

			// Draw digit
			var digitIdx = GetSpriteIndex((char) ('0' + digit));
			if (digitIdx >= 0)
			{
				DrawSprite(context, digitIdx, ref visualX, visualY);
			}

			// Draw decimal point exactly once — right after last integer digit
			if (!decimalDrawn && isDecimalPart && (decimalsToDraw > 0))
			{
				var dotIdx = GetSpriteIndex('.');
				if (dotIdx >= 0)
				{
					DrawSprite(context, dotIdx, ref visualX, visualY);
				}
				decimalDrawn = true;
			}
		}

		// Edge case: value was like 0.0005 → show at least "0."
		if (!integerStarted && (decimalsToDraw > 0))
		{
			var zeroIdx = GetSpriteIndex('0');
			if (zeroIdx >= 0)
			{
				DrawSprite(context, zeroIdx, ref visualX, visualY);
			}
			var dotIdx = GetSpriteIndex('.');
			if (dotIdx >= 0)
			{
				DrawSprite(context, dotIdx, ref visualX, visualY);
			}
			decimalDrawn = true;
		}
	}

	/// <summary>
	/// General text drawing using sprite sheet (zero-allocation hot path)
	/// </summary>
	public void Draw(DrawingContext context, char value, ref double visualX, ref double visualY)
	{
		var idx = GetSpriteIndex(value);
		if (idx < 0)
		{
			// Fallback: unknown → space or ?
			idx = GetSpriteIndex('?');
			if (idx < 0)
			{
				idx = GetSpriteIndex(' ');
			}
			if (idx < 0)
			{
				return;
			}
		}

		DrawSprite(context, idx, ref visualX, visualY);
	}

	/// <summary>
	/// General text drawing using sprite sheet (zero-allocation hot path)
	/// </summary>
	public void Draw(DrawingContext context, ReadOnlySpan<char> text, ref double visualX, ref double visualY)
	{
		if ((_spriteSheet == null) || text.IsEmpty)
		{
			return;
		}

		var x = visualX;

		foreach (var c in text)
		{
			if (c == '\n')
			{
				visualX = x;
				visualY += _spriteHeight;
				continue;
			}
			if ((c == '\r') || (c == '\t'))
			{
				continue;
			}

			var idx = GetSpriteIndex(c);
			if (idx < 0)
			{
				// Fallback: unknown → space or ?
				idx = GetSpriteIndex('?');
				if (idx < 0)
				{
					idx = GetSpriteIndex(' ');
				}
				if (idx < 0)
				{
					continue;
				}
			}

			DrawSprite(context, idx, ref visualX, visualY);
		}
	}

	public void OnOwnerPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs change)
	{
		if ((change.Property == TemplatedControl.FontFamilyProperty) ||
			(change.Property == TemplatedControl.FontSizeProperty) ||
			(change.Property == TemplatedControl.FontStretchProperty) ||
			(change.Property == TemplatedControl.FontStyleProperty) ||
			(change.Property == TemplatedControl.FontWeightProperty) ||
			(change.Property == TemplatedControl.ForegroundProperty))
		{
			RefreshSprintSheet();
		}
	}

	public void RefreshSprintSheet()
	{
		_spriteSheet?.Dispose();
		_spriteSheet = null;

		if (_spriteCharacters.Length == 0)
		{
			return;
		}

		var typeface = GetActualTypeface();
		var fontSize = _owner.FontSize;
		var foreground = _owner.Foreground ?? Brushes.White;

		double x = 0;
		double maxHeight = 0;

		for (var i = 0; i < _spriteCharacters.Length; i++)
		{
			using var layout = new TextLayout(_spriteCharacters[i], typeface, fontSize, foreground);
			var w = layout.WidthIncludingTrailingWhitespace;
			var h = layout.Height;

			_charWidths[i] = w;
			_charSourceRects[i] = new Rect(x, 0, w, h);
			maxHeight = Math.Max(maxHeight, h);

			x += w;
		}

		_spriteHeight = maxHeight;

		if ((x <= 0) || (maxHeight <= 0))
		{
			return;
		}

		var sheetSize = new PixelSize((int) Math.Ceiling(x), (int) Math.Ceiling(maxHeight));
		_spriteSheet?.Dispose();
		_spriteSheet = new RenderTargetBitmap(sheetSize);

		using var ctx = _spriteSheet.CreateDrawingContext();
		ctx.PushRenderOptions(new RenderOptions
		{
			TextRenderingMode = TextRenderingMode.Antialias,
			BitmapInterpolationMode = BitmapInterpolationMode.HighQuality
		});

		double offsetX = 0;
		for (var i = 0; i < _spriteCharacters.Length; i++)
		{
			using var layout = new TextLayout(_spriteCharacters[i], typeface, fontSize, foreground);
			layout.Draw(ctx, new Point(offsetX, 0));
			offsetX += _charWidths[i];
		}
	}

	private void DrawSprite(DrawingContext context, int index, ref double x, double y)
	{
		if ((index < 0) || (index >= _charSourceRects.Length))
		{
			return;
		}

		var src = _charSourceRects[index];
		var dest = new Rect(x, y, _charWidths[index], _spriteHeight);
		context.DrawImage(_spriteSheet!, src, dest);

		x += _charWidths[index];
	}

	private Typeface GetActualTypeface()
	{
		return _owner switch
		{
			CornerstoneContentControl s => s.Typeface,
			CornerstoneTemplatedControl s => s.Typeface,
			CornerstoneUserControl s => s.Typeface,
			_ => new Typeface(_owner.FontFamily, _owner.FontStyle, _owner.FontWeight, _owner.FontStretch)
		};
	}

	private int GetSpriteIndex(char c)
	{
		return _charToIndex.TryGetValue(c, out var idx) ? idx :
			_charToIndex.TryGetValue('?', out idx) ? idx : -1;
	}

	#endregion
}