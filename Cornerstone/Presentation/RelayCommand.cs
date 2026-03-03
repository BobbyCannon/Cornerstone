#region References

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
	private readonly Action<object> _executeDelegateForStaticMembers;
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
		_executeDelegateForStaticMembers = execute ?? throw new ArgumentNullException(nameof(execute));

		if (execute.Target != null)
		{
			_executeReference = new WeakReference<object>(execute.Target);
			_executeCallback = execute.GetMethodInfo();
			_executeParameter = parameter != null ? new WeakReference<object>(parameter) : null;
		}
		else
		{
			// For static methods, store the MethodInfo directly
			_executeCallback = execute.GetMethodInfo();
			_executeParameter = parameter != null ? new WeakReference<object>(parameter) : null;
		}

		if (canExecute?.Target != null)
		{
			_canExecuteReference = new WeakReference<object>(canExecute.Target);
			_canExecuteCallback = canExecute.GetMethodInfo();
		}
	}

	#endregion

	#region Methods

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

		var result = _canExecuteCallback?.Invoke(action, [parameter]);
		return result is true;
	}

	public void Execute(object parameter)
	{
		if (_executeReference != null)
		{
			// Instance method execution
			if (_executeReference.TryGetTarget(out var action))
			{
				parameter ??= _executeParameter?.TryGetTarget(out var target) == true ? target : null;
				_executeCallback?.Invoke(action, [parameter]);
			}
		}
		else
		{
			// Static method execution
			parameter ??= _executeParameter?.TryGetTarget(out var target) == true ? target : null;
			_executeDelegateForStaticMembers?.Invoke(parameter); // Use the stored delegate for static methods
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