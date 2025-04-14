#region References

using System;
using System.Runtime.CompilerServices;
using Avalonia.Headless.NUnit;
using Cornerstone.Avalonia.TextEditor;
using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Avalonia.TextEditor.Editing;
using Cornerstone.Avalonia.TextEditor.Rendering;
using Cornerstone.UnitTests;

#endregion

namespace Cornerstone.IntegrationTests.Avalonia.TextEditor;

//[TestFixture]
public class WeakReferenceTests : CornerstoneUnitTest
{
	#region Methods

	//[AvaloniaTest]
	public void DocumentDoesNotHoldReferenceToLineMargin()
	{
		var textDocument = new TextEditorDocument();
		var wr = CreateControl<TextView>(t =>
		{
			t.Document = textDocument;
			new LineNumberMargin { TextView = t };
		});

		IsFalse(wr.IsAlive);
		GC.KeepAlive(textDocument);
	}

	//[AvaloniaTest]
	public void DocumentDoesNotHoldReferenceToTextArea()
	{
		var textDocument = new TextEditorDocument();
		var wr = CreateControl<TextArea>(t => t.Document = textDocument);
		IsFalse(wr.IsAlive);
		GC.KeepAlive(textDocument);
	}

	//[AvaloniaTest]
	public void DocumentDoesNotHoldReferenceToTextEditor()
	{
		var textDocument = new TextEditorDocument();
		var wr = CreateControl<TextEditorControl>(t => t.Document = textDocument);
		IsFalse(wr.IsAlive);
		GC.KeepAlive(textDocument);
	}

	//[AvaloniaTest]
	public void DocumentDoesNotHoldReferenceToTextView()
	{
		var textDocument = new TextEditorDocument();
		AreEqual(0, textDocument.LineTrackers.Count);

		var wr = CreateControl<TextView>(t => t.Document = textDocument);

		IsFalse(wr.IsAlive);
		// document cannot immediately clear the line tracker
		AreEqual(1, textDocument.LineTrackers.Count);

		// but it should clear it on the next change
		textDocument.Insert(0, "a");
		AreEqual(0, textDocument.LineTrackers.Count);
	}

	//[AvaloniaTest]
	public void TextViewCanBeCollectedTest()
	{
		var wr = CreateControl<TextView>();
		IsFalse(wr.IsAlive);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference CreateControl<T>(Action<T> action = null)
		where T : class, new()
	{
		WeakReference wr;

		var control = new T();
		wr = new WeakReference(control);
		action?.Invoke(control);
		control = null;

		GarbageCollect();

		return wr;
	}

	private static void GarbageCollect()
	{
		for (var i = 0; i < 3; i++)
		{
			GC.WaitForPendingFinalizers();
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
		}
	}

	#endregion
}