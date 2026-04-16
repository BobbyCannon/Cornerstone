#region References

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

#endregion

namespace Cornerstone.Avalonia.Extensions;

public static class AvaloniaExtensions
{
	#region Methods

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_inpcChanged")]
	public static extern ref PropertyChangedEventHandler GetPropertyChangedHandler(AvaloniaObject instance);

	/// <summary>
	/// Sets the value of the property only if it hasn't been explicitly set.
	/// </summary>
	public static bool SetDefaultIfNotSet<T>(this AvaloniaObject o, AvaloniaProperty property, T value)
	{
		if (o.IsSet(property))
		{
			return false;
		}

		o.SetValue(property, value);
		return true;
	}

	public static void TryFocusLater(this Control target, int delayMs = 1)
	{
		if (target is null)
		{
			return;
		}

		DispatcherTimer.RunOnce(
			() =>
			{
				target.BringIntoView();
				var ok = target.Focus(NavigationMethod.Tab);
				//Debug.WriteLine($"Delayed focus → {target.Name}  success:{ok}  focused:{target.IsFocused}");
			},
			TimeSpan.FromMilliseconds(delayMs)
		);
	}

	#endregion
}