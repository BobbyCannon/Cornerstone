#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using KeyModifiers = Avalonia.Input.KeyModifiers;
using Key = Avalonia.Input.Key;

#endregion

namespace Cornerstone.Avalonia.Controls;

/// <summary>
/// Manages single-key shortcuts and two-key chord shortcuts for an Avalonia <see cref="Window" />.
/// <list type="bullet">
/// <item> Single-key bindings execute immediately. </item>
/// <item> Chord bindings wait for the second key (with configurable timeout). </item>
/// <item> Chord prefixes take precedence over any single-key binding on the same first key. </item>
/// <item> Multiple chords can share the same prefix (different second keys are supported). </item>
/// <item> If the second key of a chord doesn't match, the key is immediately re-evaluated as a possible single-key or new chord prefix. </item>
/// </list>
/// </summary>
public class ShortcutBindingManager : IDisposable
{
	#region Fields

	private readonly IReadOnlyList<ShortcutBinding> _bindings;
	private (Key Key, KeyModifiers Modifiers)? _pendingPrefix;
	private readonly DispatcherTimer _timeoutTimer;
	private readonly TopLevel _topLevel;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="ShortcutBindingManager" /> class.
	/// </summary>
	/// <param name="bindings"> The collection of shortcut bindings. </param>
	/// <param name="timeout"> Optional timeout for chord completion (defaults to 1.5 seconds). </param>
	/// <exception cref="ArgumentException"> Thrown if <paramref name="bindings" /> is empty. </exception>
	public ShortcutBindingManager(IEnumerable<ShortcutBinding> bindings, TimeSpan? timeout = null)
	{
		_topLevel = CornerstoneApplication.GetTopLevel();
		if (_topLevel != null)
		{
			_topLevel.KeyDown += OnKeyDown;
		}

		var bindingList = bindings?.ToList()
			?? throw new ArgumentNullException(nameof(bindings));

		if (bindingList.Count == 0)
		{
			throw new ArgumentException("At least one binding is required.", nameof(bindings));
		}

		_bindings = bindingList.AsReadOnly();
		_timeoutTimer = new DispatcherTimer { Interval = timeout ?? TimeSpan.FromMilliseconds(1500) };
		_timeoutTimer.Tick += OnTimeout;
	}

	#endregion

	#region Methods

	public void Dispose()
	{
		if (_topLevel != null)
		{
			_topLevel.KeyDown -= OnKeyDown;
		}

		_timeoutTimer.Stop();
		_timeoutTimer.Tick -= OnTimeout;
	}

	protected virtual void OnExecuted(ShortcutBinding binding)
	{
		Executed?.Invoke(this, binding);
	}

	private void HandleFirstKey(KeyEventArgs e)
	{
		var key = e.Key;
		var mods = e.KeyModifiers;

		// Chord prefixes take precedence
		var binding = _bindings.FirstOrDefault(b => (b.Key == key) && (b.KeyModifiers == mods));
		if (binding is { HasKeyChord: true })
		{
			// Wait for the second binding
			_pendingPrefix = (key, mods);
			_timeoutTimer.Start();
			e.Handled = true;
			return;
		}

		if (binding != null)
		{
			// Process the single binding
			OnExecuted(binding);
			e.Handled = true;
		}
	}

	private void HandleSecondKey(KeyEventArgs e)
	{
		var key = e.Key;
		var mods = e.KeyModifiers;

		// Look for a chord that exactly matches the pending prefix + this second key
		var binding = _bindings.FirstOrDefault(b =>
			b.HasKeyChord
			&& (b.Key == _pendingPrefix!.Value.Key)
			&& (b.KeyModifiers == _pendingPrefix.Value.Modifiers)
			&& (b.SecondKey == key)
			&& (b.SecondKeyModifiers == mods)
		);

		if (binding != null)
		{
			OnExecuted(binding);
			Reset();
			e.Handled = true;
			return;
		}

		Reset();
		HandleFirstKey(e);
	}

	private void OnKeyDown(object sender, KeyEventArgs e)
	{
		ProcessKey(e);
	}

	private void OnTimeout(object sender, EventArgs e)
	{
		Reset();
	}

	private void ProcessKey(KeyEventArgs e)
	{
		if (_pendingPrefix == null)
		{
			HandleFirstKey(e);
		}
		else
		{
			HandleSecondKey(e);
		}
	}

	private void Reset()
	{
		_pendingPrefix = null;
		_timeoutTimer.Stop();
	}

	#endregion

	#region Events

	public event EventHandler<ShortcutBinding> Executed;

	#endregion
}