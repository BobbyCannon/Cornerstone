namespace Cornerstone.Avalonia.Controls;

/// <summary>
/// Control that can freeze its native surface (snapshot underlay + hide host) for Avalonia airspace.
/// Implemented by <see cref="PausableNativeHost" /> and product shells that forward to one.
/// </summary>
public interface INativeHostPausable
{
	#region Properties

	/// <summary>
	/// When true, freeze the native surface so Avalonia can paint over this region.
	/// </summary>
	bool IsPaused { get; set; }

	#endregion
}