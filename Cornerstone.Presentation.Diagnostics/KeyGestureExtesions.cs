#region References

using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;

#endregion

namespace Cornerstone.Presentation.Diagnostics;

public static class KeyGestureExtensions
{
	#region Methods

	public static bool Matches(this KeyGesture gesture, RawKeyEventArgs keyEvent)
	{
		return ((KeyModifiers) (keyEvent.Modifiers & RawInputModifiers.KeyboardMask) == gesture.KeyModifiers) 
			&& (ResolveNumPadOperationKey(keyEvent.Key) == ResolveNumPadOperationKey(gesture.Key));
	}

	private static Key ResolveNumPadOperationKey(Key key)
	{
		return key switch
		{
			Key.Add => Key.OemPlus,
			Key.Subtract => Key.OemMinus,
			Key.Decimal => Key.OemPeriod,
			_ => key
		};
	}

	#endregion
}