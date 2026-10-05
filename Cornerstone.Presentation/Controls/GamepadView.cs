using System.ComponentModel;
using Cornerstone.Input;
using Cornerstone.Presentation.Controls.Metadata;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Controls.StyleClasses;

namespace Cornerstone.Presentation.Controls;

/// <summary>
/// Shows live GamepadState: sticks, d-pad, buttons, and triggers.
/// </summary>
[PseudoClasses(":connected")]
public sealed class GamepadView : TemplatedControl
{
	public static readonly StyledProperty<GamepadState> GamepadProperty =
		PresentationProperty.Register<GamepadView, GamepadState>(nameof(Gamepad));

	public GamepadState Gamepad
	{
		get => GetValue(GamepadProperty);
		set => SetValue(GamepadProperty, value);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		if (Gamepad == null)
		{
			Gamepad = new GamepadState();
		}

		RefreshPseudoClasses();
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		if (change.Property == GamepadProperty)
		{
			if (change.OldValue is GamepadState oldValue)
			{
				oldValue.PropertyChanged -= GamepadOnPropertyChanged;
			}

			if (change.NewValue is GamepadState newValue)
			{
				newValue.PropertyChanged += GamepadOnPropertyChanged;
			}

			RefreshPseudoClasses();
		}

		base.OnPropertyChanged(change);
	}

	private void GamepadOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(GamepadState.IsConnected))
		{
			RefreshPseudoClasses();
		}
	}

	private void RefreshPseudoClasses()
	{
		if (!CheckAccess())
		{
			Dispatcher.Post(RefreshPseudoClasses);
			return;
		}

		PseudoClasses.Set(":connected", Gamepad?.IsConnected ?? false);
	}
}
