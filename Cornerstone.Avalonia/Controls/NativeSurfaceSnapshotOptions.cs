namespace Cornerstone.Avalonia.Controls;

/// <summary>
/// Options for capturing a native surface placeholder snapshot used during pause/overlay mode.
/// </summary>
public class NativeSurfaceSnapshotOptions
{
	#region Properties

	/// <summary>
	/// Maximum height in pixels after scaling. 0 means no cap. Default 0.
	/// </summary>
	public int MaxHeight { get; set; }

	/// <summary>
	/// Maximum width in pixels after scaling. 0 means no cap. Default 0.
	/// </summary>
	public int MaxWidth { get; set; }

	/// <summary>
	/// Capture scale factor (0–1]. 1.0 is full resolution; lower values soften when stretched.
	/// </summary>
	public double Scale { get; set; } = 1.0;

	#endregion

	#region Methods

	public static NativeSurfaceSnapshotOptions Default()
	{
		return new NativeSurfaceSnapshotOptions();
	}

	#endregion
}