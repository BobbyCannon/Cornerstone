#region References

using System;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.NativeHosts;

#endregion

namespace Cornerstone.Presentation.Theme.Extensions;

public static class ControlExtensions
{
	#region Methods

	/// <summary>
	/// Resolves the platform child handle (HWND / native view pointer) for a
	/// <see cref="NativeControlHost" /> after Cornerstone has created its internal child.
	/// </summary>
	public static Task<IntPtr> GetHwndAsync(this NativeControlHost nativeControlHost)
	{
		return NativeControlHostExtensions.GetHwndAsync(nativeControlHost);
	}

	#endregion
}