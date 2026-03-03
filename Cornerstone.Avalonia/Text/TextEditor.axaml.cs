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

	#endregion

	#region Constructors

	public TextEditor()
	{
		ViewModel = new TextEditorViewModel();

		_imClient = new();
		_lineNumbers = new LineNumberMargin(this) { IsVisible = ViewModel.ShowLineNumbers };

		LeftMargins = [_lineNumbers];
		TextInputMethodClientRequestedEvent.AddClassHandler<TextEditor>((tb, e) => e.Client = tb._imClient);
	}

	static TextEditor()
	{
		AffectsRender<TextRenderer>(
			ForegroundProperty
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
	public ScrollBarVisibility HorizontalScrollBarVisibility
	{
		get => ViewModel.WordWrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
		set => ViewModel.WordWrap = value != ScrollBarVisibility.Disabled;
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
		get => ViewModel.Document.ToString();
		set => ViewModel.Document.Load(value);
	}

	#endregion

	#region Methods

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		if (e.NameScope.Find("PART_ScrollViewer") is ScrollViewer scrollViewer)
		{
			scrollViewer.ScrollChanged += ScrollViewerOnScrollChanged;
		}
		if (e.NameScope.Find("PART_TextRenderer") is TextRenderer textRenderer)
		{
			Renderer = textRenderer;
		}
	}

	protected override void OnGotFocus(GotFocusEventArgs e)
	{
		ViewModel.Caret.IsVisible = true;
		base.OnGotFocus(e);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		e.Handled = ViewModel.ProcessKeyDownEvent(e);
		base.OnKeyDown(e);
	}

	protected override void OnKeyUp(KeyEventArgs e)
	{
		e.Handled = ViewModel.ProcessKeyUpEvent(e);
		base.OnKeyUp(e);
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);
		UpdateShowMargins();
	}

	protected override void OnLostFocus(RoutedEventArgs e)
	{
		ViewModel.Caret.IsVisible = false;
		base.OnLostFocus(e);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == ViewModelProperty)
		{
			if (change.OldValue is TextEditorViewModel oldValue)
			{
				oldValue.PropertyChanged -= ViewModelOnPropertyChanged;
				oldValue.Document.DocumentChanged -= DocumentOnDocumentChanged;
			}
			if (change.NewValue is TextEditorViewModel newValue)
			{
				newValue.PropertyChanged += ViewModelOnPropertyChanged;
				newValue.Document.DocumentChanged += DocumentOnDocumentChanged;
			}
		}

		base.OnPropertyChanged(change);
	}

	protected override void OnTextInput(TextInputEventArgs e)
	{
		if (!e.Handled)
		{
			ViewModel.ProcessTextInput(e);
		}
		base.OnTextInput(e);
	}

	private void DocumentOnDocumentChanged(object sender, TextDocumentChangedArgs e)
	{
		_lineNumbers.InvalidateMeasure();
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