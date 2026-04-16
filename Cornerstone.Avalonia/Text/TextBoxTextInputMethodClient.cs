#region References

using Avalonia;
using Avalonia.Input;
using Avalonia.Input.TextInput;

#endregion

namespace Cornerstone.Avalonia.Text;

internal class TextEditorTextInputMethodClient : TextInputMethodClient
{
	#region Fields

	private TextEditor _editor;

	#endregion

	#region Constructors

	public TextEditorTextInputMethodClient(TextEditor editor)
	{
		SetPresenter(editor);
	}

	#endregion

	#region Properties

	public override Rect CursorRectangle => default;

	public override TextSelection Selection
	{
		get => _editor.Renderer?.ViewModel.Caret.Selection;
		set => _editor.Renderer?.ViewModel.Caret.Selection.Update(value);
	}

	public override bool SupportsPreedit => true;

	public override bool SupportsSurroundingText => true;

	public override string SurroundingText => string.Empty;

	public override Visual TextViewVisual => _editor.Renderer;

	#endregion

	#region Methods

	public override void SetPreeditText(string preeditText)
	{
		SetPreeditText(preeditText, null);
	}

	public override void SetPreeditText(string preeditText, int? cursorPos)
	{
	}

	private void OnParentPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == TextEditor.TextProperty)
		{
			RaiseSurroundingTextChanged();
		}
	}

	private void OnParentTapped(object sender, TappedEventArgs e)
	{
		RaiseInputPaneActivationRequested();
	}

	private void SetPresenter(TextEditor editor)
	{
		if (_editor != null)
		{
			_editor.PropertyChanged -= OnParentPropertyChanged;
			_editor.Tapped -= OnParentTapped;
		}

		_editor = editor;

		if (_editor != null)
		{
			_editor.PropertyChanged += OnParentPropertyChanged;
			_editor.Tapped += OnParentTapped;
		}

		RaiseTextViewVisualChanged();
		RaiseCursorRectangleChanged();
	}

	#endregion
}