using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Controls.Layout;
namespace Cornerstone.Presentation.Controls.Platform;

public enum X11NetWmWindowType
{
    Normal,
    Dialog,
    Utility,
    Menu,
    Toolbar,
    Splash,
    Dock,
    Desktop
}

[PrivateApi]
public interface IX11OptionsToplevelImplFeature
{
    void SetNetWmWindowType(X11NetWmWindowType type);
    void SetWmClass(string? className);
}
