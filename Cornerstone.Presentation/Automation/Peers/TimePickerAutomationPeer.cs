using System;
using Cornerstone.Presentation.Automation.Provider;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Input;

namespace Cornerstone.Presentation.Automation.Peers;

public class TimePickerAutomationPeer : ControlAutomationPeer, IValueProvider
{
    public TimePickerAutomationPeer(TimePicker owner)
        : base(owner)
    {
    }

    public bool IsReadOnly => false;
    public new TimePicker Owner => (TimePicker)base.Owner;
    public string? Value => Owner.SelectedTime?.ToString();

    public void SetValue(string? value)
    {
        if (TimeSpan.TryParse(value, out var result))
            Owner.SelectedTime = result;
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
}
