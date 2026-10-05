using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Cornerstone.Presentation.Controls.Automation.Peers;
using Cornerstone.Presentation.Platforms.Windows.Automation.Interop;

namespace Cornerstone.Presentation.Platforms.Windows.Automation;

/// <summary>
/// An automation node which serves as the root of an embedded native control automation tree.
/// </summary>
    [GeneratedComClass]
internal partial class InteropAutomationNode : AutomationNode, IRawElementProviderFragmentRoot
{
    private readonly IntPtr _handle;

    public InteropAutomationNode(InteropAutomationPeer peer)
        : base(peer)
    {
        _handle = peer.NativeControlHandle.Handle;
    }

    public override Rect GetBoundingRectangle() => default;
    public override IRawElementProviderFragmentRoot? GetFragmentRoot() => null;
    public override void GetProviderOptions(out ProviderOptions providerOptions)
    {
        providerOptions = ProviderOptions.ServerSideProvider | ProviderOptions.OverrideProvider;
    }

    public override object GetPatternProvider(int patternId) => null;
    public override void GetPropertyValue(int propertyId, nint variant)
    {
        if (variant != 0)
        {
            Marshal.StructureToPtr(default(Cornerstone.Presentation.Platforms.Windows.Automation.Marshalling.ComVariant), variant, false);
        }
    }

    public override IRawElementProviderSimple? GetHostRawElementProvider()
    {
        var hr = UiaCoreProviderApi.UiaHostProviderFromHwnd(_handle, out var result);
        Marshal.ThrowExceptionForHR(hr);
        return result;
    }

    public override IRawElementProviderFragment? Navigate(NavigateDirection direction)
    {
        return direction == NavigateDirection.Parent ? base.Navigate(direction) : null;
    }

    public IRawElementProviderFragment? ElementProviderFromPoint(double x, double y) => null;
    public IRawElementProviderFragment? GetFocus() => null;
    public IRawElementProviderSimple[]? GetEmbeddedFragmentRoots() => null;
}
