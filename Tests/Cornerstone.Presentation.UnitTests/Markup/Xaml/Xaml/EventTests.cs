#nullable enable

#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class EventTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AttachedEventIsAssigned()
	{
		var xaml = @"<Button xmlns='https://github.com/BobbyCannon/Cornerstone' InputElement.Tapped='OnTapped'/>";
		var target = new MyButton();

		CornerstoneRuntimeXamlLoader.Load(xaml, rootInstance: target);

		target.RaiseEvent(new RoutedEventArgs
		{
			RoutedEvent = InputElement.TappedEvent
		});

		CornerstoneTest.IsTrue(target.WasTapped);
	}

	[PresentationTestMethod]
	public void AttachedEventIsAssignedGeneric()
	{
		var xaml = @"<Panel xmlns='https://github.com/BobbyCannon/Cornerstone'><Grid DoubleTapped='OnTapped'><Button Name='target'/></Grid></Panel>";
		var host = new MyPanel();

		CornerstoneRuntimeXamlLoader.Load(xaml, rootInstance: host);

		var target = host.FindControl<Button>("target");

		CornerstoneTest.IsNotNull(target);

		target.RaiseEvent(new TappedEventArgs(InputElement.DoubleTappedEvent, null!));

		CornerstoneTest.IsTrue(host.WasTapped);
	}

	[PresentationTestMethod]
	public void AttachedEventRoutedEventHandler()
	{
		var xaml = @"<Panel xmlns='https://github.com/BobbyCannon/Cornerstone' Button.Click='OnClick'><Button Name='target'/></Panel>";
		var host = new MyPanel();

		CornerstoneRuntimeXamlLoader.Load(xaml, rootInstance: host);

		var target = host.GetControl<Button>("target");
		target.RaiseEvent(new RoutedEventArgs
		{
			RoutedEvent = Button.ClickEvent
		});

		CornerstoneTest.IsTrue(host.WasClicked);
	}

	[PresentationTestMethod]
	public void EventIsAssigned()
	{
		var xaml = @"<Button xmlns='https://github.com/BobbyCannon/Cornerstone' Click='OnClick'/>";
		var target = new MyButton();

		CornerstoneRuntimeXamlLoader.Load(xaml, rootInstance: target);

		target.RaiseEvent(new RoutedEventArgs
		{
			RoutedEvent = Button.ClickEvent
		});

		CornerstoneTest.IsTrue(target.WasClicked);
	}

	[PresentationTestMethod]
	public void ExceptionIsThrownIfEventNotFound()
	{
		var xaml = @"<Button xmlns='https://github.com/BobbyCannon/Cornerstone' Click='NotFound'/>";
		var target = new MyButton();

		XamlTestHelpers.AssertThrowsXamlException(() => CornerstoneRuntimeXamlLoader.Load(xaml, rootInstance: target));
	}

	#endregion

	#region Classes

	public class MyButton : Button
	{
		#region Properties

		public bool WasClicked { get; private set; }
		public bool WasTapped { get; private set; }

		#endregion

		#region Methods

		public void OnClick(object sender, RoutedEventArgs e)
		{
			WasClicked = true;
		}

		public void OnTapped(object sender, RoutedEventArgs e)
		{
			WasTapped = true;
		}

		#endregion
	}

	public class MyPanel : Panel
	{
		#region Properties

		public bool WasClicked { get; private set; }
		public bool WasTapped { get; private set; }

		#endregion

		#region Methods

		public void OnClick(object sender, RoutedEventArgs e)
		{
			WasClicked = true;
		}

		public void OnTapped(object sender, RoutedEventArgs e)
		{
			WasTapped = true;
		}

		#endregion
	}

	#endregion
}