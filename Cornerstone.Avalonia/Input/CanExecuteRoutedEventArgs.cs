#region References

using System;
using System.Windows.Input;
using Avalonia.Interactivity;

#endregion

namespace Cornerstone.Avalonia.Input;

public sealed class CanExecuteRoutedEventArgs : RoutedEventArgs
{
	#region Constructors

	internal CanExecuteRoutedEventArgs(ICommand command, object parameter)
	{
		Command = command ?? throw new ArgumentNullException(nameof(command));
		Parameter = parameter;
		RoutedEvent = RoutedCommand.CanExecuteEvent;
	}

	#endregion

	#region Properties

	public bool CanExecute { get; set; }

	public ICommand Command { get; }

	public object Parameter { get; }

	#endregion
}