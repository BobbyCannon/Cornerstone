#region References

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Cornerstone.Avalonia.TextEditor;
using Cornerstone.UnitTests;
using NUnit.Framework;

#endregion

namespace Cornerstone.IntegrationTests.Avalonia.TextEditor.Editing;

[TestFixture]
public class TextEditorTests : CornerstoneUnitTest
{
	#region Methods

	[AvaloniaTest]
	public void Defaults()
	{
		var editor = new TextEditorControl();
		IsFalse(editor.IsModified);
		IsFalse(editor.IsDataLoaded);
	}

	[AvaloniaTest]
	public void TextInput()
	{
		var editor = new TextEditorControl();
		var window = new Window { Content = editor };
		window.Show();
		editor.Focus();
		IsFalse(editor.IsModified);

		window.KeyTextInput("A");
		
		AreEqual("A", editor.Text);
		IsTrue(editor.IsModified);
	}

	#endregion
}