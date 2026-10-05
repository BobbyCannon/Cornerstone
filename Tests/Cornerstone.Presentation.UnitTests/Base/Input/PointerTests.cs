#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.GestureRecognizers;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Input;

[TestClass]
public class PointerTests : PointerTestsBase
{
	#region Methods

	[PresentationTestMethod]
	public void CaptureCapturedShouldNotCallPlatform()
	{
		var pointer = new TestPointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);

		var capture = new Border();
		pointer.Capture(capture);
		pointer.Capture(capture);

		CornerstoneTest.AreEqual(1, pointer.PlatformCaptureCalled);

		pointer.Capture(null);
		pointer.Capture(null);

		CornerstoneTest.AreEqual(2, pointer.PlatformCaptureCalled);
	}

	[PresentationTestMethod]
	public void CaptureExplicitShouldNotifyAfterHandledImplicit()
	{
		var pointer = new TestPointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);

		var capture = new Border();

		List<CaptureSource> sources = new();
		capture.PointerCaptureChanging += (sender, e) =>
		{
			sources.Add(e.CaptureSource);
			e.Handled = e.CaptureSource == CaptureSource.Implicit;
		};

		pointer.Capture(capture, CaptureSource.Implicit);
		pointer.Capture(capture, CaptureSource.Explicit);

		CornerstoneTest.IsTrue(sources.SequenceEqual([CaptureSource.Implicit, CaptureSource.Explicit]));

		CornerstoneTest.AreEqual(1, pointer.PlatformCaptureCalled);

		pointer.Capture(null, CaptureSource.Implicit);
		pointer.Capture(null, CaptureSource.Explicit);
		CornerstoneTest.IsTrue(sources.SequenceEqual([CaptureSource.Implicit, CaptureSource.Explicit, CaptureSource.Implicit, CaptureSource.Explicit]));

		CornerstoneTest.AreEqual(2, pointer.PlatformCaptureCalled);
	}

	[PresentationTestMethod]
	public void CaptureExplicitShouldNotifyAfterImplicit()
	{
		var pointer = new TestPointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);

		var capture = new Border();

		List<CaptureSource> sources = new();
		capture.PointerCaptureChanging += (sender, e) => { sources.Add(e.CaptureSource); };

		pointer.Capture(capture, CaptureSource.Implicit);
		pointer.Capture(capture, CaptureSource.Explicit);

		CornerstoneTest.IsTrue(sources.SequenceEqual([CaptureSource.Implicit, CaptureSource.Explicit]));

		CornerstoneTest.AreEqual(1, pointer.PlatformCaptureCalled);

		pointer.Capture(null, CaptureSource.Implicit); // not ignored, so captured element will become null
		pointer.Capture(null, CaptureSource.Explicit); // changing from null to null does not notify anything

		CornerstoneTest.IsTrue(sources.SequenceEqual([CaptureSource.Implicit, CaptureSource.Explicit, CaptureSource.Implicit]));

		CornerstoneTest.AreEqual(2, pointer.PlatformCaptureCalled);
	}

	[PresentationTestMethod]
	public void GestureRecognizerCaptureShouldKeepPlatformCaptureOnSameTarget()
	{
		var pointer = new TestPointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
		var target = new Border();
		var recognizer = new TestGestureRecognizer { Target = target };

		pointer.Capture(target, CaptureSource.Implicit);
		recognizer.CapturePointer(pointer);

		CornerstoneTest.IsNull(pointer.Captured);
		CornerstoneTest.Same(recognizer, pointer.CapturedGestureRecognizer);
		CornerstoneTest.AreEqual([target], pointer.PlatformCaptures);
	}

	[PresentationTestMethod]
	public void GestureRecognizerCaptureShouldMovePlatformCaptureToTarget()
	{
		var pointer = new TestPointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
		var initialCapture = new Border();
		var target = new Border { Child = initialCapture };
		var recognizer = new TestGestureRecognizer { Target = target };

		pointer.Capture(initialCapture, CaptureSource.Implicit);
		recognizer.CapturePointer(pointer);

		CornerstoneTest.IsNull(pointer.Captured);
		CornerstoneTest.Same(recognizer, pointer.CapturedGestureRecognizer);
		CornerstoneTest.AreEqual([initialCapture, target], pointer.PlatformCaptures);
	}

	[PresentationTestMethod]
	public void OnCaptureTransferPointerCaptureLostShouldPropagateUpToTheCommonParent()
	{
		Border initialParent, initialCapture, newParent, newCapture;
		var el = new StackPanel
		{
			Children =
			{
				(initialParent = new Border { Child = initialCapture = new Border() }),
				(newParent = new Border { Child = newCapture = new Border() })
			}
		};
		var receivers = new List<object>();
		var root = new TestRoot(el);
		foreach (InputElement d in root.GetSelfAndVisualDescendants())
		{
			d.PointerCaptureLost += (s, e) => receivers.Add(s);
		}
		var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);

		pointer.Capture(initialCapture);
		pointer.Capture(newCapture);
		CornerstoneTest.IsTrue(receivers.SequenceEqual(new[] { initialCapture, initialParent }));

		receivers.Clear();
		pointer.Capture(null);
		CornerstoneTest.IsTrue(receivers.SequenceEqual(new object[] { newCapture, newParent, el, root }));
	}

	#endregion

	#region Classes

	private sealed class TestGestureRecognizer : GestureRecognizer
	{
		#region Methods

		public void CapturePointer(IPointer pointer)
		{
			Capture(pointer);
		}

		protected override void PointerCaptureLost(IPointer pointer)
		{
		}

		protected override void PointerMoved(PointerEventArgs e)
		{
		}

		protected override void PointerPressed(PointerPressedEventArgs e)
		{
		}

		protected override void PointerReleased(PointerReleasedEventArgs e)
		{
		}

		#endregion
	}

	#endregion
}