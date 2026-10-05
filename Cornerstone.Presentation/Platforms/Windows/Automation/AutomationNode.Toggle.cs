using Cornerstone.Presentation.Automation.Provider;
using UIA = Cornerstone.Presentation.Platforms.Windows.Automation.Interop;

namespace Cornerstone.Presentation.Platforms.Windows.Automation
{
    internal partial class AutomationNode : UIA.IToggleProvider
    {
        ToggleState UIA.IToggleProvider.GetToggleState() => InvokeSync<IToggleProvider, ToggleState>(x => x.ToggleState);
        void UIA.IToggleProvider.Toggle() => InvokeSync<IToggleProvider>(x => x.Toggle());
    }
}
