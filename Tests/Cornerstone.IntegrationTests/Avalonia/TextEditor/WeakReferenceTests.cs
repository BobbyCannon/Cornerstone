#if !DEBUG

#region References

using System;
using System.Runtime.CompilerServices;
using Cornerstone.Avalonia.TextEditor;
using Cornerstone.Avalonia.TextEditor.Editing;
using Cornerstone.Avalonia.TextEditor.Rendering;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.TextEditor;

[TestClass]
public class WeakReferenceTests : CornerstoneUnitTest
{
	#region Methods

	//[AvaloniaTest] currently failing due to Headless platform doesn't behave as the previous UnitTestApplication
	public void DocumentDoesNotHoldReferenceToLineMargin()
	{
		var textDocument = new TextDocument();
		var wr = CreateControl<TextView>(t =>
		{
			t.Document = textDocument;
			new LineNumberMargin { TextView = t };
		});

		IsFalse(wr.IsAlive);
		GC.KeepAlive(textDocument);
	}

	//[AvaloniaTest] currently failing due to Headless platform doesn't behave as the previous UnitTestApplication
	public void DocumentDoesNotHoldReferenceToTextView()
	{
		var textDocument = new TextDocument();
		AreEqual(0, textDocument.LineTrackers.Count);

		var wr = CreateControl<TextView>(t => t.Document = textDocument);

		IsFalse(wr.IsAlive);
		// document cannot immediately clear the line tracker
		AreEqual(1, textDocument.LineTrackers.Count);

		// but it should clear it on the next change
		textDocument.Insert(0, "a");
		AreEqual(0, textDocument.LineTrackers.Count);
	}

	//[AvaloniaTest] currently failing due to Headless platform doesn't behave as the previous UnitTestApplication
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

	//[AvaloniaTest] // currently fails due to some Avalonia static
	private void DocumentDoesNotHoldReferenceToTextArea()
	{
		var textDocument = new TextDocument();
		var wr = CreateControl<TextArea>(t => t.Document = textDocument);
		IsFalse(wr.IsAlive);
		GC.KeepAlive(textDocument);
	}

	//[AvaloniaTest] // currently fails due to some Avalonia static
	private void DocumentDoesNotHoldReferenceToTextEditor()
	{
		var textDocument = new TextDocument();
		var wr = CreateControl<TextEditor>(t => t.Document = textDocument);
		IsFalse(wr.IsAlive);
		GC.KeepAlive(textDocument);
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
#endif