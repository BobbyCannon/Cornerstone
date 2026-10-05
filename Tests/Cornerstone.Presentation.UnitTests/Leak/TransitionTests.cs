#region References

using System;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Leak;

[TestClass]
public class TransitionTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void LazilyCreatedControlShouldNotLeakTransitions()
	{
		var clock = new MockGlobalClock();
		using (UnitTestApplication.Start(TestServices.StyledWindow.With(globalClock: clock)))
		{
			var sharedTransitions = new Transitions
			{
				new TransformOperationsTransition
				{
					Property = Visual.RenderTransformProperty, Duration = TimeSpan.FromSeconds(0.750)
				}
			};
			var controlTheme = new ControlTheme(typeof(Button))
			{
				BasedOn = Application.Current?.Resources[typeof(Button)] as ControlTheme,
				Setters = { new Setter(Animatable.TransitionsProperty, sharedTransitions) }
			};

			WeakReference Run()
			{
				var window = new Window();
				window.Show();
				var button = new Button { Theme = controlTheme };
				window.Content = new UserControl
				{
					Content = button,

					// When invisible, Button won't be attached to the visual tree
					IsVisible = false
				};
				window.Content = null;
				window.Close();

				return new WeakReference(button);
			}

			var weakButton = Run();
			CornerstoneTest.IsTrue(weakButton.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakButton.IsAlive);
		}
	}

	[PresentationTestMethod]
	public void SharedTransitionCollectionIsNotLeaking()
	{
		var clock = new MockGlobalClock();
		using (UnitTestApplication.Start(TestServices.StyledWindow.With(globalClock: clock)))
		{
			// Our themes do share transition collections, so we need to test this scenario well.
			var sharedTransitions = new Transitions
			{
				new TransformOperationsTransition
				{
					Property = Visual.RenderTransformProperty, Duration = TimeSpan.FromSeconds(0.750)
				}
			};
			var controlTheme = new ControlTheme(typeof(Button))
			{
				BasedOn = Application.Current?.Resources[typeof(Button)] as ControlTheme,
				Setters = { new Setter(Animatable.TransitionsProperty, sharedTransitions) }
			};

			WeakReference Run()
			{
				var button = new Button { Theme = controlTheme };
				var window = new Window();
				window.Content = button;
				window.Show();
				window.Content = null;
				window.Close();

				return new WeakReference(button);
			}

			var weakButton = Run();
			CornerstoneTest.IsTrue(weakButton.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakButton.IsAlive);
		}
	}

	[PresentationTestMethod]
	public void TransitionOnStyledPropertyIsFreed()
	{
		var clock = new MockGlobalClock();

		using (UnitTestApplication.Start(TestServices.StyledWindow.With(globalClock: clock)))
		{
			WeakReference Run()
			{
				var opacityTransition = new DoubleTransition { Duration = TimeSpan.FromSeconds(1), Property = Border.OpacityProperty };

				var border = new Border { Transitions = new Transitions { opacityTransition } };
				var window = new Window();
				window.Content = border;
				window.Show();

				border.Opacity = 0;

				clock.Pulse(TimeSpan.FromSeconds(0));
				clock.Pulse(TimeSpan.FromSeconds(0.5));

				CornerstoneTest.AreEqual(0.5, border.Opacity);

				var transitionInstance = border.TryGetTransitionInstance(opacityTransition);
				CornerstoneTest.IsNotNull(transitionInstance);

				clock.Pulse(TimeSpan.FromSeconds(1));

				CornerstoneTest.AreEqual(0, border.Opacity);

				window.Close();

				return new WeakReference(transitionInstance);
			}

			var weakTransitionInstance = Run();
			CornerstoneTest.IsTrue(weakTransitionInstance.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakTransitionInstance.IsAlive);
		}
	}

	private static void CollectGarbage()
	{
		// Process all Loaded events to free control reference(s)
		Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
		GC.Collect();
	}

	#endregion
}