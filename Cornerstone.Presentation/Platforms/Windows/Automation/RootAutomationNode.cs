using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Automation.Provider;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platforms.Windows.Automation.Interop;

namespace Cornerstone.Presentation.Platforms.Windows.Automation
{
    [GeneratedComClass]
    internal partial class RootAutomationNode : AutomationNode, IRawElementProviderFragmentRoot
    {
        public RootAutomationNode(AutomationPeer peer)
            : base(peer)
        {
            Peer = base.Peer.GetProvider<IRootProvider>() ?? throw new PresentationInternalException(
                "Attempt to create RootAutomationNode from peer which does not implement IRootProvider.");
            Peer.FocusChanged += OnRootFocusChanged;
        }

        public override IRawElementProviderFragmentRoot? GetFragmentRoot() => this;
        public new IRootProvider Peer { get; }
        public IWindowBaseImpl? WindowImpl => Peer.PlatformImpl as IWindowBaseImpl;

        public IRawElementProviderFragment? ElementProviderFromPoint(double x, double y)
        {
            if (WindowImpl is null)
                return null;

            var p = WindowImpl.PointToClient(new PixelPoint((int)x, (int)y));
            var found = InvokeSync(() => Peer.GetPeerFromPoint(p));
            var result = GetOrCreate(found) as IRawElementProviderFragment;
            return result;
        }

        public IRawElementProviderFragment? GetFocus()
        {
            var focus = InvokeSync(() => Peer.GetFocus());
            return GetOrCreate(focus);
        }

        public override IRawElementProviderSimple GetHostRawElementProvider()
        {
            var handle = WindowImpl?.Handle?.Handle ?? IntPtr.Zero;
            if (handle == IntPtr.Zero)
                return null;
            var hr = UiaCoreProviderApi.UiaHostProviderFromHwnd(handle, out var result);
            Marshal.ThrowExceptionForHR(hr);
            return result;
        }

        private void OnRootFocusChanged(object? sender, EventArgs e)
        {
            RaiseFocusChanged(GetOrCreate(Peer.GetFocus()));
        }
    }
}
