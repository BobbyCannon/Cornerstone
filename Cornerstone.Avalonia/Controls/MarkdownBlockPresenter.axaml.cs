#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using Cornerstone.Avalonia.Resources;
using Cornerstone.Avalonia.Text;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Markdown;

#endregion

namespace Cornerstone.Avalonia.Controls;

/// <summary>
/// Renders a single <see cref="MarkdownBlockGroup" /> with chrome (code header, quote border, etc.)
/// and a <see cref="TextRenderer" /> for the projected content.
/// </summary>
public class MarkdownBlockPresenter : TemplatedControl
{
	#region Fields

	public static readonly Thickness BlockQuoteBorderThickness = new(1);
	public static readonly CornerRadius BlockQuoteCornerRadius = new(4);
	public static readonly Thickness CodeBlockBorderPadding = new(10);
	public static readonly Thickness CodeBlockBorderThickness = new(1);
	public static readonly CornerRadius CodeBlockCornerRadius = new(0, 0, 4, 4);
	public static readonly Thickness HorizontalRuleBorderThickness = new(0, 0, 0, 1);
	public static readonly Thickness HorizontalRuleMargin = new(0, 8, 0, 8);
	public static readonly CornerRadius ZeroCornerRadius = new(0);
	public static readonly Thickness ZeroThickness = new(0);

	private Border _border;
	private Button _copyButton;
	private MarkdownBlockGroup _group;
	private Border _header;
	private TextBlock _headerTitle;

	/// <summary>
	/// Last character-column budget used to format a table. Used to reflow when layout width changes.
	/// </summary>
	private int _lastTableMaxChars = -1;

	private static readonly MarkdownViewTokenizer _markdownViewTokenizer = new();
	private TextRenderer _renderer;
	private MarkdownView _view;

	#endregion

	#region Methods

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		_header = e.NameScope.Find<Border>("PART_Header");
		_headerTitle = e.NameScope.Find<TextBlock>("PART_HeaderTitle");
		_border = e.NameScope.Find<Border>("PART_Border");
		DetachRendererHandlers();
		_renderer = e.NameScope.Find<TextRenderer>("PART_Renderer");
		_copyButton = e.NameScope.Find<Button>("PART_CopyButton");
		AttachToView(this.FindAncestorOfType<MarkdownView>());
		WireCopyButton();
		WireRendererHandlers();
		Apply();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		AttachToView(this.FindAncestorOfType<MarkdownView>());
		WireCopyButton();
		Apply();
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		if (_group is not null)
		{
			_group.ContentChanged -= GroupOnContentChanged;
		}

		base.OnDataContextChanged(e);
		_group = DataContext as MarkdownBlockGroup;
		_lastTableMaxChars = -1;

		if (_group is not null)
		{
			_group.ContentChanged += GroupOnContentChanged;
		}

		WireCopyButton();
		Apply();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (_group is not null)
		{
			_group.ContentChanged -= GroupOnContentChanged;
		}

		DetachRendererHandlers();
		DetachFromView();
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		// First measure often has Width=0; after arrange, reflow tables to the real content width.
		if ((change.Property == BoundsProperty) && IsTableGroup())
		{
			TryReflowTable();
		}
	}

	private void Apply()
	{
		if (_group is null || _renderer is null || _border is null || _header is null || _view is null)
		{
			return;
		}

		try
		{
			var source = _view.SourceSnapshot;
			if (string.IsNullOrEmpty(source) && (_view.Document.DocumentLength == 0))
			{
				_group.Links.Clear();
				_renderer.Text = string.Empty;
				return;
			}

			source ??= _view.Document.ToString();
			var buffer = source.AsSpan();

			// Typography + no caret for all block types (headers, code, quotes, lists, paragraphs).
			MarkdownInlineProjector.ApplyMarkdownSurface(_renderer, _view);

			_header.Background = null;
			_header.IsVisible = false;
			_renderer.IsVisible = true;
			_border.Background = null;
			_border.BorderThickness = ZeroThickness;
			_border.CornerRadius = ZeroCornerRadius;
			_border.Margin = ZeroThickness;
			_border.Padding = ZeroThickness;

			_group.Links.Clear();

			var text = (_group.Blocks.Count == 1) && MarkdownView.IsBlockLevel(_group.Blocks[0])
				? ProcessSingleBlock(buffer)
				: ProcessParagraph(buffer);

			// Avoid full Load when text is unchanged (stable groups / no-op refresh)
			if (!string.Equals(_renderer.Text, text, StringComparison.Ordinal))
			{
				_renderer.Text = text;
			}
		}
		catch
		{
			// Template may not be ready; ignore and wait for next apply.
		}
	}

	private void AttachToView(MarkdownView view)
	{
		if (ReferenceEquals(_view, view))
		{
			return;
		}

		DetachFromView();
		_view = view;
		if (_view is not null)
		{
			_view.PropertyChanged += ViewOnPropertyChanged;
		}
	}

	private void DetachFromView()
	{
		if (_view is not null)
		{
			_view.PropertyChanged -= ViewOnPropertyChanged;
		}

		_view = null;
	}

	private void DetachRendererHandlers()
	{
		if (_renderer is null)
		{
			return;
		}

		_renderer.PointerMoved -= RendererOnPointerMoved;
		_renderer.PointerPressed -= RendererOnPointerPressed;
	}

	/// <summary>
	/// Character-column budget for table formatting. Returns <see cref="int.MaxValue" /> when
	/// layout width is not ready yet (first measure) so we do not clamp to a tiny bogus width.
	/// </summary>
	private int GetMaxTableCharacterWidth()
	{
		if (_view is null || _renderer is null || !_view.WordWrap)
		{
			return int.MaxValue;
		}

		// Prefer the presenter's laid-out width; fall back to the view's content area.
		var available = Bounds.Width;
		if (!IsUsableWidth(available))
		{
			available = _view.Bounds.Width - _view.Padding.Left - _view.Padding.Right;
		}

		if (!IsUsableWidth(available))
		{
			// Layout not ready — unconstrained format; TryReflowTable will re-run after arrange.
			return int.MaxValue;
		}

		// Deduct chrome that wraps the TextRenderer (padding/border set just before this is called).
		available -= _border.Padding.Left + _border.Padding.Right
			+ _border.BorderThickness.Left + _border.BorderThickness.Right
			+ _border.Margin.Left + _border.Margin.Right
			+ _renderer.Margin.Left + _renderer.Margin.Right;

		if (!IsUsableWidth(available))
		{
			return int.MaxValue;
		}

		// Measure a single glyph without wrapping so character width is stable.
		using var textLayout = _renderer.GetTextLayout("x", available, false, _renderer.Foreground ?? _view.Foreground);
		var charWidth = textLayout.WidthIncludingTrailingWhitespace;
		if ((charWidth <= 0) || double.IsNaN(charWidth) || double.IsInfinity(charWidth))
		{
			return int.MaxValue;
		}

		// Floor and leave 1 column of slack: measured glyph width vs paint width can differ
		// slightly, and a max-width row that is 1px too wide would soft-wrap if WordWrap were on.
		var columns = (int) Math.Floor(available / charWidth) - 1;
		return Math.Max(10, columns);
	}

	private void GroupOnContentChanged(object sender, EventArgs e)
	{
		_lastTableMaxChars = -1;
		Apply();
	}

	private bool IsTableGroup()
	{
		return (_group?.Blocks.Count == 1)
			&& (_group.Blocks[0].Type == MarkdownTokenizer.TokenTypeTable);
	}

	private static bool IsUsableWidth(double width)
	{
		return (width > 1) && double.IsFinite(width);
	}

	private string ProcessParagraph(ReadOnlySpan<char> buffer)
	{
		MarkdownInlineProjector.ProjectParagraph(_group, buffer, _renderer, _view);
		return _renderer.Text ?? string.Empty;
	}

	private string ProcessSingleBlock(ReadOnlySpan<char> buffer)
	{
		var block = _group.Blocks[0];
		if (block.Type == MarkdownTokenizer.TokenTypeBlockQuote)
		{
			_renderer.ViewModel.TokenManager.Initialize(_markdownViewTokenizer);
			_renderer.Foreground = _view.Foreground;
			_border.Background = ResourceService.GetColorAsBrush("Background04");
			_border.BorderThickness = BlockQuoteBorderThickness;
			_border.CornerRadius = BlockQuoteCornerRadius;
			_border.Padding = CodeBlockBorderPadding;
			return TrimDisplayEnd(buffer.Slice(block.StartOffset, block.Length));
		}

		if (block.Type == MarkdownTokenizer.TokenTypeCodeBlock)
		{
			var (language, contentStart, contentLength) = MarkdownRenderer.ExtractCodeBlockInfo(buffer, block);
			var hasLanguage = !string.IsNullOrWhiteSpace(language);
			_header.Background = ResourceService.GetColorAsBrush("Background04");

			// Hide chrome when untagged fence (no language) — cleaner for ASCII diagrams
			_header.IsVisible = hasLanguage;
			_headerTitle?.Text = language;

			if (hasLanguage)
			{
				_renderer.ViewModel.TokenManager.Initialize(language);
			}
			else
			{
				_renderer.ViewModel.TokenManager.Initialize((Tokenizer) null);
			}

			// Code stays monospace even when the host MarkdownView uses a proportional reading font
			_renderer.FontFamily = CornerstoneTheme.DejaVuSansMono;
			_renderer.FontSize = _view.FontSize;
			_renderer.Foreground = _view.Foreground;
			_border.Background = ResourceService.GetColorAsBrush("Background04");
			_border.BorderThickness = CodeBlockBorderThickness;
			_border.CornerRadius = hasLanguage ? CodeBlockCornerRadius : new CornerRadius(4);
			_border.Padding = CodeBlockBorderPadding;
			_group.CopyRange.Update(contentStart, contentLength);
			if ((contentLength <= 0) || ((contentStart + contentLength) > buffer.Length))
			{
				return string.Empty;
			}

			return buffer.Slice(contentStart, contentLength).ToString();
		}

		if (block.Type == MarkdownTokenizer.TokenTypeHeader)
		{
			var (size, contentStart, contentLength) = MarkdownRenderer.ExtractHeaderInfo(buffer, block);
			_renderer.ViewModel.TokenManager.Initialize(_markdownViewTokenizer);
			_renderer.FontSize = size switch
			{
				1 => (int) (_view.FontSize * 2.6),
				2 => (int) (_view.FontSize * 2.2),
				3 => (int) (_view.FontSize * 2.0),
				4 => (int) (_view.FontSize * 1.6),
				5 => (int) (_view.FontSize * 1.4),
				_ => (int) (_view.FontSize * 1.2)
			};
			_renderer.Foreground = _view.Foreground;
			return TrimDisplayEnd(buffer.Slice(contentStart, contentLength));
		}

		if (block.Type == MarkdownTokenizer.TokenTypeHorizontalRule)
		{
			// Visual rule via bottom border; no source text (avoids showing ---).
			_renderer.IsVisible = false;
			_border.BorderBrush = ResourceService.GetColorAsBrush("BorderBrush");
			_border.BorderThickness = HorizontalRuleBorderThickness;
			_border.Margin = HorizontalRuleMargin;
			_border.Padding = ZeroThickness;
			_border.Background = null;
			return string.Empty;
		}

		// Tables use MarkdownTablePresenter (structured grid + per-cell links).

		if (block.Type == MarkdownTokenizer.TokenTypeUnorderedList)
		{
			// Bullets + per-item inline projection (bold, code, links).
			_renderer.FontSize = _view.FontSize;
			_renderer.Foreground = _view.Foreground;
			MarkdownInlineProjector.ProjectUnorderedList(buffer.Slice(block.StartOffset, block.Length), _renderer, _view);
			return _renderer.Text ?? string.Empty;
		}

		_renderer.ViewModel.TokenManager.Initialize(_markdownViewTokenizer);
		return TrimDisplayEnd(buffer.Slice(block.StartOffset, block.Length));
	}

	private void RendererOnPointerMoved(object sender, PointerEventArgs e)
	{
		if (_group is null || _renderer is null || (_group.Links.Count == 0))
		{
			_renderer?.Cursor = Cursor.Default;
			return;
		}

		_renderer.Cursor = TryGetLinkAtPoint(e.GetPosition(_renderer), out _)
			? new Cursor(StandardCursorType.Hand)
			: Cursor.Default;
	}

	private void RendererOnPointerPressed(object sender, PointerPressedEventArgs e)
	{
		if (_group is null || _renderer is null || _view is null
			|| !e.GetCurrentPoint(_renderer).Properties.IsLeftButtonPressed
			|| (_group.Links.Count == 0))
		{
			return;
		}

		if (!TryGetLinkAtPoint(e.GetPosition(_renderer), out var link))
		{
			return;
		}

		e.Handled = true;
		_view.RaiseLinkClicked(link.Href, link.Text);
	}

	/// <summary>
	/// Trim trailing spaces/tabs/newlines so TextRenderer does not paint an empty final line.
	/// </summary>
	private static string TrimDisplayEnd(ReadOnlySpan<char> text)
	{
		var end = text.Length;
		while (end > 0)
		{
			var c = text[end - 1];
			if (c is not (' ' or '\t' or '\r' or '\n'))
			{
				break;
			}

			end--;
		}

		return end == 0 ? string.Empty : text[..end].ToString();
	}

	private bool TryGetLinkAtPoint(Point point, out MarkdownProjectedLink link)
	{
		link = default;
		if (_renderer?.ViewModel is null || _group is null)
		{
			return false;
		}

		var viewModel = _renderer.ViewModel;
		var visualX = point.X + _renderer.Offset.X;
		var visualY = point.Y + _renderer.Offset.Y;

		if (!viewModel.Lines.TryGetLineForOffset(visualY, visualY, out var line))
		{
			return false;
		}

		var offset = line.GetNearestOffsetAtVisual(visualX, visualY, false);
		foreach (var candidate in _group.Links)
		{
			if (candidate.Contains(offset))
			{
				link = candidate;
				return true;
			}
		}

		return false;
	}

	private void TryReflowTable()
	{
		if (!IsTableGroup() || _renderer is null || _view is null)
		{
			return;
		}

		var maxChars = GetMaxTableCharacterWidth();

		// Skip while still unconstrained if we already painted unconstrained, or when budget unchanged.
		if (maxChars == _lastTableMaxChars)
		{
			return;
		}

		// If layout is still not ready, keep waiting.
		if ((maxChars == int.MaxValue) && ((_lastTableMaxChars == int.MaxValue) || (_lastTableMaxChars < 0)))
		{
			// First pass with MaxValue still needs Apply once (_lastTableMaxChars < 0).
			if (_lastTableMaxChars >= 0)
			{
				return;
			}
		}

		Apply();
	}

	private void ViewOnPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (!IsTableGroup())
		{
			return;
		}

		// Word-wrap / font / size affect character budget for table formatting.
		if ((e.Property == MarkdownView.WordWrapProperty)
			|| (e.Property == FontSizeProperty)
			|| (e.Property == FontFamilyProperty)
			|| (e.Property == BoundsProperty))
		{
			TryReflowTable();
		}
	}

	private void WireCopyButton()
	{
		if (_copyButton is null)
		{
			return;
		}

		// Prefer ancestor MarkdownView.CopyCommand; fall back when template applied before attach.
		_view ??= this.FindAncestorOfType<MarkdownView>();
		_copyButton.Command = _view?.CopyCommand;
		_copyButton.CommandParameter = _group ?? DataContext;
	}

	private void WireRendererHandlers()
	{
		if (_renderer is null)
		{
			return;
		}

		_renderer.PointerMoved += RendererOnPointerMoved;
		_renderer.PointerPressed += RendererOnPointerPressed;
	}

	#endregion
}