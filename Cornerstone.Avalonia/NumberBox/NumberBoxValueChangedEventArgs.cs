#region References

using System;

#endregion

namespace Cornerstone.Avalonia.NumberBox;

/// <summary>
/// Provides event data for the NumberBox.ValueChanged event.
/// </summary>
public class NumberBoxValueChangedEventArgs : EventArgs
{
	#region Constructors

	internal NumberBoxValueChangedEventArgs(double oldV, double newV)
	{
		OldValue = oldV;
		NewValue = newV;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Contains the new Value to be set for a NumberBox.
	/// </summary>
	public double NewValue { get; }

	/// <summary>
	/// Contains the old Value being replaced in a NumberBox.
	/// </summary>
	public double OldValue { get; }

	#endregion
}