#region References

using System;
using System.Windows.Input;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Utilities;

internal class DelegateCommand : ICommand
{
	#region Fields

	private readonly Action _action;
	private readonly Func<object, bool> _canExecute;

	#endregion

	#region Constructors

	public DelegateCommand(Action action, Func<object, bool> canExecute = null)
	{
		_action = action;
		_canExecute = canExecute ?? (_ => true);
	}

	#endregion

	#region Methods

	public bool CanExecute(object parameter)
	{
		return _canExecute(parameter);
	}

	public void Execute(object parameter)
	{
		_action();
	}

	#endregion

	#region Events

	public event EventHandler CanExecuteChanged
	{
		add { }
		remove { }
	}

	#endregion
}