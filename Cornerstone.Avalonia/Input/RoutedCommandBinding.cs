#region References

using System;
using System.Windows.Input;

#endregion

namespace Cornerstone.Avalonia.Input;

public class RoutedCommandBinding
{
	#region Constructors

	public RoutedCommandBinding(RoutedCommand command,
		EventHandler<ExecutedRoutedEventArgs> executed = null,
		EventHandler<CanExecuteRoutedEventArgs> canExecute = null)
	{
		Command = command;

		if (executed != null)
		{
			Executed += executed;
		}

		if (canExecute != null)
		{
			CanExecute += canExecute;
		}
	}

	#endregion

	#region Properties

	public RoutedCommand Command { get; }

	#endregion

	#region Methods

	public void Refresh()
	{
		CanExecute?.Invoke(this, new CanExecuteRoutedEventArgs(Command, null));
	}

	internal bool DoCanExecute(object sender, CanExecuteRoutedEventArgs e)
	{
		if (e.Handled)
		{
			return true;
		}

		var canExecute = CanExecute;
		if (canExecute == null)
		{
			if (Executed != null)
			{
				e.Handled = true;
				e.CanExecute = true;
			}
		}
		else
		{
			canExecute(sender, e);

			if (e.CanExecute)
			{
				e.Handled = true;
			}
		}

		return e.CanExecute;
	}

	internal bool DoExecuted(object sender, ExecutedRoutedEventArgs e)
	{
		if (e.Handled)
		{
			return false;
		}

		var executed = Executed;
		if (executed == null)
		{
			return false;
		}

		if (!DoCanExecute(sender, new CanExecuteRoutedEventArgs(e.Command, e.Parameter)))
		{
			return false;
		}

		executed(sender, e);
		e.Handled = true;
		return true;

	}

	#endregion

	#region Events

	public event EventHandler<CanExecuteRoutedEventArgs> CanExecute;

	public event EventHandler<ExecutedRoutedEventArgs> Executed;

	#endregion
}