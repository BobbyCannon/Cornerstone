#region References

using System;
using System.Windows.Input;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Utils;

internal class TestCommand : ICommand
{
	#region Fields

	private readonly Func<object, bool> _canExecute;
	private EventHandler _canExecuteChanged;
	private bool _enabled = true;
	private readonly Action<object> _execute;

	#endregion

	#region Constructors

	public TestCommand(bool enabled = true)
	{
		_enabled = enabled;
		_canExecute = _ => _enabled;
		_execute = _ => { };
	}

	public TestCommand(Func<object, bool> canExecute, Action<object> execute = null)
	{
		_canExecute = canExecute;
		_execute = execute ?? (_ => { });
	}

	#endregion

	#region Properties

	public bool IsEnabled
	{
		get => _enabled;
		set
		{
			if (_enabled != value)
			{
				_enabled = value;
				_canExecuteChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	public int SubscriptionCount { get; private set; }

	#endregion

	#region Methods

	public bool CanExecute(object parameter)
	{
		return _canExecute(parameter);
	}

	public void Execute(object parameter)
	{
		_execute(parameter);
	}

	public void RaiseCanExecuteChanged()
	{
		_canExecuteChanged?.Invoke(this, EventArgs.Empty);
	}

	#endregion

	#region Events

	public event EventHandler CanExecuteChanged
	{
		add
		{
			_canExecuteChanged += value;
			++SubscriptionCount;
		}
		remove
		{
			_canExecuteChanged -= value;
			--SubscriptionCount;
		}
	}

	#endregion
}