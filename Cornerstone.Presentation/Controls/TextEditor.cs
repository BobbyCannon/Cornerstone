#region References

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Primitives.PopupPositioning;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Presentation.Controls.Text.Models;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.Controls.Scrolling;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Text;

using Dispatcher = Cornerstone.Presentation.Threading.Dispatcher;
using DispatcherPriority = Cornerstone.Presentation.DispatcherPriority;

#endregion

namespace Cornerstone.Presentation.Controls;

/// <summary>
/// The text editor control.
/// </summary>
public partial class TextEditor : TextEditor<TextEditorViewModel>
{
}

public partial class TextEditor<T> : TemplatedControl<T>, IDispatchable
	where T : TextEditorViewModel, new()
{
	#region Properties

	[StyledProperty]
	public partial Profiler Profiler { get; set; }

	#endregion

	#region Fields

	private readonly TextEditorTextInputMethodClient<T> _imClient;

	/// <summary>
	/// True while we are programmatically pinning to the bottom. ScrollChanged in that
	/// window must not clear AutoScroll.
	/// </summary>
	private bool _isProgrammaticScroll;

	/// <summary>
	/// True while applying ViewMetrics.Offset after attach/layout. Extent growth in
	/// that window must not AutoScroll to the end.
	/// </summary>
	private bool _isRestoringScroll;


#endregion

	#region Constructors

	public IDispatcher GetDispatcher()
	{
		// CurrentDispatcher builds a dispatcher for the calling thread. The editor
		// was created on the UI thread, and VerifyAccess checks that one.
		return Dispatcher;
	}

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
	/// Forwards <see cref="TextEditorViewModel.AutoScroll"/>.
	/// </summary>
	[DirectProperty(nameof(TextEditorViewModel.AutoScroll))]
	public partial bool AutoScroll { get; set; }

	[DirectProperty(nameof(TextEditorViewModel.HighlightCurrentLine))]
	public partial bool HighlightCurrentLine { get; set; }

	[DirectProperty]
	public ScrollBarVisibility HorizontalScrollBarVisibility
	{
		get => GetViewModel().WordWrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
		set => GetViewModel().WordWrap = value is ScrollBarVisibility.Disabled or ScrollBarVisibility.Hidden;
	}

	[DirectProperty(nameof(TextEditorViewModel.IsReadOnly))]
	public partial bool IsReadOnly { get; set; }

	[DirectProperty]
	public ObservableCollection<Control> LeftMargins { get; }

	[AttachedProperty]
	public partial TextRenderer Renderer { get; private set; }

	[DirectProperty(nameof(TextEditorViewModel.ShowLineNumbers))]
	public partial bool ShowLineNumbers { get; set; }

	[StyledProperty(DefaultValue = true)]
	public partial bool ShowMargins { get; set; }

	[DirectProperty]
	public string Text
	{
		get => GetViewModel().ToString();
		set => GetViewModel().Load(value);
	}

	[DirectProperty(nameof(TextEditorViewModel.WordWrap))]
	public partial bool WordWrap { get; set; }

	protected Popup CompletionPopup { get; private set; }

	protected ScrollViewer ScrollViewer { get; private set; }


#endregion

	#region Methods

	public virtual void Clear()
	{
		ViewModel.Clear();
	}

	public Vector GetScrollOffset()
	{
		if (HasStableScrollViewport())
		{
			return ScrollViewer.Offset;
		}

		return ViewModel?.ViewMetrics.Offset ?? default;
	}

	/// <summary>
	/// True when the inner ScrollViewer has a real viewport (not collapsed by an inactive tab).
	/// </summary>
	public bool HasStableScrollViewport()
	{
		return (ScrollViewer != null)
			&& (ScrollViewer.Viewport.Height > 1)
			&& (ScrollViewer.Extent.Height > 1);
	}

	public void SetScrollOffset(Vector offset)
	{
		if (ViewModel != null)
		{
			ViewModel.ViewMetrics.Offset = offset;
		}

		if (ScrollViewer is null)
		{
			return;
		}

		_isProgrammaticScroll = true;
		try
		{
			if (Renderer != null)
			{
				Renderer.Offset = offset;
			}

			ScrollViewer.Offset = offset;
		}
		finally
		{
			Dispatcher.UIThread.Post(
				() => _isProgrammaticScroll = false,
				DispatcherPriority.Loaded);
		}
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

			// Pin to the bottom only. ScrollViewer.ScrollToEnd() uses X = -∞
			// (coerced to 0), which fights EnsureCaretVisible and flashes the
			// text right then left — Terminal AutoScroll hits this on every line.
			var maxY = Math.Max(0, ScrollViewer.Extent.Height - ScrollViewer.Viewport.Height);
			var offset = new Vector(ScrollViewer.Offset.X, maxY);
			ScrollViewer.Offset = offset;
			if (Renderer != null)
			{
				Renderer.Offset = offset;
			}
			FollowCaretIfAutoScrolling();
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
		var scrollViewer = ScrollViewer;
		if ((scrollViewer == null) || (ViewModel?.Lines == null))
		{
			return;
		}

		if (ViewModel.Lines.TryGetLine(lineNumber, out var line))
		{
			scrollViewer.Offset = new(scrollViewer.Offset.X, line.VisualLayout.Y);
		}
	}

	public void ScrollToOffset(int offset)
	{
		var scrollViewer = ScrollViewer;
		if ((scrollViewer == null) || (ViewModel?.Lines == null))
		{
			return;
		}

		if (ViewModel.Lines.TryGetLineForOffset(offset, out var line))
		{
			scrollViewer.Offset = new(scrollViewer.Offset.X, line.VisualLayout.Y);
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

		EnsureLeftMargins();
		UpdateShowMargins();
		ScheduleApplyViewModelScroll();
	}

	protected virtual void EnsureLeftMargins()
	{
		if (LeftMargins.Count != 0)
		{
			return;
		}

		LeftMargins.Add(new LineNumberMargin<T>(this) { IsVisible = ViewModel.ShowLineNumbers });
		LeftMargins.Add(new FoldingMargin<T>(this));
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		EnsureViewModel();
		AttachViewModel(ViewModel);
		AttachScrollViewer();
		ScheduleApplyViewModelScroll();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		PersistScrollOffsetToViewModel();
		if (ViewModel != null)
		{
			ViewModel.IsRestoringViewport = true;
		}

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

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		if (change.Property == ProfilerProperty)
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
			ScheduleApplyViewModelScroll();
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

		vm.FoldingManager.FoldingsChanged -= FoldingsOnChanged;
		vm.FoldingManager.FoldingsChanged += FoldingsOnChanged;

		vm.PropertyChanged -= ViewModelOnPropertyChanged;
		vm.PropertyChanged += ViewModelOnPropertyChanged;
		vm.FocusRequested -= ViewModelOnFocusRequested;
		vm.FocusRequested += ViewModelOnFocusRequested;
		vm.CompletionManager.Dispatcher = GetDispatcher();
		AttachCompletionManager(vm.CompletionManager);
		SyncForwardedViewModelProperties(null);
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
		vm.FoldingManager.FoldingsChanged -= FoldingsOnChanged;
		vm.PropertyChanged -= ViewModelOnPropertyChanged;
		vm.FocusRequested -= ViewModelOnFocusRequested;
		DetachCompletionManager(vm.CompletionManager);
	}

	private void FoldingsOnChanged(object sender, EventArgs e)
	{
		Renderer?.InvalidateMeasure();
		foreach (var leftMargin in LeftMargins)
		{
			leftMargin.InvalidateMeasure();
			leftMargin.InvalidateVisual();
		}
	}

	private void DocumentOnDocumentChanged(object sender, TextDocumentChangedArgs e)
	{
		RaisePropertyChanged(TextProperty, null, ViewModel.ToString());

		if (e.Type == TextDocumentChangeType.Reset)
		{
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
			FollowCaretIfAutoScrolling();
			return;
		}

		// In-place last-line edits do not grow height; skip ScrollToEnd/UpdateLayout.
		if (AutoScroll && !ViewModel.Lines.LastEditNeedsPaintOnly)
		{
			ScheduleScrollToEnd();
		}

		FollowCaretIfAutoScrolling();
	}

	/// <summary>
	/// AutoScroll follows the end of the buffer with both the viewport and the caret,
	/// unless the user is selecting.
	/// </summary>
	private void FollowCaretIfAutoScrolling()
	{
		if (!AutoScroll || (ViewModel == null))
		{
			return;
		}

		if (ViewModel.Caret.Selection.Length > 0)
		{
			return;
		}

		var end = ViewModel.DocumentLength;
		if (ViewModel.Caret.Offset != end)
		{
			ViewModel.Caret.Move(end);
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
		// Layout after a tab switch: apply the saved offset (or pin to end) once
		// the ScrollViewer has a real extent. Do this before treating OffsetDelta
		// as a user scroll-away — coerce-to-zero would turn AutoScroll off.
		if (_isRestoringScroll && !_isProgrammaticScroll && HasStableScrollViewport())
		{
			ApplyViewModelScroll();
		}

		if (!_isProgrammaticScroll && !_isRestoringScroll)
		{
			if (AutoScroll && (e.ExtentDelta.Y > 0))
			{
				ScheduleScrollToEnd();
			}
			else if ((e.OffsetDelta.Y < 0)
					&& (Math.Abs(e.ExtentDelta.Y) < 0.5)
					&& !IsNearBottom())
			{
				AutoScroll = false;
			}
		}

		foreach (var leftMargin in LeftMargins)
		{
			// Scroll only moves which numbers are painted; gutter width is digit count.
			// InvalidateMeasure here makes the Auto column relayout every caret/extent
			// tick and the text jumps right then left.
			leftMargin.InvalidateVisual();
		}

		// Diff sync and other overrides — always invoked when the ScrollViewer is attached,
		// not only when OnLoaded happened to see a non-null ScrollViewer.
		OnScrollChanged(sender, e);

		if (!_isProgrammaticScroll && !_isRestoringScroll)
		{
			PersistScrollOffsetToViewModel();
			ScrollOffsetChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private void PersistScrollOffsetToViewModel()
	{
		if ((ViewModel == null) || ViewModel.IsRestoringViewport || !HasStableScrollViewport())
		{
			return;
		}

		ViewModel.ViewMetrics.Offset = ScrollViewer.Offset;
	}

	private void ScheduleApplyViewModelScroll()
	{
		if (ViewModel == null)
		{
			return;
		}

		_isRestoringScroll = true;
		ViewModel.IsRestoringViewport = true;
		Dispatcher.UIThread.Post(ApplyViewModelScroll, DispatcherPriority.Loaded);
	}

	private void ApplyViewModelScroll()
	{
		if ((ViewModel == null) || (ScrollViewer is null) || !HasStableScrollViewport())
		{
			return;
		}

		// Saved scroll needs a document taller than the pane. First measure often
		// reports a tiny extent that would clamp the offset to 0.
		if ((ViewModel.ViewMetrics.Offset.Y > 1)
			&& (ScrollViewer.Extent.Height <= (ScrollViewer.Viewport.Height + 1)))
		{
			return;
		}

		_isProgrammaticScroll = true;
		try
		{
			if (AutoScroll)
			{
				ScrollToEnd();
				ViewModel.ViewMetrics.Offset = ScrollViewer.Offset;
			}
			else
			{
				var offset = ViewModel.ViewMetrics.ClampOffset(ScrollViewer.Extent, ScrollViewer.Viewport);
				if (Renderer != null)
				{
					Renderer.Offset = offset;
				}

				ScrollViewer.Offset = offset;
			}

			_isRestoringScroll = false;
			ViewModel.IsRestoringViewport = false;
		}
		finally
		{
			Dispatcher.UIThread.Post(
				() => _isProgrammaticScroll = false,
				DispatcherPriority.Loaded);
		}
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
		popup.OverlayDismissEventPassThrough = true;
		popup.IsOpen = true;
		Renderer?.Focus();
	}

	private void UpdateShowMargins()
	{
		foreach (var leftMargin in LeftMargins)
		{
			if (leftMargin.IsVisible)
			{
				ShowMargins = true;
				return;
			}
		}

		ShowMargins = false;
	}

	private void ViewModelOnFocusRequested(object sender, EventArgs e)
	{
		if (!this.IsAttachedToVisualTree())
		{
			return;
		}

		// After the current pointer/key event so a grid double-tap cannot steal focus back.
		Dispatcher.UIThread.Post(ApplyRequestedFocus, DispatcherPriority.Input);
	}

	private void ApplyRequestedFocus()
	{
		if (!this.IsAttachedToVisualTree())
		{
			return;
		}

		Focus();
		Renderer?.Focus();
	}

	private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		SyncForwardedViewModelProperties(e.PropertyName);

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
				var visibility = ViewModel.WordWrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
				RaisePropertyChanged(
					HorizontalScrollBarVisibilityProperty,
					visibility == ScrollBarVisibility.Disabled ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
					visibility);
				ScrollViewer?.HorizontalScrollBarVisibility = visibility;
				break;
			}
			case nameof(TextEditorViewModel.AutoScroll):
			{
				if (ViewModel.AutoScroll)
				{
					FollowCaretIfAutoScrolling();
					ScrollToEnd();
				}

				break;
			}
		}
	}


#endregion

	#region Events

	public event EventHandler ScrollOffsetChanged;


#endregion
}
