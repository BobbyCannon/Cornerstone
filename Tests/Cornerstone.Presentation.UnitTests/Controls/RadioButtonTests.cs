#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class RadioButtonTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void IndeterminateRadioButtonIsNotUncheckedAfterCheckingOtherRadioButton(bool isThreeState)
	{
		var panel = new Panel();

		var radioButton1 = new RadioButton();
		radioButton1.IsThreeState = false;
		radioButton1.IsChecked = false;

		var radioButton2 = new RadioButton();
		radioButton2.IsThreeState = isThreeState;
		radioButton2.IsChecked = null;

		panel.Children.Add(radioButton1);
		panel.Children.Add(radioButton2);

		CornerstoneTest.IsNull(radioButton2.IsChecked);

		radioButton1.IsChecked = true;

		CornerstoneTest.IsTrue(radioButton1.IsChecked);
		CornerstoneTest.IsNull(radioButton2.IsChecked);
	}

	[PresentationTestMethod]
	public void RadioButtonEmptyGroupNameNotInfluenceOtherGroups()
	{
		var parent = new Panel();

		var radioButton1 = new RadioButton();
		radioButton1.GroupName = "A";
		radioButton1.IsChecked = true;
		var radioButton2 = new RadioButton();
		radioButton2.GroupName = "A";
		radioButton2.IsChecked = false;

		var radioButton3 = new RadioButton();
		radioButton3.GroupName = null;
		radioButton3.IsChecked = false;
		var radioButton4 = new RadioButton();
		radioButton4.GroupName = null;
		radioButton4.IsChecked = true;

		parent.Children.Add(radioButton1);
		parent.Children.Add(radioButton2);
		parent.Children.Add(radioButton3);
		parent.Children.Add(radioButton4);

		CornerstoneTest.IsTrue(radioButton1.IsChecked);
		CornerstoneTest.IsFalse(radioButton2.IsChecked);
		CornerstoneTest.IsFalse(radioButton3.IsChecked);
		CornerstoneTest.IsTrue(radioButton4.IsChecked);

		radioButton3.IsChecked = true;

		CornerstoneTest.IsTrue(radioButton1.IsChecked);
		CornerstoneTest.IsFalse(radioButton2.IsChecked);
		CornerstoneTest.IsTrue(radioButton3.IsChecked);
		CornerstoneTest.IsFalse(radioButton4.IsChecked);
	}

	[PresentationTestMethod]
	public void RadioButtonInSameGroupIsUnchecked()
	{
		var parent = new Panel();

		var panel1 = new Panel();
		var panel2 = new Panel();

		parent.Children.Add(panel1);
		parent.Children.Add(panel2);

		var radioButton1 = new RadioButton();
		radioButton1.GroupName = "A";
		radioButton1.IsChecked = false;

		var radioButton2 = new RadioButton();
		radioButton2.GroupName = "A";
		radioButton2.IsChecked = true;

		var radioButton3 = new RadioButton();
		radioButton3.GroupName = "A";
		radioButton3.IsChecked = false;

		panel1.Children.Add(radioButton1);
		panel1.Children.Add(radioButton2);
		panel2.Children.Add(radioButton3);

		CornerstoneTest.IsFalse(radioButton1.IsChecked);
		CornerstoneTest.IsTrue(radioButton2.IsChecked);
		CornerstoneTest.IsFalse(radioButton3.IsChecked);

		radioButton3.IsChecked = true;

		CornerstoneTest.IsFalse(radioButton1.IsChecked);
		CornerstoneTest.IsFalse(radioButton2.IsChecked);
		CornerstoneTest.IsTrue(radioButton3.IsChecked);
	}

	[PresentationTestMethod]
	public void RadioButtonLetterSpacingCanBeSetAndRetrieved()
	{
		var radioButton = new RadioButton { LetterSpacing = 2.5 };
		CornerstoneTest.AreEqual(2.5, radioButton.LetterSpacing);
	}

	[PresentationTestMethod]
	public void RadioButtonLetterSpacingDefaultValueIsZero()
	{
		var radioButton = new RadioButton();
		CornerstoneTest.AreEqual(0, radioButton.LetterSpacing);
	}

	[PresentationTestMethod]
	public void RadioButtonLetterSpacingInheritsFromTemplatedControl()
	{
		var radioButton = new RadioButton { LetterSpacing = 3.0 };

		// LetterSpacing is inherited from TemplatedControl
		CornerstoneTest.AreEqual(3.0, radioButton.LetterSpacing);
	}

	#endregion
}