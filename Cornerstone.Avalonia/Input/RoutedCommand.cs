#region References

using System;
using System.Linq;
using System.Windows.Input;
using Avalonia.Input;
using Avalonia.Interactivity;
using KeyGesture = Avalonia.Input.KeyGesture;

#endregion

namespace Cornerstone.Avalonia.Input;

public class RoutedCommand : ICommand
{
	#region Fields

	private static IInputElement _inputElement;

	#endregion

	#region Constructors

	public RoutedCommand(string name, KeyGesture keyGesture = null)
	{
		Name = name;
		Gesture = keyGesture;
	}

	static RoutedCommand()
	{
		CanExecuteEvent = RoutedEvent.Register<CanExecuteRoutedEventArgs>(nameof(CanExecuteEvent), RoutingStrategies.Bubble, typeof(RoutedCommand));
		CanExecuteEvent.AddClassHandler<Interactive>(CanExecuteEventHandler);
		ExecutedEvent = RoutedEvent.Register<ExecutedRoutedEventArgs>(nameof(ExecutedEvent), RoutingStrategies.Bubble, typeof(RoutedCommand));
		ExecutedEvent.AddClassHandler<Interactive>(ExecutedEventHandler);

		InputElement.GotFocusEvent.AddClassHandler<Interactive>(GotFocusEventHandler);
	}

	#endregion

	#region Properties

	public static RoutedEvent<CanExecuteRoutedEventArgs> CanExecuteEvent { get; }

	public static RoutedEvent<ExecutedRoutedEventArgs> ExecutedEvent { get; }

	public KeyGesture Gesture { get; }

	public string Name { get; }

	#endregion

	#region Methods

	public bool CanExecute(object parameter, IInputElement target)
	{
		if (target == null)
		{
			return false;
		}

		var args = new CanExecuteRoutedEventArgs(this, parameter);
		target.RaiseEvent(args);

		return args.CanExecute;
	}

	public void Execute(object parameter, IInputElement target)
	{
		if (target == null)
		{
			return;
		}

		var args = new ExecutedRoutedEventArgs(this, parameter);
		target.RaiseEvent(args);
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

	bool ICommand.CanExecute(object parameter)
	{
		return CanExecute(parameter, _inputElement);
	}

	private static void CanExecuteEventHandler(Interactive control, CanExecuteRoutedEventArgs args)
	{
		if (control is IRoutedCommandHandler bindable)
		{
			var binding = bindable
				.RoutedCommandBindings
				.Where(c => c != null)
				.FirstOrDefault(c =>
					(c.Command == args.Command)
					&& c.DoCanExecute(control, args)
				);

			args.CanExecute = binding != null;
		}
	}

	void ICommand.Execute(object parameter)
	{
		Execute(parameter, _inputElement);
	}

	private static void ExecutedEventHandler(Interactive control, ExecutedRoutedEventArgs args)
	{
		if (control is IRoutedCommandHandler bindable)
		{
			// ReSharper disable once UnusedVariable
			var binding = bindable
				.RoutedCommandBindings
				.Where(c => c != null)
				.FirstOrDefault(c => c.Command == args.Command);

			binding?.DoExecuted(control, args);
		}
	}

	private static void GotFocusEventHandler(Interactive control, GotFocusEventArgs args)
	{
		_inputElement = args.Source as IInputElement;
	}

	#endregion

	#region Events

	/// <inheritdoc />
	public event EventHandler CanExecuteChanged;

	#endregion
}