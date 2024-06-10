#region References

using Avalonia.Input;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.CodeCompletion;

public struct CompletionTrigger
{
	#region Constructors

	public CompletionTrigger(Key key, KeyModifiers modifiers, bool silent)
	{
		Key = key;
		Modifiers = modifiers;
		Silent = silent;
	}

	#endregion

	#region Properties

	public Key Key { get; set; }

	public KeyModifiers Modifiers { get; set; }

	public bool Silent { get; set; }

	#endregion
}