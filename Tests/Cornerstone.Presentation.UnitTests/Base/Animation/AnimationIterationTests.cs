#region References

using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Animation.Easings;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Animation;

#region References

using Animation = Presentation.Animation.Animation;

#endregion

[TestClass]
public class AnimationIterationTests
{
	#region Methods

	[PresentationTestMethod]
	public void AnimationCanSetIsVisibleFalseAtEndWithoutPausingItself()
	{
		// An animation that sets IsVisible=false at Cue 1.0 should complete normally.
		// The visibility change at the final keyframe should not cause the animation
		// to pause before it can report completion.
		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(0.3),
			FillMode = FillMode.Forward,
			Children =
			{
				new KeyFrame
				{
					Cue = new Cue(0.0),
					Setters = { new Setter(Visual.IsVisibleProperty, true) }
				},
				new KeyFrame
				{
					Cue = new Cue(1.0),
					Setters = { new Setter(Visual.IsVisibleProperty, false) }
				}
			}
		};

		// Control starts visible (expanded state).
		var border = new Border { IsVisible = true };

		var clock = new TestClock();
		var animationRun = animation.RunAsync(border, clock, CancellationToken.None);

		clock.Step(TimeSpan.Zero);
		CornerstoneTest.IsTrue(border.IsVisible);

		// Step to the end: animation sets IsVisible=false.
		clock.Step(TimeSpan.FromSeconds(0.3));

		// Animation should have completed and the final value should hold.
		CornerstoneTest.IsTrue(animationRun.IsCompleted);
		CornerstoneTest.IsFalse(border.IsVisible);
	}

	[PresentationTestMethod]
	public void AnimationCanSetIsVisibleTrueOnInvisibleControl()
	{
		// Reproduces a bug where an expand animation tries to make a collapsed
		// (invisible) control visible at Cue 0.0, but the animation system pauses
		// animations on invisible controls, creating a deadlock where the animation
		// can't run to set IsVisible=true because the control is already invisible.
		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(0.3),
			FillMode = FillMode.Forward,
			Children =
			{
				new KeyFrame
				{
					Cue = new Cue(0.0),
					Setters = { new Setter(Visual.IsVisibleProperty, true) }
				},
				new KeyFrame
				{
					Cue = new Cue(1.0),
					Setters = { new Setter(Visual.IsVisibleProperty, true) }
				}
			}
		};

		// Control starts invisible (collapsed state).
		var border = new Border { IsVisible = false };

		var clock = new TestClock();
		var animationRun = animation.RunAsync(border, clock, CancellationToken.None);

		// Kick off the animation.
		clock.Step(TimeSpan.Zero);

		// The Cue 0.0 keyframe should have set IsVisible = true,
		// even though the control started invisible.
		CornerstoneTest.IsTrue(border.IsVisible);

		// Animation should progress to completion.
		clock.Step(TimeSpan.FromSeconds(0.3));
		CornerstoneTest.IsTrue(animationRun.IsCompleted);
	}

	[PresentationTestMethod]
	public void AnimationCompletesGracefullyWhenFirstKeyFrameValueIsNull()
	{
		var clock = new MockGlobalClock();
		var services = new TestServices(globalClock: clock);

		using (UnitTestApplication.Start(services))
		{
			var nullBinding = new Binding("NonExistentProperty");

			var animation = new Animation
			{
				Duration = TimeSpan.FromSeconds(1),
				FillMode = FillMode.Both,
				Children =
				{
					new KeyFrame
					{
						KeyTime = TimeSpan.FromSeconds(0),
						Setters = { new Setter(Layoutable.WidthProperty, nullBinding) }
					},
					new KeyFrame
					{
						KeyTime = TimeSpan.FromSeconds(1),
						Setters = { new Setter(Layoutable.WidthProperty, 200d) }
					}
				}
			};

			var border = new Border { Width = 100d, Height = 100d };

			var root = new TestRoot(border);
			root.LayoutManager.ExecuteInitialLayoutPass();

			var animationTask = animation.RunAsync(border, clock, CancellationToken.None);

			// Pulse the clock - this should not throw even though
			// the first keyframe's value is null (falls back to neutral value)
			var exception = Record.Exception(() => clock.Pulse(TimeSpan.Zero));
			CornerstoneTest.IsNull(exception);

			// The animation should continue running (using neutral value as fallback)
			clock.Pulse(TimeSpan.FromSeconds(0.5));
			CornerstoneTest.IsFalse(animationTask.IsCompleted);

			// Animation completes after its full duration
			clock.Pulse(TimeSpan.FromSeconds(1));
			CornerstoneTest.IsTrue(animationTask.IsCompleted);
		}
	}

	[PresentationTestMethod]
	public void AnimationPlaysCorrectlyAfterReattach()
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 200d) }, Cue = new Cue(1d)
		};
		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 100d) }, Cue = new Cue(0d)
		};
		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(5),
			IterationCount = new IterationCount(1),
			FillMode = FillMode.Forward,
			Children = { keyframe2, keyframe1 }
		};

		var border = new Border { Height = 100d, Width = 50d };
		var root = new TestRoot(border);
		var clock = new TestClock();
		var animationRun = animation.RunAsync(border, clock, CancellationToken.None);

		clock.Step(TimeSpan.Zero);
		CornerstoneTest.IsFalse(animationRun.IsCompleted);

		// Detach — animation completes.
		root.Child = null;
		CornerstoneTest.IsTrue(animationRun.IsCompleted);

		// Reattach and start a fresh animation.
		root.Child = border;
		var clock2 = new TestClock();
		var animationRun2 = animation.RunAsync(border, clock2, CancellationToken.None);

		clock2.Step(TimeSpan.Zero);
		CornerstoneTest.IsFalse(animationRun2.IsCompleted);

		clock2.Step(TimeSpan.FromSeconds(5));
		CornerstoneTest.IsTrue(animationRun2.IsCompleted);
		CornerstoneTest.AreEqual(200d, border.Width);
	}

	[PresentationTestMethod]
	public void AnimationWithUnresolvedBindingDoesNotThrowNullReferenceException()
	{
		// Additional test to verify the null reference fix for animator first keyframe value

		var clock = new MockGlobalClock();
		var services = new TestServices(globalClock: clock);

		using (UnitTestApplication.Start(services))
		{
			// Binding to a property that doesn't exist - will evaluate to null
			var binding = new Binding("MissingProperty");

			var animation = new Animation
			{
				Duration = TimeSpan.FromSeconds(1),
				IterationCount = new IterationCount(1),
				Children =
				{
					new KeyFrame
					{
						Cue = new Cue(0d),
						Setters = { new Setter(Layoutable.WidthProperty, binding) }
					},
					new KeyFrame
					{
						Cue = new Cue(1d),
						Setters = { new Setter(Layoutable.WidthProperty, 300d) }
					}
				}
			};

			var control = new Border { Width = 50d };
			var root = new TestRoot(control);
			root.LayoutManager.ExecuteInitialLayoutPass();

			// Start animation - the first keyframe value will be null due to unresolved binding
			var task = animation.RunAsync(control, clock, CancellationToken.None);

			// The fix ensures this doesn't throw NullReferenceException
			// Animation falls back to neutral value and continues
			clock.Pulse(TimeSpan.Zero);
			clock.Pulse(TimeSpan.FromSeconds(0.1));

			// Animation should still be running (uses neutral value as fallback)
			CornerstoneTest.IsFalse(task.IsCompleted);

			// Animation completes after its full duration
			clock.Pulse(TimeSpan.FromSeconds(1));
			CornerstoneTest.IsTrue(task.IsCompleted);
		}
	}

	[PresentationTestMethod]
	public void AutoDoesNotPauseOnInvisibleWhenStartedManually()
	{
		// When started via RunAsync (manual), Auto resolves to Always.
		// The animation should NOT pause when the control becomes invisible.
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 100d) }, Cue = new Cue(0d)
		};
		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 200d) }, Cue = new Cue(1d)
		};

		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(3),
			IterationCount = new IterationCount(1),
			Easing = new LinearEasing(),
			FillMode = FillMode.Forward,
			Children = { keyframe1, keyframe2 }
		};

		var border = new Border { Height = 100d, Width = 50d };

		var clock = new TestClock();
		var animationRun = animation.RunAsync(border, clock, CancellationToken.None);

		clock.Step(TimeSpan.Zero);
		CornerstoneTest.AreEqual(100d, border.Width);

		// Hide the control, animation should keep running under Auto + manual.
		border.IsVisible = false;

		// Width should advance while invisible (not paused).
		clock.Step(TimeSpan.FromSeconds(1.5));
		CornerstoneTest.AreEqual(150d, border.Width);
		CornerstoneTest.IsFalse(animationRun.IsCompleted);

		clock.Step(TimeSpan.FromSeconds(3));
		CornerstoneTest.IsTrue(animationRun.IsCompleted);
		CornerstoneTest.AreEqual(200d, border.Width);
	}

	[PresentationTestMethod]
	public void AutoPausesOnInvisibleWhenStartedFromStyle()
	{
		// When started via Apply (the style path), Auto resolves to OnlyIfVisible.
		// The animation should pause when the control becomes invisible.
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 100d) }, Cue = new Cue(0d)
		};
		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 200d) }, Cue = new Cue(1d)
		};

		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(3),
			IterationCount = new IterationCount(1),
			Children = { keyframe1, keyframe2 }
		};

		var border = new Border { Height = 100d, Width = 50d };

		var clock = new TestClock();
		var completed = false;

		// Apply (not RunAsync), this is the style-applied path.
		var disposable = animation.Apply(border, clock, Observable.Return(true), () => completed = true);

		clock.Step(TimeSpan.Zero);
		CornerstoneTest.AreEqual(100d, border.Width);

		// Hide the control, animation should pause under Auto.
		border.IsVisible = false;

		clock.Step(TimeSpan.FromSeconds(1.5));

		// Width should not have advanced while invisible.
		CornerstoneTest.AreEqual(100d, border.Width);

		// Show the control, animation resumes.
		border.IsVisible = true;

		clock.Step(TimeSpan.FromSeconds(4.5));
		CornerstoneTest.IsTrue(completed);

		disposable.Dispose();
	}

	[PresentationTestMethod]
	public async Task CancellationOfCompletedAnimationDoesNotFail()
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 200d) }, Cue = new Cue(1d)
		};

		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 100d) }, Cue = new Cue(0d)
		};

		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(10),
			Delay = TimeSpan.FromSeconds(0),
			DelayBetweenIterations = TimeSpan.FromSeconds(0),
			IterationCount = new IterationCount(1),
			Children = { keyframe2, keyframe1 }
		};

		var border = new Border { Height = 100d, Width = 50d };
		var propertyChangedCount = 0;
		border.PropertyChanged += (_, e) =>
		{
			if (e.Property == Layoutable.WidthProperty)
			{
				propertyChangedCount++;
			}
		};

		var clock = new TestClock();
		var cancellationTokenSource = new CancellationTokenSource();
		var animationRun = animation.RunAsync(border, clock, cancellationTokenSource.Token);

		CornerstoneTest.AreEqual(0, propertyChangedCount);

		clock.Step(TimeSpan.FromSeconds(0));
		CornerstoneTest.IsFalse(animationRun.IsCompleted);
		CornerstoneTest.AreEqual(1, propertyChangedCount);

		clock.Step(TimeSpan.FromSeconds(10));
		CornerstoneTest.IsTrue(animationRun.IsCompleted);
		CornerstoneTest.AreEqual(2, propertyChangedCount);

		cancellationTokenSource.Cancel();
		await animationRun;
	}

	[PresentationTestMethod]
	public async Task CancellationShouldStopAnimation()
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 200d) }, Cue = new Cue(1d)
		};

		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 100d) }, Cue = new Cue(0d)
		};

		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(10),
			Delay = TimeSpan.FromSeconds(0),
			DelayBetweenIterations = TimeSpan.FromSeconds(0),
			IterationCount = new IterationCount(1),
			Children = { keyframe2, keyframe1 }
		};

		var border = new Border { Height = 100d, Width = 50d };
		var propertyChangedCount = 0;
		border.PropertyChanged += (_, e) =>
		{
			if (e.Property == Layoutable.WidthProperty)
			{
				propertyChangedCount++;
			}
		};

		var clock = new TestClock();
		var cancellationTokenSource = new CancellationTokenSource();
		var animationRun = animation.RunAsync(border, clock, cancellationTokenSource.Token);
		CornerstoneTest.IsFalse(animationRun.IsCompleted);

		CornerstoneTest.AreEqual(0, propertyChangedCount);

		clock.Step(TimeSpan.FromSeconds(0));
		CornerstoneTest.IsFalse(animationRun.IsCompleted);
		CornerstoneTest.AreEqual(1, propertyChangedCount);

		cancellationTokenSource.Cancel();
		clock.Step(TimeSpan.FromSeconds(1));
		clock.Step(TimeSpan.FromSeconds(2));
		clock.Step(TimeSpan.FromSeconds(3));

		await animationRun;

		clock.Step(TimeSpan.FromSeconds(6));
		CornerstoneTest.IsTrue(animationRun.IsCompleted);
		CornerstoneTest.AreEqual(2, propertyChangedCount);
	}

	[PresentationTestMethod]
	public async Task CancellingExpandAnimationMidFlightThenCollapsingWorks()
	{
		// Reproduces the scenario where a user rapidly toggles expand/collapse:
		// the first animation is cancelled and a new one starts in the opposite direction.
		// Uses single-property animations to isolate the visibility behavior.
		var expandAnimation = new Animation
		{
			Duration = TimeSpan.FromSeconds(0.3),
			FillMode = FillMode.Forward,
			Children =
			{
				new KeyFrame
				{
					Cue = new Cue(0.0),
					Setters = { new Setter(Visual.IsVisibleProperty, true) }
				},
				new KeyFrame
				{
					Cue = new Cue(1.0),
					Setters = { new Setter(Visual.IsVisibleProperty, true) }
				}
			}
		};

		var collapseAnimation = new Animation
		{
			Duration = TimeSpan.FromSeconds(0.3),
			FillMode = FillMode.Forward,
			Children =
			{
				new KeyFrame
				{
					Cue = new Cue(0.0),
					Setters = { new Setter(Visual.IsVisibleProperty, true) }
				},
				new KeyFrame
				{
					Cue = new Cue(1.0),
					Setters = { new Setter(Visual.IsVisibleProperty, false) }
				}
			}
		};

		var border = new Border { IsVisible = false };

		// Start expand.
		var cts1 = new CancellationTokenSource();
		var clock1 = new TestClock();
		var expandRun = expandAnimation.RunAsync(border, clock1, cts1.Token);

		clock1.Step(TimeSpan.Zero);
		CornerstoneTest.IsTrue(border.IsVisible);

		// Partially through expand, cancel and start collapse.
		clock1.Step(TimeSpan.FromSeconds(0.15));
		cts1.Cancel();
		await expandRun;

		var cts2 = new CancellationTokenSource();
		var clock2 = new TestClock();
		var collapseRun = collapseAnimation.RunAsync(border, clock2, cts2.Token);

		clock2.Step(TimeSpan.Zero);
		clock2.Step(TimeSpan.FromSeconds(0.3));

		CornerstoneTest.IsTrue(collapseRun.IsCompleted);
		CornerstoneTest.IsFalse(border.IsVisible);
	}

	[PresentationTestMethod]
	[DataRow(FillMode.Backward, 100.0, 0.3, 1.0, false)]
	[DataRow(FillMode.Backward, 100.0, 0.3, 1.0, true)]
	[DataRow(FillMode.Both, 300.0, 0.3, 1.0, false)]
	[DataRow(FillMode.Both, 300.0, 0.3, 1.0, true)]
	[DataRow(FillMode.Forward, 300.0, 0.3, 1.0, false)]
	[DataRow(FillMode.Forward, 300.0, 0.3, 1.0, true)]
	[DataRow(FillMode.Backward, 100.0, 0.3, 0.7, false)]
	[DataRow(FillMode.Backward, 100.0, 0.3, 0.7, true)]
	[DataRow(FillMode.Both, 300.0, 0.3, 0.7, false)]
	[DataRow(FillMode.Both, 300.0, 0.3, 0.7, true)]
	[DataRow(FillMode.Forward, 300.0, 0.3, 0.7, false)]
	[DataRow(FillMode.Forward, 300.0, 0.3, 0.7, true)]
	public void CheckFillModeEndValue(FillMode fillMode, double target, double startCue, double endCue, bool delay)
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 0d) }, Cue = new Cue(startCue)
		};

		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 300d) }, Cue = new Cue(endCue)
		};

		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(10d),
			Delay = delay ? TimeSpan.FromSeconds(5d) : TimeSpan.Zero,
			FillMode = fillMode,
			Children = { keyframe1, keyframe2 }
		};

		var border = new Border { Height = 100d, Width = 100d };

		var clock = new TestClock();

		animation.RunAsync(border, clock, CancellationToken.None);

		clock.Step(TimeSpan.FromSeconds(0));
		clock.Step(TimeSpan.FromSeconds(20));

		CornerstoneTest.AreEqual(target, border.Width);
	}

	[PresentationTestMethod]
	[DataRow(FillMode.Backward, 50.0, 0.0, 0.7, false)]
	[DataRow(FillMode.Backward, 50.0, 0.0, 0.7, true)]
	[DataRow(FillMode.Both, 50.0, 0.0, 0.7, false)]
	[DataRow(FillMode.Both, 50.0, 0.0, 0.7, true)]
	[DataRow(FillMode.Forward, 50.0, 0.0, 0.7, false)] // no delay but cue 0.0: the animation has started normally, explaining the 50.0 target without fill
	[DataRow(FillMode.Forward, 100.0, 0.0, 0.7, true)]
	[DataRow(FillMode.Backward, 50.0, 0.3, 0.7, false)]
	[DataRow(FillMode.Backward, 50.0, 0.3, 0.7, true)]
	[DataRow(FillMode.Both, 50.0, 0.3, 0.7, false)]
	[DataRow(FillMode.Both, 50.0, 0.3, 0.7, true)]
	[DataRow(FillMode.Forward, 100.0, 0.3, 0.7, false)]
	[DataRow(FillMode.Forward, 100.0, 0.3, 0.7, true)]
	public void CheckFillModeStartValue(FillMode fillMode, double target, double startCue, double endCue, bool delay)
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 50d) }, Cue = new Cue(startCue)
		};

		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 300d) }, Cue = new Cue(endCue)
		};

		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(10d),
			Delay = delay ? TimeSpan.FromSeconds(5d) : TimeSpan.Zero,
			FillMode = fillMode,
			Children = { keyframe1, keyframe2 }
		};

		var border = new Border { Height = 100d, Width = 100d };

		var clock = new TestClock();

		animation.RunAsync(border, clock, CancellationToken.None);

		clock.Step(TimeSpan.Zero);

		CornerstoneTest.AreEqual(target, border.Width);
	}

	[PresentationTestMethod]
	public void CheckFillModesStartandEndValuesifRetained()
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 0d) }, Cue = new Cue(0.0d)
		};

		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 300d) }, Cue = new Cue(1.0d)
		};

		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(0.05d),
			Delay = TimeSpan.FromSeconds(0.05d),
			Easing = new SineEaseInOut(),
			FillMode = FillMode.Both,
			Children = { keyframe1, keyframe2 }
		};

		var border = new Border { Height = 100d, Width = 100d };

		var clock = new TestClock();

		animation.RunAsync(border, clock, CancellationToken.None);

		clock.Step(TimeSpan.FromSeconds(0d));
		CornerstoneTest.AreEqual(border.Width, 0d);

		clock.Step(TimeSpan.FromSeconds(0.050d));
		CornerstoneTest.AreEqual(border.Width, 0d);

		clock.Step(TimeSpan.FromSeconds(0.100d));
		CornerstoneTest.AreEqual(border.Width, 300d);
	}

	[PresentationTestMethod]
	public void CheckInitialInterandTrailingDelayValues()
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 200d) }, Cue = new Cue(1d)
		};

		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 100d) }, Cue = new Cue(0d)
		};

		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(3),
			Delay = TimeSpan.FromSeconds(3),
			DelayBetweenIterations = TimeSpan.FromSeconds(3),
			IterationCount = new IterationCount(2),
			Children = { keyframe2, keyframe1 }
		};

		var border = new Border { Height = 100d, Width = 100d };

		var clock = new TestClock();
		var animationRun = animation.RunAsync(border, clock, CancellationToken.None);

		border.Measure(Size.Infinity);
		border.Arrange(new Rect(border.DesiredSize));

		clock.Step(TimeSpan.Zero);

		// Initial Delay.
		clock.Step(TimeSpan.FromSeconds(0));
		CornerstoneTest.AreEqual(100d, border.Width);

		clock.Step(TimeSpan.FromSeconds(6));

		// First Inter-Iteration delay.
		clock.Step(TimeSpan.FromSeconds(8));
		CornerstoneTest.AreEqual(border.Width, 200d);

		// Trailing Delay should be non-existent.
		clock.Step(TimeSpan.FromSeconds(14));
		CornerstoneTest.IsTrue(animationRun.Status == TaskStatus.RanToCompletion);
		CornerstoneTest.AreEqual(border.Width, 100d);
	}

	[PresentationTestMethod]
	public void CheckKeyTimeCorrectlyConvertedToCue()
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 100d) }, KeyTime = TimeSpan.FromSeconds(0.5)
		};

		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 0d) }, KeyTime = TimeSpan.FromSeconds(0)
		};

		var animation = new Animation { Duration = TimeSpan.FromSeconds(1), Children = { keyframe2, keyframe1 } };

		var border = new Border { Height = 100d, Width = 100d };

		var clock = new TestClock();

		animation.RunAsync(border, clock, CancellationToken.None);

		clock.Step(TimeSpan.Zero);
		CornerstoneTest.AreEqual(border.Width, 0d);

		clock.Step(TimeSpan.FromSeconds(1));
		CornerstoneTest.AreEqual(border.Width, 100d);
	}

	[PresentationTestMethod]
	public void DisposeSubscriptionShouldStopAnimation()
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 200d) }, Cue = new Cue(1d)
		};

		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 100d) }, Cue = new Cue(0d)
		};

		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(10),
			Delay = TimeSpan.FromSeconds(0),
			DelayBetweenIterations = TimeSpan.FromSeconds(0),
			IterationCount = new IterationCount(1),
			Children = { keyframe2, keyframe1 }
		};

		var border = new Border { Height = 100d, Width = 50d };
		var propertyChangedCount = 0;
		var animationCompletedCount = 0;
		border.PropertyChanged += (_, e) =>
		{
			if (e.Property == Layoutable.WidthProperty)
			{
				propertyChangedCount++;
			}
		};

		var clock = new TestClock();
		var disposable = animation.Apply(border, clock, Observable.Return(true), () => animationCompletedCount++);

		CornerstoneTest.AreEqual(0, propertyChangedCount);

		clock.Step(TimeSpan.FromSeconds(0));
		CornerstoneTest.AreEqual(0, animationCompletedCount);
		CornerstoneTest.AreEqual(1, propertyChangedCount);

		disposable.Dispose();

		// Clock ticks should be ignored after Dispose
		clock.Step(TimeSpan.FromSeconds(5));
		clock.Step(TimeSpan.FromSeconds(6));
		clock.Step(TimeSpan.FromSeconds(7));

		// On animation disposing (cancellation) on completed is not invoked (is it expected)
		CornerstoneTest.AreEqual(0, animationCompletedCount);

		// Initial property changed before cancellation + animation value removal.
		CornerstoneTest.AreEqual(2, propertyChangedCount);
	}

	[PresentationTestMethod]
	public void DoNotRunCancelledAnimation()
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 200d) }, Cue = new Cue(1d)
		};

		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 100d) }, Cue = new Cue(0d)
		};

		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(10),
			Delay = TimeSpan.FromSeconds(0),
			DelayBetweenIterations = TimeSpan.FromSeconds(0),
			IterationCount = new IterationCount(1),
			Children = { keyframe2, keyframe1 }
		};

		var border = new Border { Height = 100d, Width = 100d };
		var propertyChangedCount = 0;
		border.PropertyChanged += (_, e) =>
		{
			if (e.Property == Layoutable.WidthProperty)
			{
				propertyChangedCount++;
			}
		};

		var clock = new TestClock();
		var cancellationTokenSource = new CancellationTokenSource();
		cancellationTokenSource.Cancel();
		var animationRun = animation.RunAsync(border, clock, cancellationTokenSource.Token);

		clock.Step(TimeSpan.FromSeconds(10));
		CornerstoneTest.AreEqual(0, propertyChangedCount);
		CornerstoneTest.IsTrue(animationRun.IsCompleted);
	}

	[PresentationTestMethod]
	public void DontRunInfiniteIterationAnimationOnRunAsyncMethod()
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 200d) }, Cue = new Cue(1d)
		};

		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 100d) }, Cue = new Cue(0d)
		};

		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(10),
			Delay = TimeSpan.FromSeconds(0),
			DelayBetweenIterations = TimeSpan.FromSeconds(0),
			IterationCount = IterationCount.Infinite,
			Children = { keyframe2, keyframe1 }
		};

		var border = new Border { Height = 100d, Width = 50d };
		var clock = new TestClock();
		var cancellationTokenSource = new CancellationTokenSource();
		var animationRun = animation.RunAsync(border, clock, cancellationTokenSource.Token);

		CornerstoneTest.IsTrue(animationRun.IsCompleted);
		CornerstoneTest.IsNotNull(animationRun.Exception);
	}

	[PresentationTestMethod]
	public void FillModeAppliesFinalValueWhenVisualDetachedDuringAnimation()
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 100d) },
			Cue = new Cue(0d)
		};
		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 300d) },
			Cue = new Cue(1d)
		};

		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(5),
			IterationCount = new IterationCount(1),
			FillMode = FillMode.Forward,
			Children = { keyframe1, keyframe2 }
		};

		var border = new Border { Height = 100d, Width = 50d };
		var root = new TestRoot(border);
		var clock = new TestClock();
		var animationRun = animation.RunAsync(border, clock, CancellationToken.None);

		clock.Step(TimeSpan.Zero);
		CornerstoneTest.AreEqual(100d, border.Width);

		// Detach from visual tree immediately
		root.Child = null;

		// The final value should be applied
		CornerstoneTest.IsTrue(animationRun.IsCompleted);
		CornerstoneTest.AreEqual(300d, border.Width);
	}

	// https://github.com/AvaloniaUI/Avalonia/issues/12582
	[PresentationTestMethod]
	public async Task InterpolatorIsNotCalledAfterLastIteration()
	{
		var animator = new FakeAnimator();

		Setter CreateWidthSetter(double value)
		{
			var setter = new Setter(Layoutable.WidthProperty, value);
			Animation.SetAnimator(setter, animator);
			return setter;
		}

		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(1),
			Delay = TimeSpan.FromSeconds(0),
			DelayBetweenIterations = TimeSpan.FromSeconds(0),
			IterationCount = new IterationCount(1),
			Easing = new LinearEasing(),
			Children =
			{
				new KeyFrame
				{
					Setters = { CreateWidthSetter(100d) },
					Cue = new Cue(0d)
				},
				new KeyFrame
				{
					Setters = { CreateWidthSetter(200d) },
					Cue = new Cue(1d)
				}
			}
		};

		var border = new Border
		{
			Height = 100d,
			Width = 50d
		};

		var clock = new TestClock();
		var animationRun = animation.RunAsync(border, clock, CancellationToken.None);

		clock.Step(TimeSpan.Zero);
		CornerstoneTest.AreEqual(1, animator.CallCount);
		CornerstoneTest.AreEqual(0.0d, animator.LastProgress);
		animator.LastProgress = double.NaN;

		clock.Step(TimeSpan.FromSeconds(0.5d));
		CornerstoneTest.AreEqual(2, animator.CallCount);
		CornerstoneTest.AreEqual(0.5d, animator.LastProgress);
		animator.LastProgress = double.NaN;

		clock.Step(TimeSpan.FromSeconds(1.5d));
		CornerstoneTest.AreEqual(3, animator.CallCount);
		CornerstoneTest.AreEqual(1.0d, animator.LastProgress);

		await animationRun;
	}

	[PresentationTestMethod]
	[DataRow(0, 1, 2)]
	[DataRow(0, 2, 1)]
	[DataRow(1, 0, 2)]
	[DataRow(1, 2, 0)]
	[DataRow(2, 0, 1)]
	[DataRow(2, 1, 0)]
	public void KeyFramesOrderDoesNotMatter(int index0, int index1, int index2)
	{
		static KeyFrame CreateKeyFrame(double width, double cue)
		{
			return new()
			{
				Setters = { new Setter(Layoutable.WidthProperty, width) },
				Cue = new Cue(cue)
			};
		}

		var keyFrames = new[]
		{
			CreateKeyFrame(100.0, 0.0),
			CreateKeyFrame(200.0, 0.5),
			CreateKeyFrame(300.0, 1.0)
		};

		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(1.0),
			IterationCount = new IterationCount(1),
			Easing = new LinearEasing(),
			FillMode = FillMode.Forward
		};

		animation.Children.Add(keyFrames[index0]);
		animation.Children.Add(keyFrames[index1]);
		animation.Children.Add(keyFrames[index2]);

		var border = new Border
		{
			Height = 100.0,
			Width = 50.0
		};

		var clock = new TestClock();
		animation.RunAsync(border, clock, CancellationToken.None);

		clock.Step(TimeSpan.Zero);
		CornerstoneTest.AreEqual(100.0, border.Width);

		clock.Step(TimeSpan.FromSeconds(0.5));
		CornerstoneTest.AreEqual(200.0, border.Width);

		clock.Step(TimeSpan.FromSeconds(1.0));
		CornerstoneTest.AreEqual(300.0, border.Width);
	}

	[PresentationTestMethod]
	public void OnlyIfVisiblePausesAnimationWhenControlStartsInvisible()
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 200d) }, Cue = new Cue(1d)
		};
		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 100d) }, Cue = new Cue(0d)
		};
		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(3),
			IterationCount = new IterationCount(1),

			// Explicit opt-in: RunAsync (manual) with Auto resolves to Always,
			// but this test specifically exercises the pause-on-invisible feature.
			PlaybackBehavior = PlaybackBehavior.OnlyIfVisible,
			Children = { keyframe2, keyframe1 }
		};

		var border = new Border { Height = 100d, Width = 100d, IsVisible = false };
		var clock = new TestClock();
		var animationRun = animation.RunAsync(border, clock, CancellationToken.None);

		// Clock ticks while invisible should not advance the animation.
		clock.Step(TimeSpan.Zero);
		clock.Step(TimeSpan.FromSeconds(1));
		clock.Step(TimeSpan.FromSeconds(2));
		CornerstoneTest.AreEqual(100d, border.Width);
		CornerstoneTest.IsFalse(animationRun.IsCompleted);

		// Make visible — animation starts from the beginning.
		border.IsVisible = true;

		// The pause absorbed 2s of wall-clock time, so to reach internal time 3s:
		// wall = 2 + 3 = 5
		clock.Step(TimeSpan.FromSeconds(5));
		CornerstoneTest.IsTrue(animationRun.IsCompleted);
		CornerstoneTest.AreEqual(100d, border.Width);
	}

	[PresentationTestMethod]
	public void OnlyIfVisiblePausesAnimationWhenIsEffectivelyVisibleIsFalse()
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 200d) }, Cue = new Cue(1d)
		};
		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 150d) }, Cue = new Cue(0.5d)
		};

		var keyframe3 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 100d) }, Cue = new Cue(0d)
		};

		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(3),
			Delay = TimeSpan.FromSeconds(3),
			DelayBetweenIterations = TimeSpan.FromSeconds(3),
			IterationCount = new IterationCount(2),

			// Explicit opt-in: RunAsync (manual) with Auto resolves to Always,
			// but this test specifically exercises the pause-on-invisible feature.
			PlaybackBehavior = PlaybackBehavior.OnlyIfVisible,
			Children = { keyframe1, keyframe2, keyframe3 }
		};

		var border = new Border { Height = 100d, Width = 100d };

		var clock = new TestClock();
		var animationRun = animation.RunAsync(border, clock, CancellationToken.None);

		border.Measure(Size.Infinity);
		border.Arrange(new Rect(border.DesiredSize));

		clock.Step(TimeSpan.Zero);

		clock.Step(TimeSpan.FromSeconds(0));
		CornerstoneTest.AreEqual(100d, border.Width);

		// Hide the border — this should pause the animation clock.
		border.IsVisible = false;

		clock.Step(TimeSpan.FromSeconds(4.5));

		// Width should not change while invisible (animation is paused).
		CornerstoneTest.AreEqual(100d, border.Width);

		// Show the border — animation resumes from where it left off.
		border.IsVisible = true;

		// The pause absorbed 4.5s of wall-clock time, so internal time = wall - 4.5.
		// To reach internal time 6s (end of iteration 1, cue 1.0 -> width 200):
		// wall = 4.5 + 6 = 10.5
		clock.Step(TimeSpan.FromSeconds(10.5));
		CornerstoneTest.AreEqual(200d, border.Width);

		// To complete the animation (internal time 14s triggers trailing delay of iter 2):
		// wall = 4.5 + 14 = 18.5
		clock.Step(TimeSpan.FromSeconds(18.5));
		CornerstoneTest.IsTrue(animationRun.Status == TaskStatus.RanToCompletion);
		CornerstoneTest.AreEqual(100d, border.Width);
	}

	[PresentationTestMethod]
	public void OnlyIfVisiblePausesAnimationWhenIsEffectivelyVisibleIsFalseNested()
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 200d) }, Cue = new Cue(1d)
		};
		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 150d) }, Cue = new Cue(0.5d)
		};

		var keyframe3 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 100d) }, Cue = new Cue(0d)
		};

		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(3),
			Delay = TimeSpan.FromSeconds(3),
			DelayBetweenIterations = TimeSpan.FromSeconds(3),
			IterationCount = new IterationCount(2),

			// Explicit opt-in: RunAsync (manual) with Auto resolves to Always,
			// but this test specifically exercises the pause-on-invisible feature.
			PlaybackBehavior = PlaybackBehavior.OnlyIfVisible,
			Children = { keyframe1, keyframe2, keyframe3 }
		};

		var border = new Border { Height = 100d, Width = 100d };

		var borderParent = new Border { Child = border };

		var clock = new TestClock();
		var animationRun = animation.RunAsync(border, clock, CancellationToken.None);

		border.Measure(Size.Infinity);
		border.Arrange(new Rect(border.DesiredSize));

		clock.Step(TimeSpan.Zero);

		clock.Step(TimeSpan.FromSeconds(0));
		CornerstoneTest.AreEqual(100d, border.Width);

		// Hide the parent — this makes border.IsEffectivelyVisible false,
		// which should pause the animation clock.
		borderParent.IsVisible = false;

		clock.Step(TimeSpan.FromSeconds(4.5));

		// Width should not change while parent is invisible (animation is paused).
		CornerstoneTest.AreEqual(100d, border.Width);

		// Show the parent — animation resumes from where it left off.
		borderParent.IsVisible = true;

		// The pause absorbed 4.5s of wall-clock time, so internal time = wall - 4.5.
		// To reach internal time 6s (end of iteration 1, cue 1.0 -> width 200):
		// wall = 4.5 + 6 = 10.5
		clock.Step(TimeSpan.FromSeconds(10.5));
		CornerstoneTest.AreEqual(200d, border.Width);

		// To complete the animation (internal time 14s triggers trailing delay of iter 2):
		// wall = 4.5 + 14 = 18.5
		clock.Step(TimeSpan.FromSeconds(18.5));
		CornerstoneTest.IsTrue(animationRun.Status == TaskStatus.RanToCompletion);
		CornerstoneTest.AreEqual(100d, border.Width);
	}

	[PresentationTestMethod]
	public void OnlyIfVisibleResumesTransformAnimationsWithVisual()
	{
		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(1),
			IterationCount = IterationCount.Infinite,
			Easing = new LinearEasing(),
			PlaybackBehavior = PlaybackBehavior.OnlyIfVisible,
			Children =
			{
				new KeyFrame
				{
					Cue = new Cue(0d),
					Setters =
					{
						new Setter(ScaleTransform.ScaleXProperty, 1d),
						new Setter(Visual.OpacityProperty, 0.9d)
					}
				},
				new KeyFrame
				{
					Cue = new Cue(1d),
					Setters =
					{
						new Setter(ScaleTransform.ScaleXProperty, 2.5d),
						new Setter(Visual.OpacityProperty, 0d)
					}
				}
			}
		};
		var border = new Border();
		var clock = new MockGlobalClock();

		using var subscription = animation.Apply(border, clock, Observable.Return(true), null);

		clock.Pulse(TimeSpan.FromSeconds(0.25));

		var scale = (ScaleTransform) ((TransformGroup) border.RenderTransform!).Children[0];
		var scaleBeforePause = scale.ScaleX;
		var opacityBeforePause = border.Opacity;

		border.IsVisible = false;
		clock.Pulse(TimeSpan.FromSeconds(0.75));

		CornerstoneTest.AreEqual(scaleBeforePause, scale.ScaleX);
		CornerstoneTest.AreEqual(opacityBeforePause, border.Opacity);

		border.IsVisible = true;
		clock.Pulse(TimeSpan.FromSeconds(1));

		CornerstoneTest.AreEqual(1.375d, scale.ScaleX);
		CornerstoneTest.AreEqual(0.675d, border.Opacity, 10);
	}

	[PresentationTestMethod]
	[DataRow(0.0)]
	[DataRow(0.5)]
	[DataRow(1.0)]
	public void SingleKeyFrameWorks(double cue)
	{
		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(1.0),
			IterationCount = new IterationCount(1),
			Easing = new LinearEasing(),
			FillMode = FillMode.Forward,
			Children =
			{
				new KeyFrame
				{
					Setters = { new Setter(Layoutable.WidthProperty, 100.0) },
					Cue = new Cue(cue)
				}
			}
		};

		var border = new Border
		{
			Height = 100.0,
			Width = 50.0
		};

		var clock = new TestClock();
		animation.RunAsync(border, clock, CancellationToken.None);

		clock.Step(TimeSpan.Zero);
		clock.Step(TimeSpan.FromSeconds(cue));
		CornerstoneTest.AreEqual(100.0, border.Width);
	}

	[PresentationTestMethod]
	public void StopAndDisposeAnimationWhenDetachedFromVisualTree()
	{
		var keyframe1 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 200d) }, Cue = new Cue(1d)
		};
		var keyframe2 = new KeyFrame
		{
			Setters = { new Setter(Layoutable.WidthProperty, 100d) }, Cue = new Cue(0d)
		};
		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(10),
			IterationCount = new IterationCount(1),
			Children = { keyframe2, keyframe1 }
		};
		var border = new Border { Height = 100d, Width = 50d };
		var root = new TestRoot(border);
		var clock = new TestClock();
		var animationRun = animation.RunAsync(border, clock, CancellationToken.None);
		clock.Step(TimeSpan.Zero);
		clock.Step(TimeSpan.FromSeconds(0));
		CornerstoneTest.IsFalse(animationRun.IsCompleted);

		// Detach from visual tree
		root.Child = null;

		// Animation should be completed/disposed
		CornerstoneTest.IsTrue(animationRun.IsCompleted);

		// Further clock ticks should not affect the border
		var widthAfterDetach = border.Width;
		clock.Step(TimeSpan.FromSeconds(5));
		clock.Step(TimeSpan.FromSeconds(10));
		CornerstoneTest.AreEqual(widthAfterDetach, border.Width);
	}

	[PresentationTestMethod]
	public void WidthAnimationResumesAfterIsVisibleSetTrueOnInvisibleControl()
	{
		// Tests the expand scenario with OnlyIfVisible: the control starts invisible
		// and the animation is paused. Once IsVisible is set to true externally,
		// the animation resumes and completes.
		var animation = new Animation
		{
			Duration = TimeSpan.FromSeconds(0.3),
			Easing = new LinearEasing(),
			FillMode = FillMode.Forward,
			PlaybackBehavior = PlaybackBehavior.OnlyIfVisible,
			Children =
			{
				new KeyFrame
				{
					Cue = new Cue(0.0),
					Setters = { new Setter(Layoutable.WidthProperty, 0d) }
				},
				new KeyFrame
				{
					Cue = new Cue(1.0),
					Setters = { new Setter(Layoutable.WidthProperty, 100d) }
				}
			}
		};

		// Control starts invisible (collapsed state).
		var border = new Border { Width = 0d, IsVisible = false };

		var clock = new TestClock();
		var animationRun = animation.RunAsync(border, clock, CancellationToken.None);

		// Animation is paused because control is invisible.
		clock.Step(TimeSpan.Zero);
		CornerstoneTest.AreEqual(0d, border.Width);
		CornerstoneTest.IsFalse(animationRun.IsCompleted);

		// Simulate what the expand handler does: set IsVisible = true externally.
		border.IsVisible = true;

		// The animation should now resume and complete.
		clock.Step(TimeSpan.FromSeconds(0.3));
		CornerstoneTest.IsTrue(animationRun.IsCompleted);
		CornerstoneTest.AreEqual(100d, border.Width);
	}

	#endregion

	#region Classes

	private sealed class FakeAnimator : InterpolatingAnimator<double>
	{
		#region Properties

		public int CallCount { get; set; }
		public double LastProgress { get; set; } = double.NaN;

		#endregion

		#region Methods

		public override double Interpolate(double progress, double oldValue, double newValue)
		{
			++CallCount;
			LastProgress = progress;
			return newValue;
		}

		#endregion
	}

	#endregion
}