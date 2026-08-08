#region References

using System;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.Extensions;

public static class ControlExtensions
{
	#region Methods

	/// <summary>
	/// Resolves the platform child handle (HWND / native view pointer) for a
	/// <see cref="NativeControlHost" /> after Avalonia has created its internal child.
	/// </summary>
	public static async Task<IntPtr> GetHwndAsync(this NativeControlHost nativeControlHost)
	{
		if (nativeControlHost == null)
		{
			return IntPtr.Zero;
		}

		var hostType = typeof(NativeControlHost);
		var nativeHandleField = hostType.GetField("_nativeControlHandle", BindingFlags.NonPublic | BindingFlags.Instance);
		if (nativeHandleField == null)
		{
			throw new Exception("Could not find _nativeControlHandle field.");
		}

		object dumbWindow = null;
		for (var i = 0; i < 10; i++)
		{
			dumbWindow = nativeHandleField.GetValue(nativeControlHost);
			if (dumbWindow != null)
			{
				break;
			}
			await Task.Delay(100);
		}

		if (dumbWindow == null)
		{
			return IntPtr.Zero;
		}

		var dumbWindowType = dumbWindow.GetType();
		var handleProp = dumbWindowType.GetProperty("Handle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		if (handleProp == null)
		{
			throw new Exception("Could not find Handle property in DumbWindow.");
		}

		var hwnd = (IntPtr) handleProp.GetValue(dumbWindow)!;
		return hwnd;
	}

	#endregion
}