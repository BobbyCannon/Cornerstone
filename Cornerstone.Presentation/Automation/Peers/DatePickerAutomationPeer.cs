using System;
using Cornerstone.Presentation.Automation.Provider;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Input;

namespace Cornerstone.Presentation.Automation.Peers;

public class DatePickerAutomationPeer : ControlAutomationPeer, IValueProvider
{
    public DatePickerAutomationPeer(DatePicker owner)
        : base(owner)
    {
    }

    public bool IsReadOnly => false;
    public new DatePicker Owner => (DatePicker)base.Owner;
    public string? Value => Owner.SelectedDate?.ToString();

    public void SetValue(string? value)
    {
        if (DateTimeOffset.TryParse(value, out var result))
            Owner.SelectedDate = result;
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
}
