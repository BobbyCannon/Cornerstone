#region References

using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.NumberBox;

public class NumberBoxAutomationPeer : ControlAutomationPeer, IRangeValueProvider, IValueProvider
{
	#region Constructors

	public NumberBoxAutomationPeer(Control owner) : base(owner)
	{
	}

	#endregion

	#region Properties

	public bool IsReadOnly => false;

	public double LargeChange => GetImpl().LargeChange;

	public double Maximum => GetImpl().Maximum;

	public double Minimum => GetImpl().Minimum;

	public double SmallChange => GetImpl().SmallChange;

	public double Value => GetImpl().Value;

	string IValueProvider.Value => GetImpl().Text;

	#endregion

	#region Methods

	public void SetValue(string value)
	{
		GetImpl().Text = value;
	}

	public void SetValue(double value)
	{
		GetImpl().Value = value;
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return AutomationControlType.Spinner;
	}

	protected override string GetNameCore()
	{
		var name = base.GetNameCore();
		if (string.IsNullOrEmpty(name))
		{
			if (Owner is NumberBox nb)
			{
				name = nb.Header is string ? nb.Header.ToString() : null;
			}
		}

		return name;
	}

	internal void RaiseValueChangedEvent(double oldValue, double newValue)
	{
		RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, oldValue, newValue);
	}

	private NumberBox GetImpl()
	{
		return (NumberBox) Owner;
	}

	#endregion
}