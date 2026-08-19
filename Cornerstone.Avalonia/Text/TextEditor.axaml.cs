#region References

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Cornerstone.Avalonia.Text.Margins;
using Cornerstone.Avalonia.Text.Models;

#endregion

namespace Cornerstone.Avalonia.Text;

/// <summary>
/// The text editor control.
/// </summary>
public partial class TextEditor : TextEditor<TextEditorViewModel>
{
}

public partial class TextEditor<T> : CornerstoneTemplatedControl<T>
	where T : TextEditorViewModel, new()
{
	#region Fields

	private readonly TextEditorTextInputMethodClient<T> _imClient;

	/// <summary>
	/// True while we are programmatically pinning to the bottom. ScrollChanged in that
	/// window must not clear <see cref="AutoScroll" />.
	/// </summary>
	private bool _isProgrammaticScroll;

	#endregion

	#region Constructors

	public TextEditor()
	{
		ViewModel = new T();

		_imClient = new(this);

		LeftMargins = [];
		TextInputMethodClientRequestedEvent.AddClassHandler<TextEditor>((tb, e) => e.Client = tb._imClient);

		TextOptions.SetTextOptions(this, new TextOptions
		{
			TextRenderingMode = TextRenderingMode.SubpixelAntialias,
			TextHintingMode = TextHintingMode.Strong,
			BaselinePixelAlignment = BaselinePixelAlignment.Aligned
		});
	}

	static TextEditor()
	{
		AffectsRender<TextRenderer>(
			BackgroundProperty,
			CornerRadiusProperty,
			ForegroundProperty,
			WordWrapProperty
		);

		AffectsMeasure<TextRenderer>(
			HorizontalScrollBarVisibilityProperty,
			FontFamilyProperty,
			FontSizeProperty,
			FontStyleProperty,
			FontWeightProperty
		);
	}

	#endregion

	#region Properties

	/// <summary>
	/// When true, document growth keeps the viewport pinned to the bottom.
	/// Cleared only when the user scrolls away from the bottom (not on programmatic ScrollToEnd).
	/// </summary>
	[StyledProperty(DefaultValue = true)]
	public partial bool AutoScroll { get; set; }

	[DirectProperty]
	public bool HighlightCurrentLine
	{
		get => GetViewModel().HighlightCurrentLine;
		set => GetViewModel().HighlightCurrentLine = value;
	}

	[DirectProperty]
	public ScrollBarVisibility HorizontalScrollBarVisibility
	{
		get => GetViewModel().WordWrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
		set => GetViewModel().WordWrap = value is ScrollBarVisibility.Disabled or ScrollBarVisibility.Hidden;
	}

	[DirectProperty]
	public bool IsReadOnly
	{
		get => GetViewModel().IsReadOnly;
		set => GetViewModel().IsReadOnly = value;
	}

	[DirectProperty]
	public ObservableCollection<Control> LeftMargins { get; }

	[AttachedProperty]
	public partial TextRenderer Renderer { get; private set; }

	[DirectProperty]
	public bool ShowLineNumbers
	{
		get => GetViewModel().ShowLineNumbers;
		set => GetViewModel().ShowLineNumbers = value;
	}

	[StyledProperty(DefaultValue = true)]
	public partial bool ShowMargins { get; set; }

	[DirectProperty]
	public string Text
	{
		get => GetViewModel().ToString();
		set => GetViewModel().Load(value);
	}

	[DirectProperty]
	public bool WordWrap
	{
		get => GetViewModel().WordWrap;
		set => GetViewModel().WordWrap = value;
	}

	protected Popup CompletionPopup { get; private set; }

	protected ScrollViewer ScrollViewer { get; private set; }

	#endregion

	#region Methods

	public virtual void Clear()
	{
		ViewModel.Clear();
	}

	public void ScrollToEnd()
	{
		if (ScrollViewer is null)
		{
			return;
		}

		_isProgrammaticScroll = true;
		try
		{
			// Force layout so Extent includes the latest document/lines before pinning.
			ScrollViewer.InvalidateMeasure();
			ScrollViewer.InvalidateArrange();
			UpdateLayout();

			var maxY = Math.Max(0, ScrollViewer.Extent.Height - ScrollViewer.Viewport.Height);
			ScrollViewer.Offset = new Vector(ScrollViewer.Offset.X, maxY);
			ScrollViewer.ScrollToEnd();
		}
		finally
		{
			// Keep suppress until after layout ScrollChanged has been processed.
			Dispatcher.UIThread.Post(
				() => _isProgrammaticScroll = false,
				DispatcherPriority.Loaded);
		}
	}

	public void ScrollToHome()
	{
		ScrollViewer?.ScrollToHome();
	}

	public void ScrollToLine(int lineNumber)
	{
		if (ViewModel.Lines.TryGetLine(lineNumber, out var line))
		{
			ScrollViewer?.Offset = new(ScrollViewer.Offset.X, line.VisualLayout.Y);
		}
	}

	public void ScrollToOffset(int offset)
	{
		if (ViewModel.Lines.TryGetLineForOffset(offset, out var line))
		{
			ScrollViewer?.Offset = new(ScrollViewer.Offset.X, line.VisualLayout.Y);
		}
	}

	/// <summary>
	/// Ensures the ViewModel exists and returns it.
	/// Use this in property getters/setters.
	/// </summary>
	protected override T GetViewModel()
	{
		EnsureViewModel();
		return ViewModel;
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		EnsureViewModel();

		if (e.NameScope.Find("PART_ScrollViewer") is ScrollViewer scrollViewer)
		{
			ScrollViewer = scrollViewer;
			ScrollViewer?.HorizontalScrollBarVisibility = ViewModel.WordWrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
			AttachScrollViewer();
		}
		if (e.NameScope.Find("PART_TextRenderer") is TextRenderer textRenderer)
		{
			Renderer = textRenderer;
		}

		if (CompletionPopup != null)
		{
			CompletionPopup.Closed -= CompletionPopupOnClosed;
		}

		CompletionPopup = e.NameScope.Find("PART_CompletionPopup") as Popup;
		if (CompletionPopup != null)
		{
			CompletionPopup.PlacementTarget = Renderer;
			CompletionPopup.Closed += CompletionPopupOnClosed;
		}

		if (e.NameScope.Find("PART_CompletionList") is ListBox completionList)
		{
			completionList.DoubleTapped -= CompletionListOnDoubleTapped;
			completionList.DoubleTapped += CompletionListOnDoubleTapped;
		}

		// Create margins here (after we know we have a ViewModel)
		if (LeftMargins.Count == 0)
		{
			LeftMargins.Add(new LineNumberMargin<T>(this)
			{
				IsVisible = ViewModel.ShowLineNumbers
			});
		}

		UpdateShowMargins();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		EnsureViewModel();
		AttachViewModel(ViewModel);
		AttachScrollViewer();

		// TabControl often defers visual tree for inactive tabs; re-pin when shown again.
		if (AutoScroll)
		{
			ScheduleScrollToEnd();
		}
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		DetachViewModel(ViewModel);
		DetachScrollViewer();
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnGotFocus(FocusChangedEventArgs e)
	{
		// Just pass focus to the renderer.
		base.OnGotFocus(e);
		Renderer.Focus();
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);
		UpdateShowMargins();

		// ScrollChanged for subclasses is wired in AttachScrollViewer (with PART_ScrollViewer),
		// not here — OnLoaded often runs before the template, so ScrollViewer is still null.
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == AutoScrollProperty)
		{
			if (AutoScroll)
			{
				ScrollToEnd();
			}
		}
		else if (change.Property == ProfilerProperty)
		{
			ViewModel?.Profiler = Profiler;
		}
		else if (change.Property == ViewModelProperty)
		{
			var oldValue = change.OldValue as TextEditorViewModel;
			var newValue = change.NewValue as TextEditorViewModel;

			DetachViewModel(oldValue);
			AttachViewModel(newValue);
			InvalidateMeasure();

			ViewModel?.Profiler = Profiler;
		}

		base.OnPropertyChanged(change);
	}

	protected virtual void OnScrollChanged(object sender, ScrollChangedEventArgs e)
	{
	}

	protected override void OnTextInput(TextInputEventArgs e)
	{
		if (IsReadOnly || e.Handled)
		{
			e.Handled = true;
			base.OnTextInput(e);
			return;
		}

		ViewModel.ProcessTextInput(e.Text);
		e.Handled = true;
		base.OnTextInput(e);
	}

	protected override void OnUnloaded(RoutedEventArgs e)
	{
		base.OnUnloaded(e);
	}

	private void AttachScrollViewer()
	{
		// Attempt remove then ensure attached (template apply + visual-tree attach).
		// Subclass sync hooks OnScrollChanged via ScrollViewerOnScrollChanged — do not
		// subscribe OnScrollChanged only from OnLoaded (ScrollViewer is often still null).
		ScrollViewer?.ScrollChanged -= ScrollViewerOnScrollChanged;
		ScrollViewer?.ScrollChanged += ScrollViewerOnScrollChanged;
	}

	private void AttachViewModel(TextEditorViewModel vm)
	{
		if (vm == null)
		{
			return;
		}

		vm.DocumentChanged -= DocumentOnDocumentChanged;
		vm.DocumentChanged += DocumentOnDocumentChanged;

		vm.PropertyChanged -= ViewModelOnPropertyChanged;
		vm.PropertyChanged += ViewModelOnPropertyChanged;
		vm.CompletionManager.Dispatcher = GetDispatcher();
		AttachCompletionManager(vm.CompletionManager);
	}

	private void DetachScrollViewer()
	{
		// Attempt remove then ensure attached.
		ScrollViewer?.ScrollChanged -= ScrollViewerOnScrollChanged;
	}

	private void DetachViewModel(TextEditorViewModel vm)
	{
		if (vm == null)
		{
			return;
		}

		vm.DocumentChanged -= DocumentOnDocumentChanged;
		vm.PropertyChanged -= ViewModelOnPropertyChanged;
		DetachCompletionManager(vm.CompletionManager);
	}

	private void DocumentOnDocumentChanged(object sender, TextDocumentChangedArgs e)
	{
		if (e.Type == TextDocumentChangeType.Reset)
		{
			ViewModel.Caret.Reset();
			InvalidateMeasure();
		}
		else if (!ViewModel.Lines.LastEditNeedsPaintOnly)
		{
			foreach (var leftMargin in LeftMargins)
			{
				leftMargin.InvalidateMeasure();
			}
		}

		// Host output inserted before a live prompt: keep the prompt line on screen.
		if (e.PinViewport)
		{
			SchedulePinViewport(e.Text);
			return;
		}

		// In-place last-line edits do not grow height; skip ScrollToEnd/UpdateLayout.
		if (AutoScroll && !ViewModel.Lines.LastEditNeedsPaintOnly)
		{
			ScheduleScrollToEnd();
		}
	}

	private void EnsureViewModel()
	{
		ViewModel ??= new T();
	}

	private bool IsNearBottom(double thresholdPixels = 32)
	{
		var scrollViewer = ScrollViewer;
		if (scrollViewer is null)
		{
			return true;
		}

		var maxOffset = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
		return (maxOffset - scrollViewer.Offset.Y) <= thresholdPixels;
	}

	/// <summary>
	/// After a before-prompt insert, add the inserted height to the scroll offset
	/// so the prompt / input line does not jump.
	/// </summary>
	private void SchedulePinViewport(string insertedText)
	{
		var lineHeight = ViewModel?.ViewMetrics.CharacterHeight ?? 0;
		var lineBreaks = CountNewlines(insertedText);
		var addedHeight = lineHeight * lineBreaks;

		Dispatcher.UIThread.Post(
			() =>
			{
				if (ScrollViewer is null)
				{
					return;
				}

				_isProgrammaticScroll = true;
				try
				{
					if (addedHeight > 0)
					{
						ScrollViewer.Offset = new Vector(
							ScrollViewer.Offset.X,
							ScrollViewer.Offset.Y + addedHeight);
					}
				}
				finally
				{
					Dispatcher.UIThread.Post(
						() => _isProgrammaticScroll = false,
						DispatcherPriority.Loaded);
				}
			},
			DispatcherPriority.Loaded);
	}

	private static int CountNewlines(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}

		var count = 0;
		for (var i = 0; i < text.Length; i++)
		{
			if (text[i] == '\n')
			{
				count++;
			}
		}

		return count;
	}

	/// <summary>
	/// Queue ScrollToEnd after the next layout pass so new lines are in Extent.
	/// </summary>
	private void ScheduleScrollToEnd()
	{
		Dispatcher.UIThread.Post(
			() =>
			{
				if (AutoScroll)
				{
					ScrollToEnd();
				}
			},
			DispatcherPriority.Loaded);
	}

	private void ScrollViewerOnScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		if (_isProgrammaticScroll)
		{
			// still allow margin invalidation below
		}

		// Content grew while stick-to-bottom: re-pin (Extent change, not user scroll).
		else if (AutoScroll && (e.ExtentDelta.Y > 0))
		{
			ScheduleScrollToEnd();
		}

		// Real user scroll-away from bottom (no extent growth this event).
		else if ((e.OffsetDelta.Y < 0)
				&& (Math.Abs(e.ExtentDelta.Y) < 0.5)
				&& !IsNearBottom())
		{
			AutoScroll = false;
		}

		foreach (var leftMargin in LeftMargins)
		{
			leftMargin.InvalidateMeasure();
		}

		// Diff sync and other overrides — always invoked when the ScrollViewer is attached,
		// not only when OnLoaded happened to see a non-null ScrollViewer.
		OnScrollChanged(sender, e);
	}

	private void AttachCompletionManager(CompletionManager manager)
	{
		if (manager == null)
		{
			return;
		}

		manager.PropertyChanged -= CompletionManagerOnPropertyChanged;
		manager.PropertyChanged += CompletionManagerOnPropertyChanged;
		UpdateCompletionPopup();
	}

	private void CompletionListOnDoubleTapped(object sender, TappedEventArgs e)
	{
		ViewModel?.CompletionManager.ApplySelected();
	}

	private void CompletionManagerOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if ((e.PropertyName == nameof(CompletionManager.IsOpen))
			|| (e.PropertyName == nameof(CompletionManager.VisibleItems)))
		{
			UpdateCompletionPopup();
		}
	}

	private void CompletionPopupOnClosed(object sender, EventArgs e)
	{
		if (ViewModel?.CompletionManager.IsOpen == true)
		{
			ViewModel.CompletionManager.Close();
		}
	}

	private void DetachCompletionManager(CompletionManager manager)
	{
		if (manager == null)
		{
			return;
		}

		manager.PropertyChanged -= CompletionManagerOnPropertyChanged;
	}

	private void UpdateCompletionPopup()
	{
		var popup = CompletionPopup;
		var manager = ViewModel?.CompletionManager;
		if ((popup == null) || (manager == null))
		{
			return;
		}

		if (!manager.IsOpen || (manager.VisibleItems.Count == 0))
		{
			popup.IsOpen = false;
			return;
		}

		popup.PlacementTarget = Renderer;
		popup.Placement = PlacementMode.AnchorAndGravity;
		popup.PlacementAnchor = PopupAnchor.TopLeft;
		popup.PlacementGravity = PopupGravity.BottomRight;

		var caret = ViewModel.Caret.VisualLayout;
		var scroll = ScrollViewer?.Offset ?? default;
		popup.HorizontalOffset = caret.X - scroll.X;
		popup.VerticalOffset = caret.Bottom - scroll.Y;
		popup.IsOpen = true;
	}

	private void UpdateShowMargins()
	{
		ShowMargins = ShowLineNumbers;
	}

	private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(ViewModel.ShowLineNumbers):
			{
				foreach (var leftMargin in LeftMargins)
				{
					if (leftMargin is LineNumberMargin<T> lineNumberMargin)
					{
						lineNumberMargin.IsVisible = ViewModel.ShowLineNumbers;
					}
				}

				UpdateShowMargins();
				break;
			}
			case nameof(ViewModel.WordWrap):
			{
				ScrollViewer?.HorizontalScrollBarVisibility = ViewModel.WordWrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
				break;
			}
		}
	}

	#endregion
}