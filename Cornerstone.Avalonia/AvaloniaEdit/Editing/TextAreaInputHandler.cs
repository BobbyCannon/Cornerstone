#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using Cornerstone.Avalonia.AvaloniaEdit.Utils;
using Cornerstone.Avalonia.Input;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Editing;

/// <summary>
/// A set of input bindings and event handlers for the text area.
/// </summary>
/// <remarks>
/// <para>
/// There is one active input handler per text area (<see cref="Editing.TextArea.ActiveInputHandler" />), plus
/// a number of active stacked input handlers.
/// </para>
/// <para>
/// The text area also stores a reference to a default input handler, but that is not necessarily active.
/// </para>
/// <para>
/// Stacked input handlers work in addition to the set of currently active handlers (without detaching them).
/// They are detached in the reverse order of being attached.
/// </para>
/// </remarks>
public interface ITextAreaInputHandler
{
	#region Properties

	/// <summary>
	/// Gets the text area that the input handler belongs to.
	/// </summary>
	TextArea TextArea { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Attaches an input handler to the text area.
	/// </summary>
	void Attach();

	/// <summary>
	/// Detaches the input handler from the text area.
	/// </summary>
	void Detach();

	/// <summary>
	/// Remove key from binding
	/// </summary>
	public void RemoveKey(Key key, KeyModifiers modifiers);

	#endregion
}

/// <summary>
/// Stacked input handler.
/// Uses OnEvent-methods instead of registering event handlers to ensure that the events are handled in the correct order.
/// </summary>
public abstract class TextAreaStackedInputHandler : ITextAreaInputHandler
{
	#region Constructors

	/// <summary>
	/// Creates a new TextAreaInputHandler.
	/// </summary>
	protected TextAreaStackedInputHandler(TextArea textArea)
	{
		TextArea = textArea ?? throw new ArgumentNullException(nameof(textArea));
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public TextArea TextArea { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public virtual void Attach()
	{
	}

	/// <inheritdoc />
	public virtual void Detach()
	{
	}

	/// <summary>
	/// Called for the PreviewKeyDown event.
	/// </summary>
	public virtual void OnPreviewKeyDown(KeyEventArgs e)
	{
	}

	/// <summary>
	/// Called for the PreviewKeyUp event.
	/// </summary>
	public virtual void OnPreviewKeyUp(KeyEventArgs e)
	{
	}

	/// <inheritdoc />
	public void RemoveKey(Key key, KeyModifiers modifiers)
	{
	}

	#endregion
}

/// <summary>
/// Default-implementation of <see cref="ITextAreaInputHandler" />.
/// </summary>
/// <remarks>
/// <inheritdoc cref="ITextAreaInputHandler" />
/// </remarks>
public class TextAreaInputHandler : ITextAreaInputHandler
{
	#region Fields

	private readonly List<KeyBinding> _keyBindings;
	private readonly ObserveAddRemoveCollection<ITextAreaInputHandler> _nestedInputHandlers;
	private readonly ObserveAddRemoveCollection<RoutedCommandBinding> _routedCommandBindings;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new TextAreaInputHandler.
	/// </summary>
	public TextAreaInputHandler(TextArea textArea)
	{
		TextArea = textArea ?? throw new ArgumentNullException(nameof(textArea));

		_keyBindings = [];
		_routedCommandBindings = new ObserveAddRemoveCollection<RoutedCommandBinding>(CommandBindingAdded, CommandBindingRemoved);
		_nestedInputHandlers = new ObserveAddRemoveCollection<ITextAreaInputHandler>(NestedInputHandlerAdded, NestedInputHandlerRemoved);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets whether the input handler is currently attached to the text area.
	/// </summary>
	public bool IsAttached { get; private set; }

	/// <summary>
	/// Gets the input bindings of this input handler.
	/// </summary>
	public ICollection<KeyBinding> KeyBindings => _keyBindings;

	/// <summary>
	/// Gets the collection of nested input handlers. NestedInputHandlers are activated and deactivated
	/// together with this input handler.
	/// </summary>
	public ICollection<ITextAreaInputHandler> NestedInputHandlers => _nestedInputHandlers;

	/// <summary>
	/// Gets the command bindings of this input handler.
	/// </summary>
	public ICollection<RoutedCommandBinding> RoutedCommandBindings => _routedCommandBindings;

	/// <inheritdoc />
	public TextArea TextArea { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Adds a command and input binding.
	/// </summary>
	/// <param name="command"> The command ID. </param>
	/// <param name="modifiers"> The modifiers of the keyboard shortcut. </param>
	/// <param name="key"> The key of the keyboard shortcut. </param>
	/// <param name="handler"> The event handler to run when the command is executed. </param>
	public void AddBinding(RoutedCommand command, KeyModifiers modifiers, Key key, EventHandler<ExecutedRoutedEventArgs> handler)
	{
		RoutedCommandBindings.Add(new RoutedCommandBinding(command, handler));
		KeyBindings.Add(new KeyBinding { Command = command, Gesture = new KeyGesture(key, modifiers) });
	}

	/// <inheritdoc />
	public virtual void Attach()
	{
		if (IsAttached)
		{
			throw new InvalidOperationException("Input handler is already attached");
		}

		IsAttached = true;

		TextArea.RoutedCommandBindings.AddRange(_routedCommandBindings);
		TextArea.KeyDown += TextAreaOnKeyDown;

		foreach (var handler in _nestedInputHandlers)
		{
			handler.Attach();
		}
	}

	/// <inheritdoc />
	public virtual void Detach()
	{
		if (!IsAttached)
		{
			throw new InvalidOperationException("Input handler is not attached");
		}

		IsAttached = false;

		foreach (var b in _routedCommandBindings)
		{
			TextArea.RoutedCommandBindings.Remove(b);
		}

		TextArea.KeyDown -= TextAreaOnKeyDown;

		foreach (var handler in _nestedInputHandlers)
		{
			handler.Detach();
		}
	}

	public void RemoveKey(Key key, KeyModifiers modifiers)
	{
		var keyBindingsToRemove = KeyBindings
			.Where(x => (x.Gesture.Key == key) && (x.Gesture.KeyModifiers == modifiers))
			.ToList();

		List<RoutedCommandBinding> routedBindingsToRemove;
		foreach (var keyBinding in keyBindingsToRemove)
		{
			KeyBindings.Remove(keyBinding);

			routedBindingsToRemove = RoutedCommandBindings.Where(x => x.Command == keyBinding.Command).ToList();
			foreach (var binding in routedBindingsToRemove)
			{
				RoutedCommandBindings.Remove(binding);
			}
		}

		routedBindingsToRemove = RoutedCommandBindings.Where(x => (x.Command.Gesture != null) && (x.Command.Gesture.Key == key) && (x.Command.Gesture.KeyModifiers == modifiers)).ToList();
		foreach (var binding in routedBindingsToRemove)
		{
			RoutedCommandBindings.Remove(binding);
		}

		foreach (var handler in _nestedInputHandlers)
		{
			handler.RemoveKey(key, modifiers);
		}
	}

	private void CommandBindingAdded(RoutedCommandBinding commandBinding)
	{
		if (IsAttached)
		{
			TextArea.RoutedCommandBindings.Add(commandBinding);
		}
	}

	private void CommandBindingRemoved(RoutedCommandBinding commandBinding)
	{
		if (IsAttached)
		{
			TextArea.RoutedCommandBindings.Remove(commandBinding);
		}
	}

	private void NestedInputHandlerAdded(ITextAreaInputHandler handler)
	{
		if (handler == null)
		{
			throw new ArgumentNullException(nameof(handler));
		}
		if (handler.TextArea != TextArea)
		{
			throw new ArgumentException("The nested handler must be working for the same text area!");
		}
		if (IsAttached)
		{
			handler.Attach();
		}
	}

	private void NestedInputHandlerRemoved(ITextAreaInputHandler handler)
	{
		if (IsAttached)
		{
			handler.Detach();
		}
	}

	/// <summary>
	/// workaround since InputElement.KeyBindings can't be marked as handled
	/// </summary>
	private void TextAreaOnKeyDown(object sender, KeyEventArgs keyEventArgs)
	{
		foreach (var keyBinding in _keyBindings)
		{
			if (keyEventArgs.Handled)
			{
				break;
			}

			keyBinding.TryHandle(keyEventArgs);
		}

		foreach (var commandBinding in RoutedCommandBindings)
		{
			if (commandBinding.Command.Gesture?.Matches(keyEventArgs) == true)
			{
				commandBinding.Command.Execute(null, (IInputElement) sender);
				keyEventArgs.Handled = true;
				break;
			}
		}
	}

	#endregion
}