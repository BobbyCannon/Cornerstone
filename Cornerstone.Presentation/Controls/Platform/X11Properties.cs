using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.Controls.Platform;

/// <summary>
/// Set of X11 specific properties and events that allow deeper customization of the application per platform.
/// </summary>
public class X11Properties
{
    public static readonly AttachedProperty<X11NetWmWindowType> NetWmWindowTypeProperty =
        PresentationProperty.RegisterAttached<X11Properties, Window, X11NetWmWindowType>("NetWmWindowType");

    public static void SetNetWmWindowType(Window obj, X11NetWmWindowType value) => obj.SetValue(NetWmWindowTypeProperty, value);
    public static X11NetWmWindowType GetNetWmWindowType(Window obj) => obj.GetValue(NetWmWindowTypeProperty);

    public static readonly AttachedProperty<string?> WmClassProperty =
        PresentationProperty.RegisterAttached<X11Properties, Window, string?>("WmClass");

    public static void SetWmClass(Window obj, string? value) => obj.SetValue(WmClassProperty, value);
    public static string? GetWmClass(Window obj) => obj.GetValue(WmClassProperty);

    static X11Properties()
    {
        NetWmWindowTypeProperty.Changed.Subscribe(OnNetWmWindowTypeChanged);
        WmClassProperty.Changed.Subscribe(OnWmClassChanged);
    }

    private static IX11OptionsToplevelImplFeature? TryGetFeature(PresentationPropertyChangedEventArgs e)
        => (e.Sender as TopLevel)?.PlatformImpl?.TryGetFeature<IX11OptionsToplevelImplFeature>();
    
    private static void OnWmClassChanged(PresentationPropertyChangedEventArgs<string?> e) => 
        TryGetFeature(e)?.SetWmClass(e.NewValue.GetValueOrDefault(null));

    private static void OnNetWmWindowTypeChanged(PresentationPropertyChangedEventArgs<X11NetWmWindowType> e) =>
        TryGetFeature(e)?.SetNetWmWindowType(e.NewValue.GetValueOrDefault());
}