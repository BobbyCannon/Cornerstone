﻿#region References

using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// A command whose sole purpose is to relay its functionality to other objects by invoking delegates. The default return value for the CanExecute method is 'true'.
/// </summary>
public class RelayCommand : ICommand
{
	#region Fields

	private readonly MethodInfo _canExecuteCallback;
	private readonly WeakReference<object> _canExecuteReference;
	private readonly MethodInfo _executeCallback;
	private readonly WeakReference<object> _executeParameter;
	private readonly WeakReference<object> _executeReference;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new command.
	/// </summary>
	/// <param name="execute"> The execution logic. </param>
	/// <param name="canExecute"> The execution status logic. </param>
	public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
		: this(execute, null, canExecute)
	{
	}

	/// <summary>
	/// Creates a new command.
	/// </summary>
	/// <param name="execute"> The execution logic. </param>
	/// <param name="parameter"> An optional parameter. </param>
	/// <param name="canExecute"> The execution status logic. </param>
	public RelayCommand(Action<object> execute, object parameter, Func<object, bool> canExecute = null)
	{
		_executeReference = new WeakReference<object>(execute.Target);
		_executeCallback = execute.GetMethodInfo();
		_executeParameter = new WeakReference<object>(parameter);

		if (canExecute != null)
		{
			_canExecuteReference = new WeakReference<object>(canExecute.Target);
			_canExecuteCallback = canExecute.GetMethodInfo();
		}
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	[DebuggerStepThrough]
	public bool CanExecute(object parameter)
	{
		if (_canExecuteReference == null)
		{
			return true;
		}

		if (!_canExecuteReference.TryGetTarget(out var action))
		{
			return false;
		}

		var result = _canExecuteCallback.Invoke(action, [parameter]);
		return result is true;
	}

	/// <inheritdoc />
	public void Execute(object parameter)
	{
		if (_executeReference.TryGetTarget(out var action))
		{
			parameter ??= _executeParameter.TryGetTarget(out var target) ? target : null;
			_executeCallback.Invoke(action, [parameter]);
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

	/// <inheritdoc />
	public event EventHandler CanExecuteChanged;

	#endregion
}