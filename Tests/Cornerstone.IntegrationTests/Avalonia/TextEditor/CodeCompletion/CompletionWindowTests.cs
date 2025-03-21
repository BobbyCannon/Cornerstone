#region References

using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Cornerstone.Avalonia.TextEditor;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.IntegrationTests.Avalonia.TextEditor.CodeCompletion;

[TestClass]
public class CompletionWindowTests : CornerstoneUnitTest
{
	#region Methods

	[AvaloniaTest]
	public void Insertion()
	{
		UnitTestApplication.InitializeStyles();

	}

	private static TextEditorControl CreateEditor()
	{
		var textEditor = new TextEditorControl();
		var window = new Window { Content = textEditor };
		window.Show();
		textEditor.ApplyTemplate();
		return textEditor;
	}

	#endregion
}