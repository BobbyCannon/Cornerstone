#region References

using Cornerstone.Presentation.Platform;

#endregion

namespace Cornerstone.Presentation.Controls.NativeHosts;

/// <summary>
/// Platform surface behind a <see cref="NativeSurfaceHost" />: handle, visibility, and resize.
/// </summary>
public interface INativeSurface
{
	#region Properties

	/// <summary>
	/// Whether the native surface is currently painting.
	/// </summary>
	bool IsNativeSurfaceVisible { get; }

	IPlatformHandle PlatformHandle { get; }

	#endregion

	#region Methods

	void HandleResize(int width, int height, float scaling);

	/// <summary>
	/// Shows or hides the native surface without destroying the underlying engine when possible.
	/// Presentation is still driven by the host's NativeControlHost visibility (HideWithSize).
	/// </summary>
	void SetNativeSurfaceVisible(bool visible);

	#endregion
}
