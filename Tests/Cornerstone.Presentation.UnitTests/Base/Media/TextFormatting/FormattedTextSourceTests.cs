#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.TextFormatting;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.TextFormatting;

[TestClass]
public class FormattedTextSourceTests
{
	#region Methods

	[PresentationTestMethod]
	public void GetTextRunWithTwoTextStyleOverridesShouldGenerateCorrectFirstRun()
	{
		//Prepare a sample text: The two "He" at the beginning of each line should be displayed with other TextRunProperties
		var text = "Hello World\r\nHello";
		var typeface = new Typeface();
		var defaultTextRunProperties = new GenericTextRunProperties(typeface);
		IReadOnlyList<ValueSpan<TextRunProperties>> textStyleOverrides = new List<ValueSpan<TextRunProperties>>
		{
			new(0, 2, new GenericTextRunProperties(typeface, backgroundBrush: Brushes.Aqua)),
			new(13, 2, new GenericTextRunProperties(typeface, backgroundBrush: Brushes.Aqua))
		};

		var textSource = new FormattedTextSource(text, defaultTextRunProperties, textStyleOverrides);
		var textRun = textSource.GetTextRun(0);

		CornerstoneTest.IsNotNull(textRun);
		CornerstoneTest.AreEqual(2, textRun.Length);
		CornerstoneTest.AreEqual("He", textRun.Text.ToString());
	}

	#endregion
}