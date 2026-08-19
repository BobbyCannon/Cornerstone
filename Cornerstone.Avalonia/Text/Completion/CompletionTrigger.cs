#region References

using Avalonia.Input;

#endregion

namespace Cornerstone.Avalonia.Text.Completion;

/// <summary>
/// A key that should start a completion session.
/// Silent triggers (Tab, Ctrl+Space) do not insert the key into the document.
/// </summary>
public readonly struct CompletionTrigger
{
	#region Constructors

	public CompletionTrigger(Key key, KeyModifiers modifiers = KeyModifiers.None, bool silent = false)
	{
		Key = key;
		Modifiers = modifiers;
		Silent = silent;
	}

	#endregion

	#region Properties

	public Key Key { get; }

	public KeyModifiers Modifiers { get; }

	public bool Silent { get; }

	#endregion

	#region Methods

	public bool Matches(Key key, KeyModifiers modifiers)
	{
		return (Key == key) && (Modifiers == modifiers);
	}

	#endregion
}
