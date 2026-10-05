#region References

using System;
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
public class ComboBoxAutomationPeerTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void EditableComboBoxIsNotReadOnly()
	{
		var provider = Value(new ComboBox { IsEditable = true });

		CornerstoneTest.IsFalse(provider.IsReadOnly);
	}

	[PresentationTestMethod]
	public void NonEditableComboBoxIsReadOnly()
	{
		var provider = Value(new ComboBox());

		CornerstoneTest.IsTrue(provider.IsReadOnly);
	}

	[PresentationTestMethod]
	public void SetValueThrowsWhenNotEditable()
	{
		var provider = Value(new ComboBox());

		Assert.Throws<InvalidOperationException>(() => provider.SetValue("x"));
	}

	[PresentationTestMethod]
	public void SetValueUpdatesTextWhenEditable()
	{
		var comboBox = new ComboBox { IsEditable = true };
		var provider = Value(comboBox);

		provider.SetValue("typed");

		CornerstoneTest.AreEqual("typed", comboBox.Text);
		CornerstoneTest.AreEqual("typed", provider.Value);
	}

	[PresentationTestMethod]
	public void TextChangeRaisesValuePropertyChanged()
	{
		var comboBox = new ComboBox { IsEditable = true };
		var peer = ControlAutomationPeer.CreatePeerForElement(comboBox);

		var raised = 0;
		peer.PropertyChanged += (_, e) =>
		{
			if (e.Property == ValuePatternIdentifiers.ValueProperty)
			{
				CornerstoneTest.AreEqual("abc", e.NewValue);
				raised++;
			}
		};

		comboBox.Text = "abc";

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void ValueReturnsTextWhenEditable()
	{
		var provider = Value(new ComboBox { IsEditable = true, Text = "hello" });

		CornerstoneTest.AreEqual("hello", provider.Value);
	}

	private static IValueProvider Value(ComboBox comboBox)
	{
		return CornerstoneTest.IsAssignableFrom<IValueProvider>(ControlAutomationPeer.CreatePeerForElement(comboBox));
	}

	#endregion
}