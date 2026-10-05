namespace Cornerstone.Presentation.Input.Platform;

/// <summary>
/// Simple <see cref="IPlatformClipboardManagerImpl"/> implementation holding pre-created clipboard instances.
/// </summary>
internal sealed class PlatformClipboardManager : IPlatformClipboardManagerImpl
{
    private readonly IClipboard _clipboard;
    private readonly IClipboard _primarySelection;

    public PlatformClipboardManager(IClipboard clipboard, IClipboard primarySelection)
    {
        _clipboard = clipboard;
        _primarySelection = primarySelection;
    }

    public IClipboard TryGetClipboard(ClipboardType type)
    {
        switch (type)
        {
            case ClipboardType.Default:
                return _clipboard;
            case ClipboardType.PrimarySelection:
                return _primarySelection;
            default:
                return null;
        }
    }
}
