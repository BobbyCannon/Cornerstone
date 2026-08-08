#region References

using System.Threading.Tasks;
using Avalonia.Platform;

#endregion

namespace Cornerstone.Avalonia.Controls;

/// <summary>
/// Platform surface behind a <see cref="PausableNativeHost" />: handle, snapshot, visibility, resize.
/// </summary>
public interface IPausableNativeSurface
{
	#region Properties

	/// <summary>
	/// Whether the native surface is currently painting.
	/// When false, Avalonia content in the same slot can appear on top.
	/// </summary>
	bool IsNativeSurfaceVisible { get; }

	IPlatformHandle PlatformHandle { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Captures the currently visible native surface as a PNG for placeholder underlay mode.
	/// </summary>
	Task<NativeSurfaceSnapshot> CaptureSnapshotAsync(NativeSurfaceSnapshotOptions options = null);

	void HandleResize(int width, int height, float scaling);

	/// <summary>
	/// Shows or hides the native surface without destroying the underlying engine when possible.
	/// Presentation is still driven by the host's NativeControlHost visibility (HideWithSize).
	/// </summary>
	void SetNativeSurfaceVisible(bool visible);

	#endregion
}