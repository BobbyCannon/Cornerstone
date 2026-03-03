#region References

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

#endregion

namespace Avalonia.Diagnostics.Controls;

//TODO: UpdateSourceTrigger & Binding.ValidationRules could help removing the need for this control.
public sealed class CommitTextBox : TextBox
{
	#region Fields

	public static readonly DirectProperty<CommitTextBox, string> CommittedTextProperty;
	private string _committedText;

	#endregion

	#region Constructors

	static CommitTextBox()
	{
		CommittedTextProperty = AvaloniaProperty.RegisterDirect<CommitTextBox, string>(
			nameof(CommittedText), o => o.CommittedText, (o, v) => o.CommittedText = v);
	}

	#endregion

	#region Properties

	public string CommittedText
	{
		get => _committedText;
		set => SetAndRaise(CommittedTextProperty, ref _committedText, value);
	}

	protected override Type StyleKeyOverride => typeof(TextBox);

	#endregion

	#region Methods

	protected override void OnKeyUp(KeyEventArgs e)
	{
		base.OnKeyUp(e);

		switch (e.Key)
		{
			case Key.Enter:

				TryCommit();

				e.Handled = true;

				break;

			case Key.Escape:

				Cancel();

				e.Handled = true;

				break;
		}
	}

	protected override void OnLostFocus(RoutedEventArgs e)
	{
		base.OnLostFocus(e);

		TryCommit();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == CommittedTextProperty)
		{
			Text = CommittedText;
		}
	}

	private void Cancel()
	{
		Text = CommittedText;
		DataValidationErrors.ClearErrors(this);
	}

	private void TryCommit()
	{
		if (!DataValidationErrors.GetHasErrors(this))
		{
			CommittedText = Text;
		}
		else
		{
			Text = CommittedText;
			DataValidationErrors.ClearErrors(this);
		}
	}

	#endregion
}