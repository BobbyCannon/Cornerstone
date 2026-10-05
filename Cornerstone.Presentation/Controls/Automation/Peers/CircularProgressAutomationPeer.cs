using System;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Automation.Provider;
using Cornerstone.Presentation.Controls.Primitives;

namespace Cornerstone.Presentation.Controls.Automation.Peers
{
	public class CircularProgressAutomationPeer : RangeBaseAutomationPeer, IRangeValueProvider
	{
		public CircularProgressAutomationPeer(RangeBase owner)
			: base(owner)
		{
		}

		protected override string GetClassNameCore()
		{
			return "CircularProgress";
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.ProgressBar;
		}

		void IRangeValueProvider.SetValue(double val)
		{
			throw new InvalidOperationException("CircularProgress is ReadOnly, value can't be set.");
		}

		bool IRangeValueProvider.IsReadOnly
		{
			get => true;
		}

		double IRangeValueProvider.LargeChange
		{
			get => double.NaN;
		}

		double IRangeValueProvider.SmallChange
		{
			get => double.NaN;
		}
	}
}
