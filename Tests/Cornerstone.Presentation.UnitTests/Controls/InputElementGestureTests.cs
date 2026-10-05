#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class InputElementGestureTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void SwipeGestureEndedPublicEventCanBeObserved()
	{
		var target = new Border();
		SwipeGestureEndedEventArgs received = null;

		target.SwipeGestureEnded += (_, e) => received = e;

		var args = new SwipeGestureEndedEventArgs(42, new Vector(12, 34));
		target.RaiseEvent(args);

		CornerstoneTest.Same(args, received);
		CornerstoneTest.AreEqual(InputElement.SwipeGestureEndedEvent, args.RoutedEvent);
	}

	#endregion
}