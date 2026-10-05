#region References

using Cornerstone.Presentation.Automation;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Automation.Provider;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Controls.Input;

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