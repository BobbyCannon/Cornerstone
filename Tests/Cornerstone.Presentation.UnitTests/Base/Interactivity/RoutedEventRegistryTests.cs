#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Interactivity;

[TestClass]
public class RoutedEventRegistryTests
{
	#region Methods

	[PresentationTestMethod]
	public void ClickEventShouldBeRegisteredOnButton()
	{
		var expectedEvents = new List<RoutedEvent> { Button.ClickEvent };
		var registeredEvents = RoutedEventRegistry.Instance.GetRegistered<Button>();
		CornerstoneTest.Contains(registeredEvents, expectedEvents.Contains);
	}

	[PresentationTestMethod]
	public void ClickEventShouldNotBeRegisteredOnContentControl()
	{
		// force ContentControl type to be loaded
		new ContentControl();
		var expectedEvents = new List<RoutedEvent> { Button.ClickEvent };
		var registeredEvents = RoutedEventRegistry.Instance.GetRegistered<ContentControl>();
		CornerstoneTest.DoesNotContain(registeredEvents, expectedEvents.Contains);
	}

	[PresentationTestMethod]
	public void InputElementEventsShouldNotBeRegisteredOnButton()
	{
		// force Button type to be loaded
		new Button();
		var expectedEvents = new List<RoutedEvent> { InputElement.PointerPressedEvent, InputElement.PointerReleasedEvent };
		var registeredEvents = RoutedEventRegistry.Instance.GetRegistered<Button>();
		CornerstoneTest.DoesNotContain(registeredEvents, expectedEvents.Contains);
	}

	[PresentationTestMethod]
	public void PointerEventsShouldBeRegistered()
	{
		var expectedEvents = new List<RoutedEvent> { InputElement.PointerPressedEvent, InputElement.PointerReleasedEvent };
		var registeredEvents = RoutedEventRegistry.Instance.GetRegistered<InputElement>();
		CornerstoneTest.Contains(registeredEvents, expectedEvents.Contains);
	}

	#endregion
}