#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class LabelTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void LabelLetterSpacingCanBeNegative()
	{
		var label = new Label { LetterSpacing = -1.5 };
		CornerstoneTest.AreEqual(-1.5, label.LetterSpacing);
	}

	[PresentationTestMethod]
	public void LabelLetterSpacingCanBeSetAndRetrieved()
	{
		var label = new Label { LetterSpacing = 2.5 };
		CornerstoneTest.AreEqual(2.5, label.LetterSpacing);
	}

	[PresentationTestMethod]
	public void LabelLetterSpacingDefaultValueIsZero()
	{
		var label = new Label();
		CornerstoneTest.AreEqual(0, label.LetterSpacing);
	}

	[PresentationTestMethod]
	public void LabelLetterSpacingInheritsFromTemplatedControl()
	{
		var label = new Label { LetterSpacing = 3.0 };

		// LetterSpacing is inherited from TemplatedControl
		CornerstoneTest.AreEqual(3.0, label.LetterSpacing);
	}

	#endregion
}