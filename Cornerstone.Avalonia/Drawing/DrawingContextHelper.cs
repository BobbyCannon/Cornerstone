#region References

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Cornerstone.Data.Bytes;

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

	public double SpriteHeight { get; private set; }

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
	public void Draw(DrawingContext context, decimal value, ref double visualX, ref double visualY, int precision = 2)
	{
		if (_spriteSheet == null)
		{
			return;
		}

		// 24 digits + sign + dot + safety
		Span<int> buffer = stackalloc int[32];
		var formatted = FormattedNumberSpan.Create(value, precision, buffer);
		Draw(context, formatted, ref visualX, ref visualY, precision);
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

		// 24 digits + sign + dot + safety
		Span<int> buffer = stackalloc int[32];
		var formatted = FormattedNumberSpan.Create(value, precision, buffer);
		Draw(context, formatted, ref visualX, ref visualY, precision);
	}

	/// <summary>
	/// Draws numeric value using pre-rendered sprites (zero heap allocations in hot path)
	/// </summary>
	public void Draw(DrawingContext context, FormattedNumberSpan formatted, ref double visualX, ref double visualY, int precision = 2)
	{
		// Handle sign (outside the span — keeps span clean)
		if (formatted.HasSign)
		{
			var idx = GetSpriteIndex('-');
			if (idx >= 0)
			{
				DrawSprite(context, idx, ref visualX, visualY);
			}
		}

		foreach (var val in formatted.Digits)
		{
			var idx = val == -1
				? GetSpriteIndex('.')
				: GetSpriteIndex((char) ('0' + val));

			if (idx >= 0)
			{
				DrawSprite(context, idx, ref visualX, visualY);
			}
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
				visualY += SpriteHeight;
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

	public void Draw(DrawingContext context, ByteUnit unit, ref double visualX, ref double visualY)
	{
		switch (unit)
		{
			case ByteUnit.Terabyte:
			{
				Draw(context, ByteSize.TerabyteSymbol, ref visualX, ref visualY);
				break;
			}
			case ByteUnit.Terabit:
			{
				Draw(context, ByteSize.TerabitSymbol, ref visualX, ref visualY);
				break;
			}
			case ByteUnit.Megabyte:
			{
				Draw(context, ByteSize.MegabyteSymbol, ref visualX, ref visualY);
				break;
			}
			case ByteUnit.Megabit:
			{
				Draw(context, ByteSize.MegabitSymbol, ref visualX, ref visualY);
				break;
			}
			case ByteUnit.Kilobyte:
			{
				Draw(context, ByteSize.KilobyteSymbol, ref visualX, ref visualY);
				break;
			}
			case ByteUnit.Kilobit:
			{
				Draw(context, ByteSize.KilobitSymbol, ref visualX, ref visualY);
				break;
			}
			case ByteUnit.Gigabyte:
			{
				Draw(context, ByteSize.GigabyteSymbol, ref visualX, ref visualY);
				break;
			}
			case ByteUnit.Gigabit:
			{
				Draw(context, ByteSize.GigabitSymbol, ref visualX, ref visualY);
				break;
			}
			default:
			{
				Draw(context, ByteSize.ByteSymbol, ref visualX, ref visualY);
				break;
			}
		}
	}

	public double Measure(ByteUnit unit)
	{
		return unit switch
		{
			ByteUnit.Terabyte => Measure(ByteSize.TerabyteSymbol),
			ByteUnit.Terabit => Measure(ByteSize.TerabitSymbol),
			ByteUnit.Megabyte => Measure(ByteSize.MegabyteSymbol),
			ByteUnit.Megabit => Measure(ByteSize.MegabitSymbol),
			ByteUnit.Kilobyte => Measure(ByteSize.KilobyteSymbol),
			ByteUnit.Kilobit => Measure(ByteSize.KilobitSymbol),
			ByteUnit.Gigabyte => Measure(ByteSize.GigabyteSymbol),
			ByteUnit.Gigabit => Measure(ByteSize.GigabitSymbol),
			_ => Measure(ByteSize.ByteSymbol)
		};
	}

	public double Measure(ReadOnlySpan<char> text)
	{
		if (text.IsEmpty)
		{
			return 0;
		}

		double total = 0;
		foreach (var c in text)
		{
			var idx = _charToIndex.GetValueOrDefault(c, -1);
			if (idx >= 0)
			{
				total += _charWidths[idx];
			}
			else
			{
				total += _charWidths[0];
			}
		}
		return total;
	}

	public double Measure(decimal value, int precision = 2)
	{
		Span<int> buffer = stackalloc int[32];
		var fmt = FormattedNumberSpan.Create(value, precision, buffer);
		return Measure(fmt);
	}

	public double Measure(double value, int precision = 2)
	{
		Span<int> buffer = stackalloc int[32];
		var fmt = FormattedNumberSpan.Create(value, precision, buffer);
		return Measure(fmt);
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

		SpriteHeight = maxHeight;

		if ((x <= 0) || (maxHeight <= 0))
		{
			return;
		}

		var sheetSize = new PixelSize((int) Math.Ceiling(x), (int) Math.Ceiling(maxHeight));
		_spriteSheet?.Dispose();
		_spriteSheet = new RenderTargetBitmap(sheetSize);

		using var ctx = _spriteSheet.CreateDrawingContext();
		ctx.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.HighQuality });
		ctx.PushTextOptions(new TextOptions { TextRenderingMode = TextRenderingMode.Antialias });

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
		var dest = new Rect(x, y, _charWidths[index], SpriteHeight);
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

	private double GetCharWidth(char c)
	{
		var idx = _charToIndex.GetValueOrDefault(c, -1);
		return idx >= 0 ? _charWidths[idx] : _charWidths[0]; // fallback to space
	}

	private int GetSpriteIndex(char c)
	{
		return _charToIndex.TryGetValue(c, out var idx) ? idx :
			_charToIndex.TryGetValue('?', out idx) ? idx : -1;
	}

	private double Measure(FormattedNumberSpan value)
	{
		double totalWidth = 0;

		if (value.HasSign)
		{
			totalWidth += GetCharWidth('-');
		}

		foreach (var d in value.Digits)
		{
			totalWidth += GetCharWidth(d == -1 ? '.' : (char) ('0' + d));
		}

		return totalWidth;
	}

	#endregion

	#region Structures

	public ref struct FormattedNumberSpan
	{
		#region Fields

		private readonly Span<int> _buffer;
		private int _length;

		#endregion

		#region Constructors

		public FormattedNumberSpan(Span<int> destinationBuffer)
		{
			_buffer = destinationBuffer;
			_length = 0;
			DecimalPosition = 0;
			HasSign = false;
		}

		#endregion

		#region Properties

		public int DecimalPosition { get; private set; }

		public ReadOnlySpan<int> Digits => _buffer[.._length];

		public bool HasSign { get; private set; }

		#endregion

		#region Methods

		public static FormattedNumberSpan Create(double value, int precision, Span<int> destinationBuffer)
		{
			var fmt = new FormattedNumberSpan(destinationBuffer);

			var negative = value < 0;
			if (negative)
			{
				value = -value; // we don't try to preserve -0.0 sign here
			}

			fmt.HasSign = negative;

			if (double.IsNaN(value) || double.IsInfinity(value))
			{
				fmt.AppendDigit(0);
				return fmt;
			}

			if (value == 0)
			{
				fmt.AppendDigit(0);
				if (precision > 0)
				{
					fmt.AppendSpecial(-1);
					for (var i = 0; i < precision; i++)
					{
						fmt.AppendDigit(0);
					}
				}
				fmt.DecimalPosition = precision;
				return fmt;
			}

			var scaled = value * Math.Pow(10, precision);
			var truncated = (long) scaled;
			return FormatNumber(precision, truncated, fmt);
		}

		public static FormattedNumberSpan Create(decimal value, int precision, Span<int> destinationBuffer)
		{
			var fmt = new FormattedNumberSpan(destinationBuffer);

			var negative = value < 0;
			if (negative)
			{
				value = -value;
			}

			fmt.HasSign = negative;

			if (value == 0)
			{
				fmt.AppendDigit(0);
				if (precision > 0)
				{
					fmt.AppendSpecial(-1);
					for (var i = 0; i < precision; i++)
					{
						fmt.AppendDigit(0);
					}
				}
				fmt.DecimalPosition = precision;
				return fmt;
			}

			var scaled = value * (decimal) Math.Pow(10, precision);
			var truncated = (long) scaled;
			return FormatNumber(precision, truncated, fmt);
		}

		private void AppendDigit(int d)
		{
			AppendSpecial(d);
		}

		private void AppendSpecial(int val)
		{
			if (_length >= _buffer.Length)
			{
				throw new InvalidOperationException("Buffer too small");
			}

			_buffer[_length++] = val;
		}

		private static FormattedNumberSpan FormatNumber(int precision, long truncated, FormattedNumberSpan fmt)
		{
			// Extract digits (little-endian: least significant first)
			Span<int> digits = stackalloc int[32];
			var count = 0;

			var temp = truncated;
			do
			{
				digits[count++] = (int) (temp % 10);
				temp /= 10;
			} while (temp > 0);

			// Make sure we have enough slots for decimal places
			var requiredDecimalSlots = precision;
			while (count < (requiredDecimalSlots + 1)) // +1 for possible integer 0
			{
				digits[count++] = 0;
			}

			var started = false;

			// Integer part (from most significant digit)
			for (var i = count - 1; i >= precision; i--)
			{
				var d = digits[i];
				if (started || (d != 0))
				{
					started = true;
					fmt.AppendDigit(d);
				}
			}

			if (!started)
			{
				fmt.AppendDigit(0);
			}

			// Decimal part — always exactly 'precision' digits
			if (precision > 0)
			{
				fmt.AppendSpecial(-1);

				for (var i = precision - 1; i >= 0; i--)
				{
					fmt.AppendDigit(digits[i]);
				}
			}

			fmt.DecimalPosition = precision;
			return fmt;
		}

		#endregion
	}

	#endregion
}