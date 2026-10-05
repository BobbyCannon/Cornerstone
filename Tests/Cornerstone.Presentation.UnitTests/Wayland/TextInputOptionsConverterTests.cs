#region References

using Cornerstone.Presentation.Input.TextInput;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Wayland;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NWayland.Protocols.TextInputUnstableV3;

#endregion

namespace Cornerstone.Presentation.UnitTests.Wayland;

#region References

using Hint = ZwpTextInputV3.ContentHintEnum;
using Purpose = ZwpTextInputV3.ContentPurposeEnum;

#endregion

[TestClass]
public class TextInputOptionsConverterTests
{
	#region Methods

	[PresentationTestMethod]
	public void AlphaAddsLatinHint()
	{
		var (h, _) = TextInputOptionsConverter.Convert(new TextInputOptions { ContentType = TextInputContentType.Alpha });
		CornerstoneTest.IsTrue((h & Hint.Latin) != 0);
	}

	[PresentationTestMethod]
	[DataRow(TextInputContentType.Digits, Purpose.Digits)]
	[DataRow(TextInputContentType.Pin, Purpose.Pin)]
	[DataRow(TextInputContentType.Number, Purpose.Number)]
	[DataRow(TextInputContentType.Email, Purpose.Email)]
	[DataRow(TextInputContentType.Url, Purpose.Url)]
	[DataRow(TextInputContentType.Name, Purpose.Name)]
	public void ContentTypeMapsPurpose(TextInputContentType ct, Purpose expected)
	{
		var (_, p) = TextInputOptionsConverter.Convert(new TextInputOptions { ContentType = ct });
		CornerstoneTest.AreEqual(expected, p);
	}

	[PresentationTestMethod]
	public void DefaultIsNoneNormal()
	{
		var (h, p) = TextInputOptionsConverter.Convert(new TextInputOptions());
		CornerstoneTest.AreEqual(Hint.None, h);
		CornerstoneTest.AreEqual(Purpose.Normal, p);
	}

	[PresentationTestMethod]
	public void FlagsComposeIntoHint()
	{
		var (h, _) = TextInputOptionsConverter.Convert(new TextInputOptions
		{
			Multiline = true,
			IsSensitive = true,
			Lowercase = true,
			Uppercase = true,
			AutoCapitalization = true,
			ShowSuggestions = true
		});
		CornerstoneTest.IsTrue((h & Hint.Multiline) != 0);
		CornerstoneTest.IsTrue((h & Hint.SensitiveData) != 0);
		CornerstoneTest.IsTrue((h & Hint.Lowercase) != 0);
		CornerstoneTest.IsTrue((h & Hint.Uppercase) != 0);
		CornerstoneTest.IsTrue((h & Hint.AutoCapitalization) != 0);
		CornerstoneTest.IsTrue((h & Hint.Completion) != 0);
		CornerstoneTest.IsTrue((h & Hint.Spellcheck) != 0);
	}

	[PresentationTestMethod]
	public void PasswordForcesHiddenAndSensitiveEvenWithShowSuggestions()
	{
		var (h, p) = TextInputOptionsConverter.Convert(new TextInputOptions
		{
			ContentType = TextInputContentType.Password,
			ShowSuggestions = true
		});
		CornerstoneTest.AreEqual(Purpose.Password, p);
		CornerstoneTest.IsTrue((h & Hint.HiddenText) != 0);
		CornerstoneTest.IsTrue((h & Hint.SensitiveData) != 0);
		CornerstoneTest.IsFalse((h & Hint.Completion) != 0);
		CornerstoneTest.IsFalse((h & Hint.Spellcheck) != 0);
	}

	[PresentationTestMethod]
	public void ShowSuggestionsFalseOmitsCompletion()
	{
		var (h, _) = TextInputOptionsConverter.Convert(new TextInputOptions { ShowSuggestions = false });
		CornerstoneTest.IsFalse((h & Hint.Completion) != 0);
		CornerstoneTest.IsFalse((h & Hint.Spellcheck) != 0);
	}

	#endregion
}