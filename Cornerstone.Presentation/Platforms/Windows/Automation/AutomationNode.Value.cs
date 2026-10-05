using System.Runtime.InteropServices;
using Cornerstone.Presentation.Automation.Provider;
using UIA = Cornerstone.Presentation.Platforms.Windows.Automation.Interop;

namespace Cornerstone.Presentation.Platforms.Windows.Automation
{
    internal partial class AutomationNode : UIA.IValueProvider
    {
        bool UIA.IValueProvider.GetIsReadOnly() => InvokeSync<IValueProvider, bool>(x => x.IsReadOnly);
        string? UIA.IValueProvider.GetValue() => InvokeSync<IValueProvider, string?>(x => x.Value);

        void UIA.IValueProvider.SetValue([MarshalAs(UnmanagedType.LPWStr)] string? value)
        {
            InvokeSync<IValueProvider>(x => x.SetValue(value));
        }
    }
}
