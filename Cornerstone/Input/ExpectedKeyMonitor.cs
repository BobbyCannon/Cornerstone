#region References

using Cornerstone.Threading;

#endregion

namespace Cornerstone.Input;

public class ExpectedKeyMonitor : Lockable
{
	#region Fields

	private int _index;

	#endregion

	#region Constructors

	public ExpectedKeyMonitor(params ExpectedKeyState[] keys)
	{
		Keys = keys;
	}

	#endregion

	#region Properties

	public ExpectedKeyState[] Keys { get; }

	#endregion

	#region Methods

	public bool Process(KeyboardState state)
	{
		try
		{
			EnterWriteLock();

			var key = Keys[_index];
			var matched = Match(key, state);

			if (matched)
			{
				_index++;
			}

			return matched && (_index >= Keys.Length);
		}
		finally
		{
			if (_index >= Keys.Length)
			{
				_index = 0;
			}

			ExitWriteLock();
		}
	}

	public void Reset()
	{
		try
		{
			EnterWriteLock();

			_index = 0;
		}
		finally
		{
			ExitWriteLock();
		}
	}

	private bool Match(ExpectedKeyState expected, KeyboardState actual)
	{
		if (expected.TriggerOnPress != actual.IsPressed)
		{
			return false;
		}

		if (expected.Key != actual.Key)
		{
			return false;
		}

		return MatchShift(expected, actual)
			&& MatchControl(expected, actual)
			&& MatchAlt(expected, actual);
	}

	private bool MatchAlt(ExpectedKeyState expected, KeyboardState actual)
	{
		// Check to see if either Alt is acceptable
		if (expected.IsAltRequired)
		{
			return actual.IsAltPressed
				|| actual.IsLeftAltPressed
				|| actual.IsRightAltPressed;
		}

		// If either Alt is not accept then ensure specific Alt is accurate
		return (expected.IsLeftAltRequired == actual.IsLeftAltPressed)
			&& (expected.IsRightAltRequired == actual.IsRightAltPressed);
	}

	private bool MatchControl(ExpectedKeyState expected, KeyboardState actual)
	{
		// Check to see if either Control is acceptable
		if (expected.IsControlRequired)
		{
			return actual.IsControlPressed
				|| actual.IsLeftControlPressed
				|| actual.IsRightControlPressed;
		}

		// If either Control is not accept then ensure specific Control is accurate
		return (expected.IsLeftControlRequired == actual.IsLeftControlPressed)
			&& (expected.IsRightControlRequired == actual.IsRightControlPressed);
	}

	private bool MatchShift(ExpectedKeyState expected, KeyboardState actual)
	{
		// Check to see if either Shift is acceptable
		if (expected.IsShiftRequired)
		{
			return actual.IsShiftPressed
				|| actual.IsLeftShiftPressed
				|| actual.IsRightShiftPressed;
		}

		// If either Shift is not accept then ensure specific Shift is accurate
		return (expected.IsLeftShiftRequired == actual.IsLeftShiftPressed)
			&& (expected.IsRightShiftRequired == actual.IsRightShiftPressed);
	}

	#endregion
}