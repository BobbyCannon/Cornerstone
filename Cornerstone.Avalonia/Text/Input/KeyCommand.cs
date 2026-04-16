#region References

using System;
using System.Windows.Input;
using Avalonia.Input;

#endregion

namespace Cornerstone.Avalonia.Text.Input;

/// <summary>
/// A command whose sole purpose is to relay its functionality to other objects by invoking delegates. The default return value for the CanExecute method is 'true'.
/// </summary>
public class KeyCommand : ICommand
{
	#region Fields

	private readonly Func<KeyEventArgs, bool> _canExecute;
	private readonly Action<KeyEventArgs> _execute;
	private readonly bool _willHandle;

	#endregion

	#region Constructors

	public KeyCommand(Action<KeyEventArgs> execute, Func<KeyEventArgs, bool> canExecute = null, bool willHandle = true)
	{
		_execute = execute;
		_canExecute = canExecute;
		_willHandle = willHandle;
	}

	#endregion

	#region Methods

	public bool CanExecute(object parameter)
	{
		return _canExecute?.Invoke(parameter as KeyEventArgs) ?? true;
	}

	public void Execute(object parameter)
	{
		var args = parameter as KeyEventArgs;
		_execute?.Invoke(args);

		if (args != null)
		{
			args.Handled |= _willHandle;
		}
	}

	/// <summary>
	/// Refresh the command state.
	/// </summary>
	public void Refresh()
	{
		OnCanExecuteChanged();
	}

	/// <summary>
	/// Overridable can execute event.
	/// </summary>
	protected virtual void OnCanExecuteChanged()
	{
		CanExecuteChanged?.Invoke(this, EventArgs.Empty);
	}

	#endregion

	#region Events

	public event EventHandler CanExecuteChanged;

	#endregion
}