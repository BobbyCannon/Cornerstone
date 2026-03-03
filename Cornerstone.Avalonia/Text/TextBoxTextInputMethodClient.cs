#region References

using Avalonia;
using Avalonia.Input;
using Avalonia.Input.TextInput;

#endregion

namespace Cornerstone.Avalonia.Text;

internal class TextEditorTextInputMethodClient : TextInputMethodClient
{
	#region Fields

	private TextEditor _parent;
	private TextRenderer _presenter;

	#endregion

	#region Properties

	public override Rect CursorRectangle => default;

	public override TextSelection Selection
	{
		get => new();
		set { }
	}

	public override bool SupportsPreedit => true;

	public override bool SupportsSurroundingText => true;

	public override string SurroundingText => string.Empty;

	public override Visual TextViewVisual => _presenter!;

	#endregion

	#region Methods

	public override void SetPreeditText(string preeditText)
	{
		SetPreeditText(preeditText, null);
	}

	public override void SetPreeditText(string preeditText, int? cursorPos)
	{
		if ((_presenter == null) || (_parent == null))
		{
		}

		//_presenter.SetCurrentValue(TextRenderer.PreeditTextProperty, preeditText);
		//_presenter.SetCurrentValue(TextRenderer.PreeditTextCursorPositionProperty, cursorPos);
	}

	public void SetPresenter(TextRenderer presenter, TextEditor parent)
	{
		if (_parent != null)
		{
			_parent.PropertyChanged -= OnParentPropertyChanged;
			_parent.Tapped -= OnParentTapped;
		}

		_parent = parent;

		if (_parent != null)
		{
			_parent.PropertyChanged += OnParentPropertyChanged;
			_parent.Tapped += OnParentTapped;
		}

		var oldPresenter = _presenter;

		if (oldPresenter != null)
		{
			//oldPresenter.ClearValue(TextRenderer.PreeditTextProperty);
			//oldPresenter.CaretBoundsChanged -= (s, e) => RaiseCursorRectangleChanged();
		}

		_presenter = presenter;

		if (_presenter != null)
		{
			//_presenter.CaretBoundsChanged += (s, e) => RaiseCursorRectangleChanged();
		}

		RaiseTextViewVisualChanged();
		RaiseCursorRectangleChanged();
	}

	private void OnParentPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == TextEditor.TextProperty)
		{
			RaiseSurroundingTextChanged();
		}

		//if ((e.Property == TextEditor.SelectionStartProperty) || (e.Property == TextEditor.SelectionEndProperty))
		//{
		//	if (_isInChange)
		//	{
		//		_selectionChanged = true;
		//	}
		//	else
		//	{
		//		RaiseSelectionChanged();
		//	}
		//}
	}

	private void OnParentTapped(object sender, TappedEventArgs e)
	{
		RaiseInputPaneActivationRequested();
	}

	#endregion
}