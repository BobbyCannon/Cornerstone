using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Automation.Provider;
using UIA = Cornerstone.Presentation.Platforms.Windows.Automation.Interop;

namespace Cornerstone.Presentation.Platforms.Windows.Automation
{
    internal partial class AutomationNode : UIA.ISelectionProvider, UIA.ISelectionItemProvider
    {
        bool UIA.ISelectionProvider.CanSelectMultiple() => InvokeSync<ISelectionProvider, bool>(x => x.CanSelectMultiple);

        bool UIA.ISelectionProvider.IsSelectionRequired() =>
            InvokeSync<ISelectionProvider, bool>(x => x.IsSelectionRequired);

        bool UIA.ISelectionItemProvider.GetIsSelected() => InvokeSync<ISelectionItemProvider, bool>(x => x.IsSelected);

        UIA.IRawElementProviderSimple? UIA.ISelectionItemProvider.GetSelectionContainer()
        {
            var peer = InvokeSync<ISelectionItemProvider, ISelectionProvider?>(x => x.SelectionContainer);
            return GetOrCreate(peer as AutomationPeer);
        }

        UIA.IRawElementProviderSimple[] UIA.ISelectionProvider.GetSelection()
        {
            var peers = InvokeSync<ISelectionProvider, IReadOnlyList<AutomationPeer>>(x => x.GetSelection());
            return peers.Select(x => (UIA.IRawElementProviderSimple)GetOrCreate(x)).ToArray();
        }

        void UIA.ISelectionItemProvider.AddToSelection() => InvokeSync<ISelectionItemProvider>(x => x.AddToSelection());

        void UIA.ISelectionItemProvider.RemoveFromSelection() =>
            InvokeSync<ISelectionItemProvider>(x => x.RemoveFromSelection());

        void UIA.ISelectionItemProvider.Select() => InvokeSync<ISelectionItemProvider>(x => x.Select());
    }
}
