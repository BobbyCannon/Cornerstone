using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.Platforms.Windows;

internal class Win32NativeToManagedMenuExporter : INativeMenuExporter
{
    private NativeMenu? _nativeMenu;

    public void SetNativeMenu(NativeMenu? nativeMenu)
    {
        _nativeMenu = nativeMenu;
    }

    internal NativeMenu? GetNativeMenu() => _nativeMenu;
}
