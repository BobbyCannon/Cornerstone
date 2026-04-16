#region References

using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Cornerstone.Avalonia.Text.Margins;

#endregion

namespace Cornerstone.Avalonia.Text;

/// <summary>
/// The text editor control.
/// </summary>
public partial class TextEditor : CornerstoneTemplatedControl<TextEditorViewModel>
{
	#region Fields

	private readonly TextEditorTextInputMethodClient _imClient;
	private readonly LineNumberMargin _lineNumbers;
	private ScrollViewer _scrollViewer;

	#endregion

	#region Constructors

	public TextEditor()
	{
		ViewModel = new TextEditorViewModel();

		_imClient = new(this);
		_lineNumbers = new LineNumberMargin(this) { IsVisible = ViewModel.ShowLineNumbers };

		LeftMargins = [_lineNumbers];
		TextInputMethodClientRequestedEvent.AddClassHandler<TextEditor>((tb, e) => e.Client = tb._imClient);
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

	[DirectProperty]
	public bool HighlightCurrentLine
	{
		get => ViewModel.HighlightCurrentLine;
		set => ViewModel.HighlightCurrentLine = value;
	}

	[DirectProperty]
	public ScrollBarVisibility HorizontalScrollBarVisibility
	{
		get => ViewModel.WordWrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
		set => ViewModel.WordWrap = value is ScrollBarVisibility.Disabled or ScrollBarVisibility.Hidden;
	}

	[DirectProperty]
	public ObservableCollection<Control> LeftMargins { get; }

	[AttachedProperty]
	public partial TextRenderer Renderer { get; private set; }

	[DirectProperty]
	public bool ShowLineNumbers
	{
		get => ViewModel.ShowLineNumbers;
		set => ViewModel.ShowLineNumbers = value;
	}

	[StyledProperty]
	public partial bool ShowMargins { get; set; }

	[DirectProperty]
	public string Text
	{
		get => ViewModel.ToString();
		set => ViewModel.Load(value);
	}

	[DirectProperty]
	public bool WordWrap
	{
		get => ViewModel.WordWrap;
		set => ViewModel.WordWrap = value;
	}

	#endregion

	#region Methods

	public void ScrollToEnd()
	{
		_scrollViewer?.ScrollToEnd();
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		if (e.NameScope.Find("PART_ScrollViewer") is ScrollViewer scrollViewer)
		{
			_scrollViewer = scrollViewer;
			_scrollViewer.ScrollChanged += ScrollViewerOnScrollChanged;
		}
		if (e.NameScope.Find("PART_TextRenderer") is TextRenderer textRenderer)
		{
			// Override text renderers default viewmodel
			textRenderer.ViewModel = ViewModel;
			Renderer = textRenderer;
		}
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
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == DataContextProperty)
		{
			if (change.OldValue is TextEditorViewModel oldValue)
			{
				oldValue.DocumentChanged -= DocumentOnDocumentChanged;
				oldValue.PropertyChanged -= ViewModelOnPropertyChanged;
			}
			if (change.NewValue is TextEditorViewModel newValue)
			{
				newValue.DocumentChanged += DocumentOnDocumentChanged;
				newValue.PropertyChanged += ViewModelOnPropertyChanged;
				InvalidateMeasure();
			}
		}

		base.OnPropertyChanged(change);
	}

	protected override void OnTextInput(TextInputEventArgs e)
	{
		if (!e.Handled)
		{
			ViewModel.ProcessTextInput(e);
			e.Handled = true;
		}
		base.OnTextInput(e);
	}

	private void DocumentOnDocumentChanged(object sender, TextDocumentChangedArgs e)
	{
		if (e.Type == TextDocumentChangeType.Reset)
		{
			ViewModel.Caret.Reset();
			InvalidateMeasure();
		}
		else
		{
			_lineNumbers.InvalidateMeasure();
		}
	}

	private void ScrollViewerOnScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		_lineNumbers.InvalidateMeasure();
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
				_lineNumbers.IsVisible = ViewModel.ShowLineNumbers;
				UpdateShowMargins();
				break;
			}
		}
	}

	#endregion
}