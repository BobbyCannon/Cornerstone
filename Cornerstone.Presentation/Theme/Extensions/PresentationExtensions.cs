#region References

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Threading;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Theme.Extensions;

public static class PresentationExtensions
{
	#region Methods

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_inpcChanged")]
	public static extern ref PropertyChangedEventHandler GetPropertyChangedHandler(PresentationObject instance);

	/// <summary>
	/// Sets the value of the property only if it hasn't been explicitly set.
	/// </summary>
	public static bool SetDefaultIfNotSet<T>(this PresentationObject o, PresentationProperty property, T value)
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