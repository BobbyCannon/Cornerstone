#region References

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Cornerstone.Avalonia.AvaloniaEdit;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Avalonia.AvaloniaEdit.Editing;

[TestFixture]
public class TextEditorTests : CornerstoneUnitTest
{
	#region Methods

	[AvaloniaTest]
	public void Defaults()
	{
		var editor = new TextEditor();
		IsFalse(editor.IsModified);
		IsFalse(editor.IsDataLoaded);
	}

	[AvaloniaTest]
	public void TextInput()
	{
		var editor = new TextEditor();
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