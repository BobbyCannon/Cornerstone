#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class CheckBoxTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void CheckBoxLetterSpacingCanBeNegative()
	{
		var checkBox = new CheckBox { LetterSpacing = -1.5 };
		CornerstoneTest.AreEqual(-1.5, checkBox.LetterSpacing);
	}

	[PresentationTestMethod]
	public void CheckBoxLetterSpacingCanBeSetAndRetrieved()
	{
		var checkBox = new CheckBox { LetterSpacing = 2.5 };
		CornerstoneTest.AreEqual(2.5, checkBox.LetterSpacing);
	}

	[PresentationTestMethod]
	public void CheckBoxLetterSpacingDefaultValueIsZero()
	{
		var checkBox = new CheckBox();
		CornerstoneTest.AreEqual(0, checkBox.LetterSpacing);
	}

	[PresentationTestMethod]
	public void CheckBoxLetterSpacingInheritsFromTemplatedControl()
	{
		var checkBox = new CheckBox { LetterSpacing = 3.0 };

		// LetterSpacing is inherited from TemplatedControl
		CornerstoneTest.AreEqual(3.0, checkBox.LetterSpacing);
	}

	#endregion
}