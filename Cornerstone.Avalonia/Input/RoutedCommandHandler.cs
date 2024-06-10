#region References

using System;
using Avalonia.Input;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia.Input;

public class RoutedCommandHandler : IRoutedCommandHandler
{
	#region Constructors

	public RoutedCommandHandler()
	{
		KeyBindings = [];
		RoutedCommandBindings = [];
	}

	#endregion

	#region Properties

	public SpeedyList<KeyBinding> KeyBindings { get; }

	/// <inheritdoc />
	public SpeedyList<RoutedCommandBinding> RoutedCommandBindings { get; }

	#endregion

	#region Methods

	public void AddBinding(RoutedCommand command, KeyModifiers modifiers, Key key, EventHandler<ExecutedRoutedEventArgs> handler, EventHandler<CanExecuteRoutedEventArgs> canExecuteHandler = null)
	{
		RoutedCommandBindings.Add(new RoutedCommandBinding(command, handler, canExecuteHandler));
		KeyBindings.Add(command.CreateKeyBinding(modifiers, key));
	}

	public void Attach(IInputElement element)
	{
		element.KeyBindings.AddRange(KeyBindings);
	}

	#endregion
}

public interface IRoutedCommandHandler
{
	#region Properties

	SpeedyList<RoutedCommandBinding> RoutedCommandBindings { get; }

	#endregion
}