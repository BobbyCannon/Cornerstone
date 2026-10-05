#region References

using System;
using Cornerstone.Presentation.Media.TextFormatting.Unicode;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.TextFormatting;

[TestClass]
public class Utf16UtilsTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow("\ud87e\udc32123", 1, 2)]
	[DataRow("\ud87e\udc32123", 2, 3)]
	[DataRow("test", 3, 3)]
	[DataRow("\ud87e\udc32", 0, 0)]
	[DataRow("12\ud87e\udc3212", 2, 2)]
	[DataRow("12\ud87e\udc3212", 3, 4)]
	public void CharacterOffsetToStringOffset(string s, int charOffset, int stringOffset)
	{
		CornerstoneTest.AreEqual(stringOffset, Utf16Utils.CharacterOffsetToStringOffset(s, charOffset, false));
	}

	[PresentationTestMethod]
	[DataRow("\ud87e\udc32", 2, true)]
	[DataRow("12", 2, true)]
	public void CharacterOffsetToStringOffsetThrowsOnOutOfRange(string s, int charOffset, bool throws)
	{
		if (throws)
		{
			Assert.Throws<IndexOutOfRangeException>(() =>
				Utf16Utils.CharacterOffsetToStringOffset(s, charOffset, true));
		}
		else
		{
			Utf16Utils.CharacterOffsetToStringOffset(s, charOffset, true);
		}
	}

	#endregion
}