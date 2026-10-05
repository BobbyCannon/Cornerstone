namespace Cornerstone.Presentation.Wayland.Server;

/// <summary>
/// Priority for cross-thread dispatch between UI and Wayland threads.
/// </summary>
internal enum WaylandDispatchPriority
{
    /// <summary>
    /// UI→worker: batched with the next compositor commit.
    /// Worker→UI: posted at default dispatcher priority.
    /// </summary>
    Normal,

    /// <summary>
    /// UI→worker: out-of-band, processed immediately by the worker.
    /// Worker→UI: posted at Send dispatcher priority (highest).
    /// </summary>
    Oob
}
