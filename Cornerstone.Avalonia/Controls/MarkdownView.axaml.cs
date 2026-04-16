#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Cornerstone.Collections;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Markdown;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Avalonia.Controls;

public partial class MarkdownView : CornerstoneTemplatedControl
{
	#region Fields

	private readonly IQueue<MarkdownBlockGroup> _blockGroupPool;
	private readonly DebounceThrottleManager _manager;
	private readonly IQueue<Block> _markdownBlockPool;
	private readonly IStringBuffer _markdownBuffer;
	private readonly MarkdownParser _markdownParser;
	private ScrollViewer _scrollViewer;
	private readonly ThrottleProxy _throttle;

	#endregion

	#region Constructors

	public MarkdownView()
	{
		_manager = DebounceThrottleManager.Create(GetInstance<IDateTimeProvider>(), 50);
		_markdownBuffer = new StringBuffer();
		_markdownBlockPool = new SpeedyQueue<Block>();
		_blockGroupPool = new SpeedyQueue<MarkdownBlockGroup>();
		_markdownParser = new MarkdownParser(_markdownBuffer, _markdownBlockPool);
		_throttle = _manager.CreateThrottle(TimeSpan.FromMilliseconds(100), ThrottleRender);

		Blocks = [];
		BlockGroups = [];
	}

	static MarkdownView()
	{
		AffectsMeasure<MarkdownView>(
			FontFamilyProperty,
			FontSizeProperty,
			FontStyleProperty,
			FontWeightProperty,
			MarkdownProperty,
			WordWrapProperty
		);
	}

	#endregion

	#region Properties

	[StyledProperty]
	public partial string Markdown { get; set; }

	[StyledProperty]
	public partial bool WordWrap { get; set; }

	[StyledProperty]
	protected partial ObservableCollection<MarkdownBlockGroup> BlockGroups { get; set; }

	[StyledProperty]
	protected partial ObservableCollection<Block> Blocks { get; set; }

	#endregion

	#region Methods

	public static bool IsBlockLevel(Block block)
	{
		return (block.Type == MarkdownTokenizer.TokenTypeBlockQuote)
			|| (block.Type == MarkdownTokenizer.TokenTypeCodeBlock)
			|| (block.Type == MarkdownTokenizer.TokenTypeHeader)
			|| (block.Type == MarkdownTokenizer.TokenTypeTable);
	}

	public void ScrollToEnd()
	{
		_scrollViewer?.ScrollToEnd();
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		_scrollViewer = e.NameScope.Find<ScrollViewer>("PARTS_ScrollViewer");
		if (_scrollViewer != null)
		{
			_scrollViewer.HorizontalScrollBarVisibility = WordWrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Visible;
		}
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		_manager.StartWorking();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_manager.StopWorking();
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == MarkdownProperty)
		{
			_throttle.Trigger();
		}
		else if (change.Property == WordWrapProperty)
		{
			if (_scrollViewer != null)
			{
				_scrollViewer.HorizontalScrollBarVisibility = WordWrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Visible;
				_throttle.Trigger();
			}
		}
	}

	private void ThrottleRender(CancellationToken token, object obj, bool force)
	{
		if (token.IsCancellationRequested)
		{
			return;
		}

		_markdownBuffer.Clear();

		this.Dispatch(() => _markdownBuffer.Append(Markdown));
		var parsedBlocks = _markdownParser.Process().ToList();

		for (var i = parsedBlocks.Count - 1; i >= 0; i--)
		{
			var b = parsedBlocks[i];
			if ((b.Type == Tokenizer.TokenTypeNewLine)
				|| (b.Type == Tokenizer.TokenTypeWhitespace))
			{
				parsedBlocks.RemoveAt(i);
				_markdownBlockPool.Enqueue(b);
			}
		}

		var parsedGroups = new List<MarkdownBlockGroup>((parsedBlocks.Count / 2) + 2);

		MarkdownBlockGroup currentParagraph = null;

		foreach (var block in parsedBlocks)
		{
			if (IsBlockLevel(block))
			{
				// Finish any open paragraph
				currentParagraph = null;

				// Single-block group
				var group = _blockGroupPool.TryDequeue(out var pooled) ? pooled : new MarkdownBlockGroup();
				group.Reset();
				group.Add(block);
				parsedGroups.Add(group);
			}
			else
			{
				// Inline element -> belongs in a paragraph group
				if (currentParagraph == null)
				{
					currentParagraph = _blockGroupPool.TryDequeue(out var pooled) ? pooled : new MarkdownBlockGroup();
					currentParagraph.Reset();
					parsedGroups.Add(currentParagraph);
				}
				currentParagraph.Add(block);
			}
		}

		this.DispatchAsync(() =>
		{
			var toRemove = Blocks.Except(parsedBlocks).ToArray();
			var toAdd = parsedBlocks.Except(Blocks).ToArray();
			var toPool = parsedBlocks.Intersect(Blocks).ToArray();

			foreach (var item in toPool)
			{
				_markdownBlockPool.Enqueue(item);
			}
			foreach (var item in toRemove)
			{
				Blocks.Remove(item);
				_markdownBlockPool.Enqueue(item);
			}
			foreach (var item in toAdd)
			{
				Blocks.Add(item);
			}

			// Update BlockGroups (pooled, low-allocation)
			// Remove & return old groups to pool
			for (var i = BlockGroups.Count - 1; i >= 0; i--)
			{
				var group = BlockGroups[i];
				BlockGroups.RemoveAt(i);
				group.Reset();
				_blockGroupPool.Enqueue(group);
			}

			// Add the newly grouped ones
			foreach (var group in parsedGroups)
			{
				BlockGroups.Add(group);
			}
		});
	}

	#endregion
}