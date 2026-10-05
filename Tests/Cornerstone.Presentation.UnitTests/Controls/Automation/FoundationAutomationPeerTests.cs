#region References

using Cornerstone.Presentation.Automation;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Automation.Provider;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Automation;

[TestClass]
public class FoundationAutomationPeerTests
{
	#region Classes

	[TestClass]
	public class CalendarDatePickerPeer : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CreatesCalendarDatePickerAutomationPeer()
		{
			var control = new CalendarDatePicker();
			var peer = ControlAutomationPeer.CreatePeerForElement(control);

			CornerstoneTest.IsType<CalendarDatePickerAutomationPeer>(peer);
		}

		[PresentationTestMethod]
		public void ExpandCollapseTracksIsDropDownOpen()
		{
			var control = new CalendarDatePicker();
			var peer = (IExpandCollapseProvider) ControlAutomationPeer.CreatePeerForElement(control);

			CornerstoneTest.IsTrue(peer.ShowsMenu);
			CornerstoneTest.AreEqual(ExpandCollapseState.Collapsed, peer.ExpandCollapseState);

			peer.Expand();
			CornerstoneTest.IsTrue(control.IsDropDownOpen);
			CornerstoneTest.AreEqual(ExpandCollapseState.Expanded, peer.ExpandCollapseState);

			peer.Collapse();
			CornerstoneTest.IsFalse(control.IsDropDownOpen);
			CornerstoneTest.AreEqual(ExpandCollapseState.Collapsed, peer.ExpandCollapseState);
		}

		[PresentationTestMethod]
		public void HasButtonControlType()
		{
			var control = new CalendarDatePicker();
			var peer = (CalendarDatePickerAutomationPeer) ControlAutomationPeer.CreatePeerForElement(control);

			CornerstoneTest.AreEqual(AutomationControlType.Button, peer.GetAutomationControlType());
			CornerstoneTest.AreEqual("CalendarDatePicker", peer.GetClassName());
		}

		[PresentationTestMethod]
		public void ImplementsIInvokeAndIValueProviders()
		{
			var control = new CalendarDatePicker();
			var peer = ControlAutomationPeer.CreatePeerForElement(control);

			CornerstoneTest.IsAssignableFrom<IInvokeProvider>(peer);
			CornerstoneTest.IsAssignableFrom<IExpandCollapseProvider>(peer);
			CornerstoneTest.IsAssignableFrom<IValueProvider>(peer);
		}

		[PresentationTestMethod]
		public void InvokeOpensDropDown()
		{
			var control = new CalendarDatePicker();
			var peer = (IInvokeProvider) ControlAutomationPeer.CreatePeerForElement(control);

			peer.Invoke();

			CornerstoneTest.IsTrue(control.IsDropDownOpen);
		}

		[PresentationTestMethod]
		public void PropertyChangedRaisesExpandCollapseStateWhenDropDownOpenChanges()
		{
			var control = new CalendarDatePicker();
			var peer = (CalendarDatePickerAutomationPeer) ControlAutomationPeer.CreatePeerForElement(control);
			AutomationPropertyChangedEventArgs changed = null;

			peer.PropertyChanged += (_, e) =>
			{
				if (e.Property == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty)
				{
					changed = e;
				}
			};

			control.IsDropDownOpen = true;

			CornerstoneTest.IsNotNull(changed);
			CornerstoneTest.AreEqual(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, changed!.Property);
			CornerstoneTest.AreEqual(ExpandCollapseState.Collapsed, changed.OldValue);
			CornerstoneTest.AreEqual(ExpandCollapseState.Expanded, changed.NewValue);
		}

		[PresentationTestMethod]
		public void PropertyChangedRaisesValueWhenTextChanges()
		{
			var control = new CalendarDatePicker();
			var peer = (CalendarDatePickerAutomationPeer) ControlAutomationPeer.CreatePeerForElement(control);
			AutomationPropertyChangedEventArgs changed = null;

			peer.PropertyChanged += (_, e) =>
			{
				if (e.Property == ValuePatternIdentifiers.ValueProperty)
				{
					changed = e;
				}
			};

			control.Text = "January";

			CornerstoneTest.IsNotNull(changed);
			CornerstoneTest.AreEqual(ValuePatternIdentifiers.ValueProperty, changed!.Property);
			CornerstoneTest.IsNull(changed.OldValue);
			CornerstoneTest.AreEqual("January", changed.NewValue);
		}

		[PresentationTestMethod]
		public void SetValueUpdatesText()
		{
			var control = new CalendarDatePicker();
			var peer = (IValueProvider) ControlAutomationPeer.CreatePeerForElement(control);

			CornerstoneTest.IsFalse(peer.IsReadOnly);

			peer.SetValue("automation text");

			CornerstoneTest.AreEqual("automation text", control.Text);
		}

		[PresentationTestMethod]
		public void ValueMirrorsOwnerText()
		{
			var control = new CalendarDatePicker { Text = "typed value" };
			var peer = (CalendarDatePickerAutomationPeer) ControlAutomationPeer.CreatePeerForElement(control);

			CornerstoneTest.AreEqual("typed value", peer.Value);

			control.Text = "updated typed value";

			CornerstoneTest.AreEqual("updated typed value", peer.Value);
		}

		#endregion
	}

	[TestClass]
	public class NumericUpDownPeer : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CreatesNumericUpDownAutomationPeer()
		{
			var control = new NumericUpDown();
			var peer = ControlAutomationPeer.CreatePeerForElement(control);

			CornerstoneTest.IsType<NumericUpDownAutomationPeer>(peer);
		}

		[PresentationTestMethod]
		public void ImplementsIRangeValueProvider()
		{
			var control = new NumericUpDown();
			var peer = ControlAutomationPeer.CreatePeerForElement(control);

			CornerstoneTest.IsAssignableFrom<IRangeValueProvider>(peer);
		}

		[PresentationTestMethod]
		public void IsSpinnerControlType()
		{
			var control = new NumericUpDown();
			var peer = (NumericUpDownAutomationPeer) ControlAutomationPeer.CreatePeerForElement(control);

			CornerstoneTest.AreEqual(AutomationControlType.Spinner, peer.GetAutomationControlType());
			CornerstoneTest.AreEqual("NumericUpDown", peer.GetClassName());
		}

		[PresentationTestMethod]
		public void NullValueReportsDefaultClampedToRange()
		{
			var control = new NumericUpDown
			{
				Minimum = 10,
				Maximum = 20,
				Value = null
			};

			var peer = (IRangeValueProvider) ControlAutomationPeer.CreatePeerForElement(control);

			CornerstoneTest.AreEqual(10d, peer.Value);
		}

		[PresentationTestMethod]
		public void PropertyChangedRaisesRangeWhenValueChanges()
		{
			var control = new NumericUpDown();
			var peer = (NumericUpDownAutomationPeer) ControlAutomationPeer.CreatePeerForElement(control);
			AutomationPropertyChangedEventArgs changed = null;

			peer.PropertyChanged += (_, e) =>
			{
				if (e.Property == RangeValuePatternIdentifiers.ValueProperty)
				{
					changed = e;
				}
			};

			control.Value = 7.5m;

			CornerstoneTest.IsNotNull(changed);
			CornerstoneTest.AreEqual(RangeValuePatternIdentifiers.ValueProperty, changed!.Property);
			CornerstoneTest.IsNull(changed.OldValue);
			CornerstoneTest.AreEqual(7.5m, changed.NewValue);
		}

		[PresentationTestMethod]
		public void RangeValuesReflectOwner()
		{
			var control = new NumericUpDown
			{
				Minimum = 10,
				Maximum = 20,
				Increment = 3,
				IsReadOnly = true,
				Value = 14
			};

			var peer = (IRangeValueProvider) ControlAutomationPeer.CreatePeerForElement(control);

			CornerstoneTest.AreEqual(10d, peer.Minimum);
			CornerstoneTest.AreEqual(20d, peer.Maximum);
			CornerstoneTest.AreEqual(14d, peer.Value);
			CornerstoneTest.AreEqual(3d, peer.SmallChange);
			CornerstoneTest.AreEqual(3d, peer.LargeChange);
			CornerstoneTest.IsTrue(peer.IsReadOnly);
		}

		[PresentationTestMethod]
		public void SetValueUpdatesOwnerValue()
		{
			var control = new NumericUpDown();
			var peer = (IRangeValueProvider) ControlAutomationPeer.CreatePeerForElement(control);

			peer.SetValue(42.5);

			CornerstoneTest.AreEqual(42.5m, control.Value);
		}

		#endregion
	}

	[TestClass]
	public class ToolTipPeer : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ControlTypeIsToolTip()
		{
			var control = new ToolTip();
			var peer = (ToolTipAutomationPeer) ControlAutomationPeer.CreatePeerForElement(control);

			CornerstoneTest.AreEqual(AutomationControlType.ToolTip, peer.GetAutomationControlType());
			CornerstoneTest.AreEqual("ToolTip", peer.GetClassName());
		}

		[PresentationTestMethod]
		public void CreatesToolTipAutomationPeer()
		{
			var control = new ToolTip();
			var peer = ControlAutomationPeer.CreatePeerForElement(control);

			CornerstoneTest.IsType<ToolTipAutomationPeer>(peer);
		}

		#endregion
	}

	#endregion
}