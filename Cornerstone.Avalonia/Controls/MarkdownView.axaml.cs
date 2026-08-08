#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornerstone.Avalonia.Text;
using Cornerstone.Collections;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Markdown;
using Cornerstone.Presentation;
using Cornerstone.Profiling;

#endregion

namespace Cornerstone.Avalonia.Controls;

public partial class MarkdownView : CornerstoneTemplatedControl
{
	#region Fields

	private readonly IQueue<MarkdownBlockGroup> _blockGroupPool;
	private static readonly ClipboardService _clipboard;
	private TextEditorViewModel _document;
	private readonly IQueue<Block> _markdownBlockPool;
	private MarkdownParser _markdownParser;

	/// <summary>
	/// True while we are programmatically pinning to the bottom. ScrollChanged in that
	/// window must not clear <see cref="AutoScroll"/>.
	/// </summary>
	private bool _isProgrammaticScroll;

	/// <summary>
	/// Host requested top-of-document after a content change (e.g. docs navigation).
	/// Applied when the ScrollViewer exists and after the next render — wins over AutoScroll for that pass.
	/// </summary>
	private bool _pendingScrollHome;

	private ScrollViewer _scrollViewer;
	private readonly Throttle _throttle;

	#endregion

	#region Constructors

	public MarkdownView()
	{
		_markdownBlockPool = new SpeedyQueue<Block>();
		_blockGroupPool = new SpeedyQueue<MarkdownBlockGroup>();
		_throttle = new Throttle(ThrottleRender, TimeSpan.FromMilliseconds(100));

		Blocks = [];
		BlockGroups = [];

		// Default private buffer; hosts can replace via binding or assignment.
		Document = CreateDocument();
	}

	static MarkdownView()
	{
		_clipboard = new ClipboardService();

		AffectsRender<MarkdownView>(FontFamilyProperty, FontSizeProperty, FontStyleProperty, FontWeightProperty);
		AffectsMeasure<MarkdownView>(WordWrapProperty);
	}

	#endregion

	#region Properties

	/// <summary>
	/// When true, document growth keeps the viewport pinned to the bottom.
	/// Cleared only when the user scrolls away from the bottom (not on programmatic ScrollToEnd).
	/// </summary>
	[StyledProperty(DefaultValue = true)]
	public partial bool AutoScroll { get; set; }

	/// <summary>
	/// Markdown document (gap buffer). Created by the control by default; bind or assign a host
	/// <see cref="TextEditorViewModel" /> (e.g. agent output) to share a single streaming buffer.
	/// Prefer <see cref="TextEditorViewModel.Append(string)" /> / <see cref="TextEditorViewModel.Load" /> /
	/// <see cref="TextEditorViewModel.Clear" /> — do not rebuild content via string properties.
	/// </summary>
	[DirectProperty]
	public TextEditorViewModel Document
	{
		get => _document;
		set
		{
			var next = value ?? CreateDocument();
			if (ReferenceEquals(_document, next))
			{
				return;
			}

			if (_document is not null)
			{
				_document.DocumentChanged -= DocumentOnDocumentChanged;
			}

			SetAndRaise(DocumentProperty, ref _document, next);

			_document.DocumentChanged += DocumentOnDocumentChanged;
			_markdownParser = new MarkdownParser(_document.Buffer, _markdownBlockPool);
			SourceSnapshot = _document.ToString();
			_throttle.Trigger();
		}
	}

	[StyledProperty]
	public partial bool WordWrap { get; set; }

	[StyledProperty]
	protected partial ObservableCollection<MarkdownBlockGroup> BlockGroups { get; set; }

	[StyledProperty]
	protected partial ObservableCollection<Block> Blocks { get; set; }

	/// <summary>
	/// Snapshot of document text taken for the last render pass (stable for presenters).
	/// </summary>
	internal string SourceSnapshot { get; private set; } = string.Empty;

	#endregion

	#region Methods

	[RelayCommand]
	public void Copy(object value)
	{
		if (value is not MarkdownBlockGroup group
			|| (group.CopyRange.Length <= 0))
		{
			return;
		}

		var buffer = SourceSnapshot;
		if (string.IsNullOrEmpty(buffer) || (group.CopyRange.EndOffset > buffer.Length))
		{
			buffer = Document.ToString();
		}

		if ((group.CopyRange.StartOffset < 0)
			|| (group.CopyRange.EndOffset > buffer.Length))
		{
			return;
		}

		var text = buffer.Substring(group.CopyRange.StartOffset, group.CopyRange.Length);
		Dispatcher.InvokeAsync(() => _clipboard?.SetTextAsync(text));
	}

	public static bool IsBlockLevel(Block block)
	{
		return (block.Type == MarkdownTokenizer.TokenTypeBlockQuote)
			|| (block.Type == MarkdownTokenizer.TokenTypeCodeBlock)
			|| (block.Type == MarkdownTokenizer.TokenTypeHeader)
			|| (block.Type == MarkdownTokenizer.TokenTypeHorizontalRule)
			|| (block.Type == MarkdownTokenizer.TokenTypeTable)
			|| (block.Type == MarkdownTokenizer.TokenTypeUnorderedList);
	}

	public void ScrollToEnd()
	{
		if (_scrollViewer is null)
		{
			return;
		}

		_isProgrammaticScroll = true;
		try
		{
			// Force layout so Extent includes the latest markdown blocks before pinning.
			_scrollViewer.InvalidateMeasure();
			_scrollViewer.InvalidateArrange();
			UpdateLayout();

			var maxY = Math.Max(0, _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height);
			_scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, maxY);
			_scrollViewer.ScrollToEnd();
		}
		finally
		{
			// global:: — control inherits Cornerstone IDispatcher "Dispatcher" property.
			global::Avalonia.Threading.Dispatcher.UIThread.Post(
				() => _isProgrammaticScroll = false,
				global::Avalonia.Threading.DispatcherPriority.Loaded);
		}
	}

	/// <summary>
	/// Re-parses and rebuilds presenters from the current <see cref="Document" /> buffer.
	/// Use when the control re-enters the visual tree (e.g. tab switch) without a DocumentChanged.
	/// </summary>
	public void RefreshPresentation()
	{
		if (_document is null)
		{
			return;
		}

		// Ensure parser tracks the live buffer (template/attach order can leave a stale parser).
		_markdownParser = new MarkdownParser(_document.Buffer, _markdownBlockPool);
		_throttle.Trigger();
	}

	/// <summary>
	/// Scrolls the document viewport to the top (e.g. when opening a file without a header fragment).
	/// Safe to call before the template is applied or before markdown has finished rendering —
	/// the request is queued and applied when the ScrollViewer and layout are ready.
	/// Pending home takes priority over <see cref="AutoScroll"/> for the next content paint.
	/// </summary>
	public void ScrollToHome()
	{
		_pendingScrollHome = true;
		TryApplyPendingScrollHome(finalize: false);
		// Content/layout may still change after Document.Load; re-apply after layout + throttle.
		ScheduleScrollHome();
	}

	/// <summary>
	/// Scrolls so the first heading whose id matches <paramref name="fragment" /> is near the top.
	/// Fragment may include a leading <c> # </c>. Returns false if no matching heading is found.
	/// </summary>
	public bool ScrollToFragment(string fragment)
	{
		if (string.IsNullOrWhiteSpace(fragment) || _scrollViewer is null)
		{
			return false;
		}

		var id = fragment.Trim();
		if (id.StartsWith('#'))
		{
			id = id[1..];
		}

		if (id.Length == 0)
		{
			return false;
		}

		var source = SourceSnapshot;
		if (string.IsNullOrEmpty(source))
		{
			source = Document.ToString();
		}

		var buffer = source.AsSpan();
		MarkdownBlockGroup targetGroup = null;

		foreach (var group in BlockGroups)
		{
			if ((group.Blocks.Count != 1)
				|| (group.Blocks[0].Type != MarkdownTokenizer.TokenTypeHeader))
			{
				continue;
			}

			var block = group.Blocks[0];
			var (_, contentStart, contentLength) = MarkdownRenderer.ExtractHeaderInfo(buffer, block);
			if ((contentLength <= 0)
				|| (contentStart < 0)
				|| ((contentStart + contentLength) > buffer.Length))
			{
				continue;
			}

			var headingId = MarkdownLink.ToHeadingId(buffer.Slice(contentStart, contentLength));
			if (string.Equals(headingId, id, StringComparison.OrdinalIgnoreCase))
			{
				targetGroup = group;
				break;
			}
		}

		if (targetGroup is null)
		{
			return false;
		}

		// Find the presenter for this group and scroll it into view.
		var itemsControl = _scrollViewer.Content as ItemsControl
			?? _scrollViewer.GetVisualDescendants().OfType<ItemsControl>().FirstOrDefault();
		if (itemsControl is null)
		{
			return false;
		}

		// Ensure containers exist
		itemsControl.UpdateLayout();
		var container = itemsControl.ContainerFromItem(targetGroup)
			?? itemsControl.GetVisualDescendants()
				.OfType<Control>()
				.FirstOrDefault(c => ReferenceEquals(c.DataContext, targetGroup));

		if (container is null)
		{
			return false;
		}

		var transform = container.TransformToVisual(_scrollViewer);
		if (transform is null)
		{
			return false;
		}

		var topLeft = transform.Value.Transform(new Point(0, 0));
		var offsetY = _scrollViewer.Offset.Y + topLeft.Y;
		if (offsetY < 0)
		{
			offsetY = 0;
		}

		_scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, offsetY);
		return true;
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		_scrollViewer = e.NameScope.Find<ScrollViewer>("PARTS_ScrollViewer");
		_scrollViewer?.HorizontalScrollBarVisibility = WordWrap
			? ScrollBarVisibility.Disabled
			: ScrollBarVisibility.Visible;
		AttachScrollViewer();
		TryApplyPendingScrollHome(finalize: false);
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		AttachScrollViewer();

		// Tab switch reuses the control: Document still has text, but presenters may be gone.
		// Always rebuild from the buffer (DocumentChanged does not fire on re-attach).
		if ((_document is not null) && (_document.DocumentLength > 0))
		{
			RefreshPresentation();
		}
		else
		{
			_throttle.Trigger();
		}

		if (_pendingScrollHome)
		{
			ScheduleScrollHome();
		}
		else if (AutoScroll)
		{
			// TabControl often defers visual tree for inactive tabs; re-pin when shown again.
			ScheduleScrollToEnd();
		}
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_scrollViewer?.ScrollChanged -= ScrollViewerOnScrollChanged;
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == AutoScrollProperty)
		{
			if (AutoScroll)
			{
				ScrollToEnd();
			}
		}
		else if (change.Property == WordWrapProperty)
		{
			if (_scrollViewer == null)
			{
				return;
			}
			_scrollViewer.HorizontalScrollBarVisibility = WordWrap
				? ScrollBarVisibility.Disabled
				: ScrollBarVisibility.Visible;
			_throttle.Trigger();
		}
	}

	/// <summary>
	/// Raises <see cref="LinkClicked" /> for a projected link activation.
	/// </summary>
	internal void RaiseLinkClicked(string href, string text)
	{
		var args = new MarkdownLinkClickedEventArgs(href, text);
		LinkClicked?.Invoke(this, args);
	}

	private void AttachScrollViewer()
	{
		_scrollViewer?.ScrollChanged -= ScrollViewerOnScrollChanged;
		_scrollViewer?.ScrollChanged += ScrollViewerOnScrollChanged;
	}

	private List<MarkdownBlockGroup> BuildGroups(List<Block> parsedBlocks, int documentLength)
	{
		var parsedGroups = new List<MarkdownBlockGroup>((parsedBlocks.Count / 2) + 2);
		var skippedBlocks = new List<Block>(64);
		MarkdownBlockGroup currentParagraph = null;

		// Index loop so a blank line can close a paragraph and skip following empty lines.
		for (var i = 0; i < parsedBlocks.Count; i++)
		{
			var block = parsedBlocks[i];

			if (IsBlockLevel(block))
			{
				currentParagraph = null;

				var group = RentGroup();
				group.Reset();
				group.Add(block);
				group.FinalizeIncomplete(documentLength);
				parsedGroups.Add(group);
				continue;
			}

			// Between blocks / before first content: never create empty whitespace-only groups.
			if (currentParagraph is null)
			{
				if (IsIgnorableDisplayWhitespace(block))
				{
					skippedBlocks.Add(block);
					continue;
				}

				currentParagraph = RentGroup();
				currentParagraph.Reset();
				parsedGroups.Add(currentParagraph);
				currentParagraph.Add(block);
				continue;
			}

			// Blank line (one or more empty lines after a newline) ends the paragraph.
			// Soft breaks (single newline before more text) stay inside the group.
			if ((block.Type == TextProcessor.TokenTypeNewLine)
				&& IsParagraphBreak(parsedBlocks, i, out var resumeAt))
			{
				for (var s = i; s < resumeAt; s++)
				{
					skippedBlocks.Add(parsedBlocks[s]);
				}

				currentParagraph = null;
				i = resumeAt - 1; // for-loop increments
				continue;
			}

			currentParagraph.Add(block);
		}

		// Trim trailing newlines/whitespace from groups (soft-break leftovers, EOF).
		for (var i = parsedGroups.Count - 1; i >= 0; i--)
		{
			var group = parsedGroups[i];
			if (group.Blocks.Count == 0)
			{
				continue;
			}

			while (group.Blocks.Count > 0)
			{
				var index = group.Blocks.Count - 1;
				var lastBlock = group.Blocks[index];
				if (IsIgnorableDisplayWhitespace(lastBlock))
				{
					skippedBlocks.Add(lastBlock);
					group.RemoveAt(index);
				}
				else
				{
					break;
				}
			}

			if (group.Blocks.Count == 0)
			{
				parsedGroups.RemoveAt(i);
				group.Reset();
				_blockGroupPool.Enqueue(group);
			}
			else
			{
				group.FinalizeIncomplete(documentLength);
			}
		}

		// Leading whitespace/newlines not in any group remain only in Blocks; not pooled here.
		_ = skippedBlocks;

		return parsedGroups;
	}

	private static bool IsIgnorableDisplayWhitespace(Block block)
	{
		return (block.Type == TextProcessor.TokenTypeNewLine)
			|| (block.Type == TextProcessor.TokenTypeWhitespace);
	}

	/// <summary>
	/// True when <paramref name="newlineIndex" /> starts a blank line (paragraph break):
	/// another newline appears before the next contentful inline (optional whitespace between).
	/// <paramref name="resumeAt" /> is the index of the next contentful block (or Count).
	/// </summary>
	private static bool IsParagraphBreak(IReadOnlyList<Block> blocks, int newlineIndex, out int resumeAt)
	{
		resumeAt = newlineIndex + 1;
		var j = newlineIndex + 1;
		var sawSecondNewline = false;

		while (j < blocks.Count)
		{
			var b = blocks[j];
			if (b.Type == TextProcessor.TokenTypeWhitespace)
			{
				j++;
				continue;
			}

			if (b.Type == TextProcessor.TokenTypeNewLine)
			{
				sawSecondNewline = true;
				j++;
				continue;
			}

			// Contentful inline
			break;
		}

		if (!sawSecondNewline)
		{
			return false;
		}

		resumeAt = j;
		return true;
	}

	private static TextEditorViewModel CreateDocument()
	{
		return new TextEditorViewModel
		{
			HighlightCurrentLine = false,
			ShowCaret = false,
			ShowLineNumbers = false,
			UndoManager = { Enabled = false }
		};
	}

	private void DocumentOnDocumentChanged(object sender, TextDocumentChangedArgs e)
	{
		_throttle.Trigger();
	}

	private void RebuildBlocksFromGroups()
	{
		// Flat mirror of blocks still owned by live groups (safe after group reconcile/pooling).
		Blocks.Clear();
		foreach (var group in BlockGroups)
		{
			foreach (var block in group.Blocks)
			{
				Blocks.Add(block);
			}
		}
	}

	private void ReconcileGroups(List<MarkdownBlockGroup> parsedGroups, int documentLength)
	{
		var commonCount = 0;
		var maxLen = Math.Min(BlockGroups.Count, parsedGroups.Count);
		while ((commonCount < maxLen) && BlockGroups[commonCount].MatchesStructure(parsedGroups[commonCount]))
		{
			// Stable group — return the freshly built duplicate (and its blocks) to the pools
			var duplicate = parsedGroups[commonCount];
			duplicate.Reset(_markdownBlockPool);
			_blockGroupPool.Enqueue(duplicate);
			commonCount++;
		}

		// In-place update for growing incomplete block (e.g. open code fence)
		if ((commonCount < BlockGroups.Count)
			&& (commonCount < parsedGroups.Count)
			&& BlockGroups[commonCount].MatchesGrowingTail(parsedGroups[commonCount]))
		{
			var existing = BlockGroups[commonCount];
			var updated = parsedGroups[commonCount];
			existing.UpdateFrom(updated, documentLength, _markdownBlockPool);
			updated.Reset();
			_blockGroupPool.Enqueue(updated);
			commonCount++;
		}

		for (var i = BlockGroups.Count - 1; i >= commonCount; i--)
		{
			var group = BlockGroups[i];
			BlockGroups.RemoveAt(i);
			group.Reset(_markdownBlockPool);
			_blockGroupPool.Enqueue(group);
		}

		for (var i = commonCount; i < parsedGroups.Count; i++)
		{
			BlockGroups.Add(parsedGroups[i]);
		}
	}

	private MarkdownBlockGroup RentGroup()
	{
		return _blockGroupPool.TryDequeue(out var pooled) ? pooled : new MarkdownBlockGroup();
	}

	private bool IsNearBottom(double thresholdPixels = 32)
	{
		if (_scrollViewer is null)
		{
			return true;
		}

		var maxOffset = Math.Max(0, _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height);
		return (maxOffset - _scrollViewer.Offset.Y) <= thresholdPixels;
	}

	/// <summary>
	/// Queue ScrollToEnd after the next layout pass so new blocks are in Extent.
	/// Skipped while a home scroll is pending (docs open must not jump to bottom).
	/// </summary>
	private void ScheduleScrollToEnd()
	{
		if (_pendingScrollHome)
		{
			return;
		}

		global::Avalonia.Threading.Dispatcher.UIThread.Post(
			() =>
			{
				if (_pendingScrollHome || !AutoScroll)
				{
					return;
				}

				ScrollToEnd();
			},
			global::Avalonia.Threading.DispatcherPriority.Loaded);
	}

	private void ScheduleScrollHome()
	{
		global::Avalonia.Threading.Dispatcher.UIThread.Post(
			() => TryApplyPendingScrollHome(finalize: false),
			global::Avalonia.Threading.DispatcherPriority.Loaded);
		// Finalize after markdown throttle (~100ms) + presenters measure.
		global::Avalonia.Threading.Dispatcher.UIThread.Post(
			() => TryApplyPendingScrollHome(finalize: true),
			global::Avalonia.Threading.DispatcherPriority.Background);
	}

	private void ScrollViewerOnScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		if (_isProgrammaticScroll)
		{
			return;
		}

		// Content grew while stick-to-bottom: re-pin after layout (not while docs force home).
		if (!_pendingScrollHome && AutoScroll && (e.ExtentDelta.Y > 0))
		{
			ScheduleScrollToEnd();
			return;
		}

		// Real user scroll-away from bottom (no extent growth this event).
		if ((e.OffsetDelta.Y < 0)
			&& (Math.Abs(e.ExtentDelta.Y) < 0.5)
			&& !IsNearBottom())
		{
			AutoScroll = false;
		}
	}

	private void ThrottleRender()
	{
		// Snapshot once per throttle tick for parse + presenters (Document may still append).
		var snapshot = Document.ToString();
		SourceSnapshot = snapshot;

		// Parser reads Document.Buffer live; snapshot is for presenters/copy.
		// Re-bind parser buffer is already Document.Buffer — Process from start.
		var parsedBlocks = _markdownParser.Process().ToList();
		var documentLength = Document.DocumentLength;
		var parsedGroups = BuildGroups(parsedBlocks, documentLength);

		this.DispatchAsync(() =>
		{
			ReconcileGroups(parsedGroups, documentLength);
			RebuildBlocksFromGroups();

			// Home (docs navigation) wins over chat-style AutoScroll for this paint.
			if (_pendingScrollHome)
			{
				ScheduleScrollHome();
			}
			else if (AutoScroll)
			{
				ScheduleScrollToEnd();
			}
		});
	}

	private void TryApplyPendingScrollHome(bool finalize)
	{
		if (!_pendingScrollHome)
		{
			return;
		}

		if (_scrollViewer is null)
		{
			return;
		}

		_isProgrammaticScroll = true;
		try
		{
			_scrollViewer.InvalidateMeasure();
			_scrollViewer.InvalidateArrange();
			UpdateLayout();
			_scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, 0);
			if (finalize)
			{
				_pendingScrollHome = false;
			}
		}
		finally
		{
			global::Avalonia.Threading.Dispatcher.UIThread.Post(
				() => _isProgrammaticScroll = false,
				global::Avalonia.Threading.DispatcherPriority.Loaded);
		}
	}

	#endregion

	#region Events

	/// <summary>
	/// Raised when the user clicks a markdown link. Hosts (e.g. documentation reader) handle navigation.
	/// </summary>
	public event EventHandler<MarkdownLinkClickedEventArgs> LinkClicked;

	#endregion
}