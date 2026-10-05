#region References

using System;
using System.Threading;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Animation;

[TestClass]
public class AnimatableTests
{
	#region Methods

	[PresentationTestMethod]
	public void AnimationIsCancelledWhenNewStyleActivates()
	{
		using (Start())
		{
			var target = CreateTarget();
			var control = CreateStyledControl(target);
			var sub = new StubDisposable();
			target.ApplyResult = sub;

			control.Opacity = 0.5;

			target.Calls.VerifyCalled("Apply", 1);
			target.Calls.VerifyLastPrefix("Apply", control);

			control.Classes.Add("foo");

			sub.Calls.VerifyCalled("Dispose");
		}
	}

	[PresentationTestMethod]
	public void AnimationIsCancelledWhenTransitionRemoved()
	{
		using var app = Start();
		var target = CreateTarget();
		var control = CreateControl(target);
		var sub = new StubDisposable();
		target.ApplyResult = sub;

		control.Opacity = 0.5;
		CornerstoneTest.IsNotNull(control.Transitions);

		control.Transitions.RemoveAt(0);

		sub.Calls.VerifyCalled("Dispose");
	}

	[PresentationTestMethod]
	public void ChangingPlaybackDirectionKeepsPlaybackTimeConstant()
	{
		using (Start())
		{
			var keyframe1 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 0d) },
				KeyTime = TimeSpan.FromSeconds(0)
			};

			var keyframe2 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 1d) },
				KeyTime = TimeSpan.FromSeconds(1)
			};

			var animation = new Presentation.Animation.Animation
			{
				Duration = TimeSpan.FromSeconds(1),
				Children = { keyframe1, keyframe2 },
				PlaybackDirection = PlaybackDirection.Normal
			};

			Border target;
			var clock = new TestClock();
			var root = new TestRoot
			{
				Clock = clock,
				Styles = { new Style(x => x.OfType<Border>()) { Animations = { animation } } },
				Child = target = new Border { Background = Brushes.Red }
			};

			root.Measure(Size.Infinity);
			root.Arrange(new Rect(root.DesiredSize));

			clock.Step(TimeSpan.FromSeconds(0));

			clock.Step(TimeSpan.FromSeconds(0.25));
			CornerstoneTest.AreEqual(0.25, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(0.3));
			CornerstoneTest.AreEqual(0.3, target.Opacity);
			animation.PlaybackDirection = PlaybackDirection.Reverse;
			clock.Step(TimeSpan.FromSeconds(0.3));
			CornerstoneTest.AreEqual(0.3, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(0.35));
			CornerstoneTest.AreEqual(0.25, target.Opacity);
		}
	}

	[PresentationTestMethod]
	public void ChangingSpeedRatioEveryFrameShouldNotLosePlaybackTime()
	{
		using (Start())
		{
			var keyframe1 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 0d) }, KeyTime = TimeSpan.FromSeconds(0)
			};

			var keyframe2 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 1d) }, KeyTime = TimeSpan.FromSeconds(10)
			};

			var animation = new Presentation.Animation.Animation
			{
				Duration = TimeSpan.FromSeconds(10),
				IterationCount = IterationCount.Infinite,
				Children = { keyframe1, keyframe2 }
			};

			Border target;
			var clock = new TestClock();
			var root = new TestRoot
			{
				Clock = clock,
				Styles = { new Style(x => x.OfType<Border>()) { Animations = { animation } } },
				Child = target = new Border { Background = Brushes.Red }
			};

			root.Measure(Size.Infinity);
			root.Arrange(new Rect(root.DesiredSize));

			clock.Step(TimeSpan.FromSeconds(0));

			// 60 frames of 1/60th of a second: one second of playback out of a ten second
			// animation. SpeedRatio stays effectively 1 but is written on every frame, as it
			// would be when bound to a slider being dragged. Re-anchoring playback time on
			// each change must not accumulate rounding errors.
			var frame = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
			var time = TimeSpan.Zero;

			for (var i = 0; i < 60; i++)
			{
				time += frame;
				animation.SpeedRatio = 1d + (i * 1e-9);
				clock.Step(time);
			}

			CornerstoneTest.AreEqual(0.1, target.Opacity, 0.001);
		}
	}

	[PresentationTestMethod]
	public void ChangingSpeedRatioKeepsPlaybackTimeConstant()
	{
		using (Start())
		{
			var keyframe1 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 0d) },
				KeyTime = TimeSpan.FromSeconds(0)
			};

			var keyframe2 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 1d) },
				KeyTime = TimeSpan.FromSeconds(1)
			};

			var animation = new Presentation.Animation.Animation
			{
				Duration = TimeSpan.FromSeconds(1),
				Children = { keyframe1, keyframe2 }
			};

			Border target;
			var clock = new TestClock();
			var root = new TestRoot
			{
				Clock = clock,
				Styles = { new Style(x => x.OfType<Border>()) { Animations = { animation } } },
				Child = target = new Border { Background = Brushes.Red }
			};

			root.Measure(Size.Infinity);
			root.Arrange(new Rect(root.DesiredSize));

			clock.Step(TimeSpan.FromSeconds(0));
			clock.Step(TimeSpan.FromSeconds(0.25));

			animation.SpeedRatio = 2;
			CornerstoneTest.AreEqual(0.25, target.Opacity);
			clock.Step(TimeSpan.FromSeconds(0.25));
			CornerstoneTest.AreEqual(0.25, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(0.25 + 0.125));
			CornerstoneTest.AreEqual(0.5, target.Opacity);

			animation.SpeedRatio = 0.5;
			CornerstoneTest.AreEqual(0.5, target.Opacity);
			clock.Step(TimeSpan.FromSeconds(0.25 + 0.125));
			CornerstoneTest.AreEqual(0.5, target.Opacity);
			clock.Step(TimeSpan.FromSeconds(0.25 + 0.125 + 0.5));
			CornerstoneTest.AreEqual(0.75, target.Opacity);

			animation.SpeedRatio = 1;
			CornerstoneTest.AreEqual(0.75, target.Opacity);
			clock.Step(TimeSpan.FromSeconds(0.25 + 0.125 + 0.5));
			CornerstoneTest.AreEqual(0.75, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(0.25 + 0.125 + 0.5 + 0.25));
			CornerstoneTest.AreEqual(1, target.Opacity);
		}
	}

	[PresentationTestMethod]
	public void DelayBetweenIterationsBehindInitialPointIsInFrontOfIterations()
	{
		using (Start())
		{
			var keyframe1 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 0d) },
				KeyTime = TimeSpan.FromSeconds(0)
			};

			var keyframe2 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 1d) },
				KeyTime = TimeSpan.FromSeconds(1)
			};

			var animation = new Presentation.Animation.Animation
			{
				Duration = TimeSpan.FromSeconds(1),
				DelayBetweenIterations = TimeSpan.FromSeconds(0.5),
				Children = { keyframe1, keyframe2 },
				IterationCount = IterationCount.Infinite,
				PlaybackDirection = PlaybackDirection.Normal
			};

			Border target;
			var clock = new TestClock();
			var root = new TestRoot
			{
				Clock = clock,
				Styles = { new Style(x => x.OfType<Border>()) { Animations = { animation } } },
				Child = target = new Border { Background = Brushes.Red }
			};

			root.Measure(Size.Infinity);
			root.Arrange(new Rect(root.DesiredSize));

			clock.Step(TimeSpan.FromSeconds(0));
			CornerstoneTest.AreEqual(0, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(0.1));
			CornerstoneTest.AreEqual(0.1, target.Opacity);
			animation.PlaybackDirection = PlaybackDirection.Reverse;
			clock.Step(TimeSpan.FromSeconds(0.1));
			CornerstoneTest.AreEqual(0.1, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(0.2));
			CornerstoneTest.AreEqual(0, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(0.2 + 0.1));
			CornerstoneTest.AreEqual(0.9, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(0.2 + 0.9));
			CornerstoneTest.AreEqual(0.1, target.Opacity, 0.000001);

			clock.Step(TimeSpan.FromSeconds(0.2 + 1));
			CornerstoneTest.AreEqual(0, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(0.2 + 1 + 0.25));
			CornerstoneTest.AreEqual(0, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(0.2 + 1 + 0.499));
			CornerstoneTest.AreEqual(0, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(0.2 + 1 + 0.5));
			CornerstoneTest.AreEqual(1, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(0.2 + 1 + 0.5 + 0.1));
			CornerstoneTest.AreEqual(0.9, target.Opacity);
		}
	}

	[PresentationTestMethod]
	[DataRow(null)] //null value
	[DataRow("stringValue")] //string value
	public void InvalidValuesInAnimationShouldNotCrashAnimations(object invalidValue)
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 1d) }, KeyTime = TimeSpan.FromSeconds(0)
		};

		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 2d) }, KeyTime = TimeSpan.FromSeconds(2)
		};

		var keyframe3 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, invalidValue) },
			KeyTime = TimeSpan.FromSeconds(3)
		};

		var animation = new Presentation.Animation.Animation
		{
			Duration = TimeSpan.FromSeconds(3),
			Children = { keyframe1, keyframe2, keyframe3 },
			IterationCount = new IterationCount(5),
			PlaybackDirection = PlaybackDirection.Alternate
		};

		var rect = new Rectangle { Width = 11 };

		var clock = new TestClock();
		animation.RunAsync(rect, clock, CancellationToken.None);

		clock.Step(TimeSpan.Zero);
		CornerstoneTest.AreEqual(1, rect.Width);
		clock.Step(TimeSpan.FromSeconds(2));
		CornerstoneTest.AreEqual(2, rect.Width);
		clock.Step(TimeSpan.FromSeconds(3));

		//here we have invalid value so value should be expected and set to initial original value
		CornerstoneTest.AreEqual(11, rect.Width);
	}

	[PresentationTestMethod]
	public void NewTransitionIsAppliedWhenLocalValueChanges()
	{
		using var app = Start();
		var target = CreateTarget();
		var control = CreateControl(target);

		target.Property = Visual.OpacityProperty;
		target.ApplyHandler = (_, _, _, _) =>
			control.SetValue(Visual.OpacityProperty, 0.9, BindingPriority.Animation);

		control.Opacity = 0.5;

		CornerstoneTest.AreEqual(0.9, control.Opacity);
		target.Calls.Clear();

		control.Opacity = 0.4;

		target.Calls.VerifyCalled("Apply", 1);
		target.Calls.VerifyLastPrefix("Apply", control);
	}

	[PresentationTestMethod]
	public void ReplacingTransitionsDuringAnimationDoesNotThrowKeyNotFound()
	{
		// Issue #4059
		using (Start())
		{
			Border target;
			var clock = new TestClock();
			var root = new TestRoot
			{
				Clock = clock,
				Styles =
				{
					new Style(x => x.OfType<Border>())
					{
						Setters =
						{
							new Setter(Animatable.TransitionsProperty,
								new Transitions
								{
									new DoubleTransition
									{
										Property = Visual.OpacityProperty,
										Duration = TimeSpan.FromSeconds(1)
									}
								})
						}
					},
					new Style(x => x.OfType<Border>().Class("foo"))
					{
						Setters =
						{
							new Setter(Animatable.TransitionsProperty,
								new Transitions
								{
									new DoubleTransition
									{
										Property = Visual.OpacityProperty,
										Duration = TimeSpan.FromSeconds(1)
									}
								}),
							new Setter(Visual.OpacityProperty, 0.0)
						}
					}
				},
				Child = target = new Border { Background = Brushes.Red }
			};

			root.Measure(Size.Infinity);
			root.Arrange(new Rect(root.DesiredSize));

			target.Classes.Add("foo");
			clock.Step(TimeSpan.FromSeconds(0));
			clock.Step(TimeSpan.FromSeconds(0.5));

			CornerstoneTest.AreEqual(0.5, target.Opacity);

			target.Classes.Remove("foo");
		}
	}

	[PresentationTestMethod]
	public void ReversingDirectionPastInitialPointClampsToInitialPoint()
	{
		using (Start())
		{
			var keyframe1 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 0d) },
				KeyTime = TimeSpan.FromSeconds(0)
			};

			var keyframe2 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 1d) },
				KeyTime = TimeSpan.FromSeconds(1)
			};

			var animation = new Presentation.Animation.Animation
			{
				Duration = TimeSpan.FromSeconds(1),
				Delay = TimeSpan.FromSeconds(0.5),
				Children = { keyframe1, keyframe2 },
				PlaybackDirection = PlaybackDirection.Normal
			};

			Border target;
			var clock = new TestClock();
			var root = new TestRoot
			{
				Clock = clock,
				Styles = { new Style(x => x.OfType<Border>()) { Animations = { animation } } },
				Child = target = new Border { Background = Brushes.Red }
			};

			root.Measure(Size.Infinity);
			root.Arrange(new Rect(root.DesiredSize));

			clock.Step(TimeSpan.FromSeconds(0));
			CornerstoneTest.AreEqual(1, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(0.5));
			CornerstoneTest.AreEqual(1, target.Opacity);
			clock.Step(TimeSpan.FromSeconds(1));
			CornerstoneTest.AreEqual(0.5, target.Opacity);
			animation.PlaybackDirection = PlaybackDirection.Reverse;
			clock.Step(TimeSpan.FromSeconds(1));
			CornerstoneTest.AreEqual(0.5, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(1.2));
			CornerstoneTest.AreEqual(0.3, target.Opacity);
			clock.Step(TimeSpan.FromSeconds(1.5));
			CornerstoneTest.AreEqual(0, target.Opacity);
			clock.Step(TimeSpan.FromSeconds(2));
			CornerstoneTest.AreEqual(0, target.Opacity);
			clock.Step(TimeSpan.FromSeconds(3));
			CornerstoneTest.AreEqual(0, target.Opacity);
			animation.PlaybackDirection = PlaybackDirection.Normal;
			clock.Step(TimeSpan.FromSeconds(3));
			CornerstoneTest.AreEqual(0, target.Opacity);
			clock.Step(TimeSpan.FromSeconds(3.5));
			CornerstoneTest.AreEqual(0, target.Opacity);
			clock.Step(TimeSpan.FromSeconds(4));
			CornerstoneTest.AreEqual(0.5, target.Opacity);
			clock.Step(TimeSpan.FromSeconds(4.5));
			CornerstoneTest.AreEqual(1, target.Opacity);
		}
	}

	[PresentationTestMethod]
	public void ReversingDirectionPastInitialPointParksAnimationUntilResumed()
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Visual.OpacityProperty, 0d) }, KeyTime = TimeSpan.FromSeconds(0)
		};

		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Visual.OpacityProperty, 1d) }, KeyTime = TimeSpan.FromSeconds(1)
		};

		var animation = new Presentation.Animation.Animation
		{
			Duration = TimeSpan.FromSeconds(1),
			Children = { keyframe1, keyframe2 }
		};

		var target = new Border { Background = Brushes.Red };

		var clock = new TestClock();
		var animationRun = animation.RunAsync(target, clock, CancellationToken.None);

		clock.Step(TimeSpan.FromSeconds(0));
		clock.Step(TimeSpan.FromSeconds(0.5));
		CornerstoneTest.AreEqual(0.5, target.Opacity);

		// Reverse the animation so that it runs back to its initial point, where a limited
		// number of iterations clamps it. The animation is parked rather than finished: it
		// stays subscribed, and so RunAsync stays pending, because playback can still be
		// resumed by changing the direction back.
		animation.PlaybackDirection = PlaybackDirection.Reverse;
		clock.Step(TimeSpan.FromSeconds(0.5));
		clock.Step(TimeSpan.FromSeconds(1));
		clock.Step(TimeSpan.FromSeconds(10));

		CornerstoneTest.AreEqual(0, target.Opacity);
		CornerstoneTest.IsFalse(animationRun.IsCompleted);

		// Resuming from the parked state runs the animation to its end and completes it.
		animation.PlaybackDirection = PlaybackDirection.Normal;
		clock.Step(TimeSpan.FromSeconds(10));
		clock.Step(TimeSpan.FromSeconds(10.5));
		CornerstoneTest.AreEqual(0.5, target.Opacity);

		clock.Step(TimeSpan.FromSeconds(11));
		CornerstoneTest.AreEqual(1, target.Opacity);
		CornerstoneTest.IsTrue(animationRun.IsCompleted);
	}

	[PresentationTestMethod]
	public void ReversingInfiniteAnimationPastInitialPointReplaysPreviousIterations()
	{
		using (Start())
		{
			var keyframe1 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 0d) }, KeyTime = TimeSpan.FromSeconds(0)
			};

			var keyframe2 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 1d) }, KeyTime = TimeSpan.FromSeconds(1)
			};

			var animation = new Presentation.Animation.Animation
			{
				Duration = TimeSpan.FromSeconds(1),
				Delay = TimeSpan.FromSeconds(0.5),
				IterationCount = IterationCount.Infinite,
				FillMode = FillMode.Both,
				Children = { keyframe1, keyframe2 }
			};

			Border target;
			var clock = new TestClock();
			var root = new TestRoot
			{
				Clock = clock,
				Styles = { new Style(x => x.OfType<Border>()) { Animations = { animation } } },
				Child = target = new Border { Background = Brushes.Red }
			};

			root.Measure(Size.Infinity);
			root.Arrange(new Rect(root.DesiredSize));

			clock.Step(TimeSpan.FromSeconds(0));
			CornerstoneTest.AreEqual(0, target.Opacity);

			// Initial delay, then half of the first iteration.
			clock.Step(TimeSpan.FromSeconds(1));
			CornerstoneTest.AreEqual(0.5, target.Opacity);

			animation.PlaybackDirection = PlaybackDirection.Reverse;
			clock.Step(TimeSpan.FromSeconds(1));
			CornerstoneTest.AreEqual(0.5, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(1.25));
			CornerstoneTest.AreEqual(0.25, target.Opacity);

			// Back at the initial point.
			clock.Step(TimeSpan.FromSeconds(1.5));
			CornerstoneTest.AreEqual(0, target.Opacity);

			// The initial delay is mirrored around the initial point, so crossing it while
			// reversed takes twice the Delay. Note that a finite animation behaves
			// differently: it clamps at the initial point instead, and only ever spends
			// Delay there. See Reversing_Direction_Past_Initial_Point_Clamps_To_Initial_Point.
			clock.Step(TimeSpan.FromSeconds(2));
			CornerstoneTest.AreEqual(0, target.Opacity);
			clock.Step(TimeSpan.FromSeconds(2.5));
			CornerstoneTest.AreEqual(0, target.Opacity);

			// Past the delay, the iteration before the initial one plays backwards.
			clock.Step(TimeSpan.FromSeconds(2.75));
			CornerstoneTest.AreEqual(0.75, target.Opacity);
			clock.Step(TimeSpan.FromSeconds(3));
			CornerstoneTest.AreEqual(0.5, target.Opacity);
			clock.Step(TimeSpan.FromSeconds(3.5));
			CornerstoneTest.AreEqual(1, target.Opacity);
		}
	}

	[PresentationTestMethod]
	public void RunNormalUseCaseAnimation()
	{
		using (Start())
		{
			var keyframe1 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 1d) }, KeyTime = TimeSpan.FromSeconds(0)
			};

			var keyframe2 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 0.5d) }, KeyTime = TimeSpan.FromSeconds(1)
			};

			var animation = new Presentation.Animation.Animation
			{
				Duration = TimeSpan.FromSeconds(10), Children = { keyframe1, keyframe2 }
			};

			Border target;
			var clock = new TestClock();
			var root = new TestRoot
			{
				Clock = clock,
				Styles = { new Style(x => x.OfType<Border>()) { Animations = { animation } } },
				Child = target = new Border { Background = Brushes.Red }
			};

			root.Measure(Size.Infinity);
			root.Arrange(new Rect(root.DesiredSize));

			clock.Step(TimeSpan.FromSeconds(0));
			clock.Step(TimeSpan.FromSeconds(0.99));

			CornerstoneTest.InRange(target.Opacity, 0.5d, 0.51d);
		}
	}

	[PresentationTestMethod]
	public void RunNormalUseCaseAnimationWithInfiniteIteration()
	{
		using (Start())
		{
			var keyframe1 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 0d) }, KeyTime = TimeSpan.FromSeconds(0)
			};

			var keyframe2 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 1d) }, KeyTime = TimeSpan.FromSeconds(1)
			};

			var animation = new Presentation.Animation.Animation
			{
				Duration = TimeSpan.FromSeconds(1),
				IterationCount = IterationCount.Infinite,
				Children = { keyframe1, keyframe2 }
			};

			Border target;
			var clock = new TestClock();
			var root = new TestRoot
			{
				Clock = clock,
				Styles = { new Style(x => x.OfType<Border>()) { Animations = { animation } } },
				Child = target = new Border { Background = Brushes.Red }
			};

			root.Measure(Size.Infinity);
			root.Arrange(new Rect(root.DesiredSize));

			clock.Step(TimeSpan.FromSeconds(0));

			clock.Step(TimeSpan.FromSeconds(0.5));
			CornerstoneTest.AreEqual(0.5, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(1));
			CornerstoneTest.AreEqual(0, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(1.5));
			CornerstoneTest.AreEqual(0.5, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(2));
			CornerstoneTest.AreEqual(0, target.Opacity);
		}
	}

	[PresentationTestMethod]
	public void SettingDurationToZeroWithDelayBetweenIterationsShouldFinishAnimation()
	{
		using (Start())
		{
			var keyframe1 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 0d) }, KeyTime = TimeSpan.FromSeconds(0)
			};

			var keyframe2 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 1d) }, KeyTime = TimeSpan.FromSeconds(1)
			};

			var animation = new Presentation.Animation.Animation
			{
				Duration = TimeSpan.FromSeconds(1),
				DelayBetweenIterations = TimeSpan.FromSeconds(0.5),
				IterationCount = IterationCount.Infinite,
				Children = { keyframe1, keyframe2 }
			};

			Border target;
			var clock = new TestClock();
			var root = new TestRoot
			{
				Clock = clock,
				Styles = { new Style(x => x.OfType<Border>()) { Animations = { animation } } },
				Child = target = new Border { Background = Brushes.Red }
			};

			root.Measure(Size.Infinity);
			root.Arrange(new Rect(root.DesiredSize));

			clock.Step(TimeSpan.FromSeconds(0));
			clock.Step(TimeSpan.FromSeconds(0.5));
			CornerstoneTest.AreEqual(0.5, target.Opacity);

			animation.Duration = TimeSpan.Zero;

			clock.Step(TimeSpan.FromSeconds(1));
			CornerstoneTest.IsFalse(double.IsNaN(target.Opacity));
			CornerstoneTest.AreEqual(1, target.Opacity);
			CornerstoneTest.IsFalse(target.IsAnimating(Visual.OpacityProperty));
		}
	}

	[PresentationTestMethod]
	public void TransitionFromStyleTriggerIsApplied()
	{
		using (Start())
		{
			var target = CreateTransition(Layoutable.WidthProperty);
			var control = CreateStyledControl(transition2: target);

			control.Classes.Add("foo");
			control.Width = 100;

			target.Calls.VerifyCalled("Apply", 1);
			target.Calls.VerifyLastPrefix("Apply", control);
		}
	}

	[PresentationTestMethod]
	public void TransitionIsAppliedWhenLocalValueChanges()
	{
		using var app = Start();
		var target = CreateTarget();
		var control = CreateControl(target);

		control.Opacity = 0.5;

		target.Calls.VerifyCalled("Apply", 1);
		target.Calls.VerifyLastPrefix("Apply", control);
	}

	[PresentationTestMethod]
	public void TransitionIsDisposedWhenLocalValueChanges()
	{
		using var app = Start();
		var target = CreateTarget();
		var control = CreateControl(target);
		var sub = new StubDisposable();
		target.ApplyResult = sub;

		control.Opacity = 0.5;
		sub.Calls.Clear();
		control.Opacity = 0.4;

		sub.Calls.VerifyCalled("Dispose");
	}

	[PresentationTestMethod]
	public void TransitionIsNotAppliedToInitialStyle()
	{
		using (Start())
		{
			var target = CreateTarget();
			var control = new Control { Transitions = new Transitions { target } };

			var root = new TestRoot
			{
				Styles =
				{
					new Style(x => x.OfType<Control>())
					{
						Setters = { new Setter(Visual.OpacityProperty, 0.8) }
					}
				}
			};

			root.Child = control;

			CornerstoneTest.AreEqual(0.8, control.Opacity);

			target.Calls.VerifyNotCalled("Apply");
		}
	}

	[PresentationTestMethod]
	public void TransitionIsNotAppliedWhenAnimatedValueChanges()
	{
		var target = CreateTarget();
		var control = CreateControl(target);

		control.SetValue(Visual.OpacityProperty, 0.5, BindingPriority.Animation);

		target.Calls.VerifyNotCalled("Apply");
	}

	[PresentationTestMethod]
	public void TransitionIsNotAppliedWhenNotAttachedToVisualTree()
	{
		var target = CreateTarget();
		var control = new Control { Transitions = new Transitions { target } };

		control.Opacity = 0.5;

		target.Calls.VerifyNotCalled("Apply");
	}

	[PresentationTestMethod]
	public void TransitionIsNotAppliedWhenRemovedFromVisualTree()
	{
		using var app = Start();
		var target = CreateTarget();
		var control = CreateControl(target);

		control.Opacity = 0.5;

		target.Calls.VerifyCalled("Apply", 1);
		target.Calls.Clear();

		var root = (TestRoot) control.Parent;
		CornerstoneTest.IsNotNull(root);
		root.Child = null;
		control.Opacity = 0.8;

		target.Calls.VerifyNotCalled("Apply");
	}

	[PresentationTestMethod]
	public void TransitionIsNotAppliedWhenStyleTriggerChangesWithLocalValuePresent()
	{
		using var app = Start();
		var target = CreateTarget();
		var control = CreateControl(target);

		control.SetValue(Visual.OpacityProperty, 0.5);

		target.Calls.VerifyCalled("Apply", 1);
		target.Calls.Clear();

		control.SetValue(Visual.OpacityProperty, 0.8, BindingPriority.StyleTrigger);

		target.Calls.VerifyNotCalled("Apply");
	}

	[PresentationTestMethod]
	public void TransitionsCanBeChangedToCollectionThatContainsTheSameTransitions()
	{
		var target = CreateTarget();
		var control = CreateControl(target);

		control.Transitions = new Transitions { target };
	}

	[PresentationTestMethod]
	public void TransitionsCanBeRemovedWhileTransitionInProgress()
	{
		using var app = Start();

		var opacityTransition = new DoubleTransition
		{
			Property = Visual.OpacityProperty, Duration = TimeSpan.FromSeconds(1)
		};

		var transitions = new Transitions { opacityTransition };
		var borderTheme = new ControlTheme(typeof(Border))
		{
			Setters = { new Setter(Animatable.TransitionsProperty, transitions) }
		};

		var clock = new TestClock();
		var root = new TestRoot { Clock = clock, Resources = { { typeof(Border), borderTheme } } };

		var border = new Border();
		root.Child = border;

		root.LayoutManager.ExecuteInitialLayoutPass();

		CornerstoneTest.Same(transitions, border.Transitions);

		// First set property with a transition to a new value, and step the clock until
		// transition is complete.
		border.Opacity = 0;
		clock.Step(TimeSpan.FromSeconds(0));
		clock.Step(TimeSpan.FromSeconds(1));
		CornerstoneTest.AreEqual(0, border.Opacity);

		// Now clear the property; a transition is now in progress but no local value is
		// set.
		border.ClearValue(Visual.OpacityProperty);

		// Remove the transition by removing the control from the logical tree. This was
		// causing an exception.
		root.Child = null;
	}

	[PresentationTestMethod]
	public void TransitionsCanReSetDuringStyling()
	{
		var target = CreateTarget();
		var control = CreateControl(target);

		// Assigning and then clearing Transitions ensures we have a transition state
		// collection created.
		control.ClearValue(Animatable.TransitionsProperty);

		control.GetValueStore().BeginStyling();

		// Setting opacity then Transitions means that we receive the Transitions change
		// after the Opacity change when EndStyling is called.
		var style = new Style
		{
			Setters =
			{
				new Setter(Visual.OpacityProperty, 0.5),
				new Setter(Animatable.TransitionsProperty, new Transitions { target })
			}
		};

		StyleHelpers.TryAttach(style, control);

		// Which means that the transition state hasn't been initialized with the new
		// Transitions when the Opacity change notification gets raised here.
		control.GetValueStore().EndStyling();
	}

	[PresentationTestMethod]
	public void ZeroDurationShouldFinishAnimation()
	{
		using (Start())
		{
			var keyframe1 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 1d) }, KeyTime = TimeSpan.FromSeconds(0)
			};

			var keyframe2 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 0.5d) }, KeyTime = TimeSpan.FromSeconds(2)
			};

			var animation = new Presentation.Animation.Animation
			{
				Duration = TimeSpan.FromSeconds(2),
				Children = { keyframe1, keyframe2 },
				FillMode = FillMode.Both
			};

			Border target;
			var clock = new TestClock();
			var root = new TestRoot
			{
				Clock = clock,
				Styles = { new Style(x => x.OfType<Border>()) { Animations = { animation } } },
				Child = target = new Border { Background = Brushes.Red }
			};

			root.Measure(Size.Infinity);
			root.Arrange(new Rect(root.DesiredSize));

			clock.Step(TimeSpan.FromSeconds(0));
			clock.Step(TimeSpan.FromSeconds(1));

			CornerstoneTest.IsTrue(target.IsAnimating(Visual.OpacityProperty));

			CornerstoneTest.AreEqual(0.75, target.Opacity);

			// This is not the normal way to access and set the animations
			// object's Duration property to zero that is defined in styles
			// but this is still valid for the RunAsync version.
			animation.Duration = TimeSpan.Zero;

			clock.Step(TimeSpan.FromSeconds(1.2));

			CornerstoneTest.AreEqual(0.5, target.Opacity);
			CornerstoneTest.IsFalse(target.IsAnimating(Visual.OpacityProperty));
		}
	}

	[PresentationTestMethod]
	public void ZeroDurationShouldFinishAnimationWithInfiniteIteration()
	{
		using (Start())
		{
			var keyframe1 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 0d) }, KeyTime = TimeSpan.FromSeconds(0)
			};

			var keyframe2 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 1d) }, KeyTime = TimeSpan.FromSeconds(1)
			};

			var animation = new Presentation.Animation.Animation
			{
				Duration = TimeSpan.FromSeconds(1),
				IterationCount = IterationCount.Infinite,
				Children = { keyframe1, keyframe2 }
			};

			Border target;
			var clock = new TestClock();
			var root = new TestRoot
			{
				Clock = clock,
				Styles = { new Style(x => x.OfType<Border>()) { Animations = { animation } } },
				Child = target = new Border { Background = Brushes.Red }
			};

			root.Measure(Size.Infinity);
			root.Arrange(new Rect(root.DesiredSize));

			clock.Step(TimeSpan.FromSeconds(0));
			CornerstoneTest.IsTrue(target.IsAnimating(Visual.OpacityProperty));

			clock.Step(TimeSpan.FromSeconds(0.5));
			CornerstoneTest.AreEqual(0.5, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(1));
			CornerstoneTest.AreEqual(0, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(1.5));
			CornerstoneTest.AreEqual(0.5, target.Opacity);

			clock.Step(TimeSpan.FromSeconds(2));
			CornerstoneTest.AreEqual(0, target.Opacity);

			// This is not the normal way to access and set the animations
			// object's Duration property to zero that is defined in styles
			// but this is still valid for the RunAsync version.
			animation.Duration = TimeSpan.Zero;
			clock.Step(TimeSpan.FromSeconds(1.2));
			CornerstoneTest.AreEqual(1, target.Opacity);
			CornerstoneTest.IsFalse(target.IsAnimating(Visual.OpacityProperty));
		}
	}

	[PresentationTestMethod]
	public void ZeroDurationWithDelayBetweenIterationsShouldFinishAnimation()
	{
		using (Start())
		{
			var keyframe1 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 0d) }, Cue = new Cue(0d)
			};

			var keyframe2 = new KeyFrame
			{
				Setters = { new Setter(Visual.OpacityProperty, 1d) }, Cue = new Cue(1d)
			};

			var animation = new Presentation.Animation.Animation
			{
				Duration = TimeSpan.Zero,
				DelayBetweenIterations = TimeSpan.FromSeconds(0.5),
				IterationCount = IterationCount.Infinite,
				Children = { keyframe1, keyframe2 }
			};

			Border target;
			var clock = new TestClock();
			var root = new TestRoot
			{
				Clock = clock,
				Styles = { new Style(x => x.OfType<Border>()) { Animations = { animation } } },
				Child = target = new Border { Background = Brushes.Red }
			};

			root.Measure(Size.Infinity);
			root.Arrange(new Rect(root.DesiredSize));

			clock.Step(TimeSpan.FromSeconds(0));
			CornerstoneTest.IsFalse(double.IsNaN(target.Opacity));
			CornerstoneTest.AreEqual(1, target.Opacity);
			CornerstoneTest.IsFalse(target.IsAnimating(Visual.OpacityProperty));

			clock.Step(TimeSpan.FromSeconds(0.5));
			CornerstoneTest.IsFalse(double.IsNaN(target.Opacity));
		}
	}

	private static Control CreateControl(ITransition transition)
	{
		var control = new Control { Transitions = new Transitions { transition } };

		var _ = new TestRoot(control);
		return control;
	}

	private static Control CreateStyledControl(
		ITransition transition1 = null,
		ITransition transition2 = null)
	{
		transition1 = transition1 ?? CreateTarget();
		transition2 = transition2 ?? CreateTransition(Layoutable.WidthProperty);

		var control = new Control
		{
			Styles =
			{
				new Style(x => x.OfType<Control>())
				{
					Setters =
					{
						new Setter
						{
							Property = Animatable.TransitionsProperty,
							Value = new Transitions { transition1 }
						}
					}
				},
				new Style(x => x.OfType<Control>().Class("foo"))
				{
					Setters =
					{
						new Setter
						{
							Property = Animatable.TransitionsProperty,
							Value = new Transitions { transition2 }
						}
					}
				}
			}
		};

		var _ = new TestRoot(control);
		return control;
	}

	private static StubTransition CreateTarget()
	{
		return CreateTransition(Visual.OpacityProperty);
	}

	private static StubTransition CreateTransition(PresentationProperty property)
	{
		var target = new StubTransition();
		target.Property = property;
		return target;
	}

	private static IDisposable Start()
	{
		var clock = new MockGlobalClock();
		var services = new TestServices(globalClock: clock);
		return UnitTestApplication.Start(services);
	}

	#endregion
}