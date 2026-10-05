using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Cornerstone.Presentation.Platforms.Windows.Automation.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("2acad808-b2d4-452d-a407-91ff1ad167b2")]
internal partial interface ISelectionItemProvider
{
    void Select();
    void AddToSelection();
    void RemoveFromSelection();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetIsSelected();

    IRawElementProviderSimple? GetSelectionContainer();
}
