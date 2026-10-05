#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Controls.Transitioning;
using Cornerstone.Presentation.Harfbuzz;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class TransitioningContentControlTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ContentPresenters2ShouldBeSetup()
	{
		using var app = Start();
		var (target, transition) = CreateTarget("foo");
		var presenter1 = target.Presenter!;
		var presenter2 = GetContentPresenters2(target);

		target.Content = "bar";
		Layout(target);

		CornerstoneTest.IsTrue(presenter2.IsVisible);
		CornerstoneTest.AreEqual("foo", presenter1.Content);
		CornerstoneTest.AreEqual("bar", presenter2.Content);
	}

	[PresentationTestMethod]
	public void ContentPresenters2ShouldInitiallyBeHidden()
	{
		using var app = Start();
		var (target, transition) = CreateTarget("foo");
		var presenter2 = GetContentPresenters2(target);

		CornerstoneTest.IsFalse(presenter2.IsVisible);
	}

	[PresentationTestMethod]
	public void ControlShouldConnectToVisualTreeOnce()
	{
		using var app = Start();
		var (target, transition) = CreateTarget(new Control());

		var control = new Control();
		var counter = 0;

		control.AttachedToVisualTree += (s, e) => counter++;

		target.Content = control;
		Layout(target);
		target.Content = new Control();
		Layout(target);

		CornerstoneTest.AreEqual(1, counter);
	}

	[PresentationTestMethod]
	public void ControlTransitionShouldBeRunOnLayout()
	{
		using var app = Start();
		var (target, transition) = CreateTarget(new Button());

		target.Content = new Canvas();
		CornerstoneTest.AreEqual(0, transition.StartCount);

		Layout(target);
		CornerstoneTest.AreEqual(1, transition.StartCount);
	}

	[PresentationTestMethod]
	public void FirstPresenterShouldRegisterTCCAsHisHost()
	{
		using var app = Start();
		var (target, transition) = CreateTarget("");
		target.PageTransition = null;

		var childControl = new Control();
		target.Presenter!.Content = childControl;

		CornerstoneTest.AreEqual(1, target.LogicalChildren.Count);
		CornerstoneTest.AreEqual(target.LogicalChildren[0], childControl);
	}

	[PresentationTestMethod]
	public void LogicalChildrenShouldNotBeDuplicated()
	{
		using var app = Start();
		var (target, transition) = CreateTarget("");
		target.PageTransition = null;

		var childControl = new Control();
		target.Content = childControl;

		CornerstoneTest.AreEqual(1, target.LogicalChildren.Count);
		CornerstoneTest.AreEqual(target.LogicalChildren[0], childControl);
	}

	[PresentationTestMethod]
	public void NewTransitionShouldBeStartedIfContentChangesWhileRunning()
	{
		using var app = Start();
		using var sync = UnitTestSynchronizationContext.Begin();
		var (target, transition) = CreateTarget("foo");
		var presenter2 = GetContentPresenters2(target);

		target.Content = "bar";
		Layout(target);

		target.Content = "baz";

		var startedRaised = 0;

		transition.Started += (from, to, forward) =>
		{
			var fromPresenter = CornerstoneTest.IsType<ContentPresenter>(from);
			var toPresenter = CornerstoneTest.IsType<ContentPresenter>(to);

			CornerstoneTest.Same(presenter2, fromPresenter);
			CornerstoneTest.Same(target.Presenter, toPresenter);
			CornerstoneTest.AreEqual("bar", fromPresenter.Content);
			CornerstoneTest.AreEqual("baz", toPresenter.Content);
			CornerstoneTest.IsTrue(forward);
			CornerstoneTest.AreEqual(1, transition.CancelCount);

			++startedRaised;
		};

		Layout(target);
		sync.ExecutePostedCallbacks();

		CornerstoneTest.AreEqual(1, startedRaised);
		CornerstoneTest.AreEqual("baz", target.Presenter!.Content);
		CornerstoneTest.AreEqual("bar", presenter2.Content);
	}

	[PresentationTestMethod]
	public void OldContentShouldBeNullWhenNewContentIsOldone()
	{
		using var app = Start();
		var (target, transition) = CreateTarget("");
		var presenter2 = GetContentPresenters2(target);
		target.PageTransition = null;

		var childControl = new Control();
		target.Presenter!.Content = childControl;

		const string fakePage1 = "fakePage1";
		const string fakePage2 = "fakePage2";

		target.Presenter!.Content = fakePage1;
		target.Presenter!.Content = fakePage2;
		target.Presenter!.Content = fakePage1;

		CornerstoneTest.AreEqual(fakePage1, target.Presenter!.Content);
		CornerstoneTest.AreEqual(null, presenter2.Content);
	}

	[PresentationTestMethod]
	public void OldPresenterShouldBeHiddenWhenTransitionCompletes()
	{
		using var app = Start();
		using var sync = UnitTestSynchronizationContext.Begin();
		var (target, transition) = CreateTarget("foo");
		var presenter1 = target.Presenter!;
		var presenter2 = GetContentPresenters2(target);

		target.Content = "bar";
		Layout(target);
		CornerstoneTest.IsTrue(presenter1.IsVisible);
		CornerstoneTest.IsTrue(presenter2.IsVisible);

		transition.Complete();
		sync.ExecutePostedCallbacks();
		CornerstoneTest.IsTrue(presenter2.IsVisible);
		CornerstoneTest.IsFalse(presenter1.IsVisible);

		target.Content = "foo";
		Layout(target);
		CornerstoneTest.IsTrue(presenter1.IsVisible);
		CornerstoneTest.IsTrue(presenter2.IsVisible);

		transition.Complete();
		sync.ExecutePostedCallbacks();
		CornerstoneTest.IsTrue(presenter1.IsVisible);
		CornerstoneTest.IsFalse(presenter2.IsVisible);
	}

	[PresentationTestMethod]
	public void TransitionCompletedShouldBeRaisedIfContentChangesWhileRunning()
	{
		using var app = Start();
		using var sync = UnitTestSynchronizationContext.Begin();
		var (target, _) = CreateTarget("foo");

		var completedTransitions = new List<TransitionCompletedEventArgs>();
		target.TransitionCompleted += (_, e) => completedTransitions.Add(e);

		target.Content = "bar";
		Layout(target);
		sync.ExecutePostedCallbacks();
		VerifyCompletedTransitions();

		target.Content = "baz";
		Layout(target);
		sync.ExecutePostedCallbacks();
		VerifyCompletedTransitions(new TransitionCompletedEventArgs("foo", "bar", false));

		void VerifyCompletedTransitions(params TransitionCompletedEventArgs[] expected)
		{
			CornerstoneTest.AreEqual(expected, completedTransitions, TransitionCompletedEventArgsComparer.Instance);
		}
	}

	[PresentationTestMethod]
	public void TransitionCompletedShouldBeRaisedWhenContentChanges()
	{
		using var app = Start();
		using var sync = UnitTestSynchronizationContext.Begin();
		var (target, transition) = CreateTarget("foo");

		var completedTransitions = new List<TransitionCompletedEventArgs>();
		target.TransitionCompleted += (_, e) => completedTransitions.Add(e);

		target.Content = "bar";
		Layout(target);
		VerifyCompletedTransitions();

		transition.Complete();
		sync.ExecutePostedCallbacks();
		VerifyCompletedTransitions(new TransitionCompletedEventArgs("foo", "bar", true));

		target.Content = "foo";
		Layout(target);
		VerifyCompletedTransitions(new TransitionCompletedEventArgs("foo", "bar", true));

		transition.Complete();
		sync.ExecutePostedCallbacks();
		VerifyCompletedTransitions(new("foo", "bar", true), new("bar", "foo", true));

		void VerifyCompletedTransitions(params TransitionCompletedEventArgs[] expected)
		{
			CornerstoneTest.AreEqual(expected, completedTransitions, TransitionCompletedEventArgsComparer.Instance);
		}
	}

	[PresentationTestMethod]
	public void TransitionShouldBeCanceledIfContentChangesWhileRunning()
	{
		using var app = Start();
		using var sync = UnitTestSynchronizationContext.Begin();
		var (target, transition) = CreateTarget("foo");

		target.Content = "bar";
		Layout(target);
		target.Content = "baz";

		CornerstoneTest.AreEqual(0, transition.CancelCount);

		Layout(target);

		CornerstoneTest.AreEqual(1, transition.CancelCount);
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void TransitionShouldBeReversedIfPropertyIsSet(bool reversed)
	{
		using var app = Start();
		using var sync = UnitTestSynchronizationContext.Begin();
		var (target, transition) = CreateTarget("foo");
		var presenter2 = GetContentPresenters2(target);

		target.IsTransitionReversed = reversed;

		target.Content = "bar";

		var startedRaised = 0;

		transition.Started += (from, to, forward) =>
		{
			CornerstoneTest.AreEqual(reversed, !forward);

			++startedRaised;
		};

		Layout(target);
		sync.ExecutePostedCallbacks();

		CornerstoneTest.AreEqual(1, startedRaised);
		CornerstoneTest.AreEqual("foo", target.Presenter!.Content);
		CornerstoneTest.AreEqual("bar", presenter2.Content);
	}

	[PresentationTestMethod]
	public void TransitionShouldBeRunOnLayout()
	{
		using var app = Start();
		var (target, transition) = CreateTarget("foo");

		target.Content = "bar";
		CornerstoneTest.AreEqual(0, transition.StartCount);

		Layout(target);
		CornerstoneTest.AreEqual(1, transition.StartCount);
	}

	[PresentationTestMethod]
	public void TransitionShouldNotBeRunWhenFirstShown()
	{
		using var app = Start();
		var (target, transition) = CreateTarget("foo");

		CornerstoneTest.AreEqual(0, transition.StartCount);
	}

	private static (TransitioningContentControl, TestTransition) CreateTarget(object content)
	{
		var transition = new TestTransition();
		var target = new TransitioningContentControl
		{
			Content = content,
			PageTransition = transition,
			Template = CreateTemplate()
		};

		var root = new TestRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();
		return (target, transition);
	}

	private static IControlTemplate CreateTemplate()
	{
		return new FuncControlTemplate((x, ns) =>
		{
			return new Panel
			{
				Children =
				{
					new ContentPresenter
					{
						Name = "PART_ContentPresenter"
					},
					new ContentPresenter
					{
						Name = "PART_ContentPresenter2"
					}
				}
			};
		});
	}

	private static ContentPresenter GetContentPresenters2(TransitioningContentControl target)
	{
		return CornerstoneTest.IsType<ContentPresenter>(target
			.GetTemplateDescendants()
			.First(x => x.Name == "PART_ContentPresenter2"));
	}

	private void Layout(Control c)
	{
		c.GetLayoutManager()?.ExecuteLayoutPass();
	}

	private static IDisposable Start()
	{
		return UnitTestApplication.Start(
			TestServices.MockThreadingInterface.With(
				fontManagerImpl: new HeadlessFontManagerStub(),
				renderInterface: new HeadlessPlatformRenderInterface(),
				textShaperImpl: new HarfBuzzTextShaper(),
				assetLoader: new StandardAssetLoader()));
	}

	#endregion

	#region Classes

	private class TestTransition : IPageTransition
	{
		#region Fields

		private TaskCompletionSource _tcs;

		#endregion

		#region Properties

		public int CancelCount { get; private set; }
		public int FinishCount { get; private set; }

		public int StartCount { get; private set; }

		#endregion

		#region Methods

		public void Complete()
		{
			_tcs!.TrySetResult();
		}

		public async Task Start(Visual from, Visual to, bool forward, CancellationToken cancellationToken)
		{
			++StartCount;
			Started?.Invoke(from, to, forward);
			if (_tcs is not null)
			{
				throw new InvalidOperationException("Transition already running");
			}
			_tcs = new TaskCompletionSource();
			cancellationToken.Register(() => _tcs?.TrySetResult());
			await _tcs.Task;
			_tcs = null;

			if (!cancellationToken.IsCancellationRequested)
			{
				++FinishCount;
			}
			else
			{
				++CancelCount;
			}
		}

		#endregion

		#region Events

		public event Action<Visual, Visual, bool> Started;

		#endregion
	}

	private sealed class TransitionCompletedEventArgsComparer : IEqualityComparer<TransitionCompletedEventArgs>
	{
		#region Properties

		public static TransitionCompletedEventArgsComparer Instance { get; } = new();

		#endregion

		#region Methods

		public bool Equals(TransitionCompletedEventArgs x, TransitionCompletedEventArgs y)
		{
			if (ReferenceEquals(x, y))
			{
				return true;
			}

			if (x is null || y is null)
			{
				return false;
			}

			return (x.From == y.From) && (x.To == y.To) && (x.HasRunToCompletion == y.HasRunToCompletion);
		}

		public int GetHashCode(TransitionCompletedEventArgs obj)
		{
			return HashCode.Combine(obj.From, obj.To, obj.HasRunToCompletion);
		}

		#endregion
	}

	#endregion
}