#region References

using System;
using System.Windows.Input;
using Avalonia.Interactivity;

#endregion

namespace Cornerstone.Avalonia.Input;

public sealed class ExecutedRoutedEventArgs : RoutedEventArgs
{
	#region Constructors

	internal ExecutedRoutedEventArgs(ICommand command, object parameter)
	{
		Command = command ?? throw new ArgumentNullException(nameof(command));
		Parameter = parameter;
		RoutedEvent = RoutedCommand.ExecutedEvent;
	}

	#endregion

	#region Properties

	public ICommand Command { get; }

	public object Parameter { get; }

	#endregion
}