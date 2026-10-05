#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.UnitTests.Controls.Utils;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class SplitButtonTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldNotFireClickEventOnSpaceKeyWhenItIsNotFocus()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var raised = 0;
			var target = new TextBox();
			var button = new SplitButton
			{
				Content = target
			};

			var window = new Window { Content = button };
			window.Show();

			button.Click += (s, e) => ++raised;
			target.Focus();
			target.RaiseEvent(CreateKeyDownEvent(Key.Space));
			target.RaiseEvent(CreateKeyUpEvent(Key.Space));
			CornerstoneTest.AreEqual(0, raised);
		}
	}

	[PresentationTestMethod]
	public void SplitButtonCommandParameterDoesNotChangeWhileExecution()
	{
		var target = new SplitButton();
		object lastParamenter = "A";
		var generator = new Random();
		var command = new TestCommand(parameter =>
			{
				target.CommandParameter = generator.Next();
				lastParamenter = parameter;
				return true;
			},
			parameter => { CornerstoneTest.AreEqual(lastParamenter, parameter); });
		target.CommandParameter = lastParamenter;
		target.Command = command;
		var root = new TestRoot { Child = target };

		(target as IClickableControl).RaiseClick();
	}

	private static KeyEventArgs CreateKeyDownEvent(Key key, Interactive source = null)
	{
		return new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, Source = source };
	}

	private static KeyEventArgs CreateKeyUpEvent(Key key, Interactive source = null)
	{
		return new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = key, Source = source };
	}

	#endregion
}