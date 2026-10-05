using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Cornerstone.Presentation.Automation.Provider;

namespace Cornerstone.Presentation.Platforms.Windows.Automation.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("56d00bd0-c4f4-433c-a836-1a52a57e0892")]
internal partial interface IToggleProvider
{
    void Toggle();
    ToggleState GetToggleState();
}
