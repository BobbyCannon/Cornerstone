#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Platform;

[TestClass]
public class InputPaneTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void InputPaneAwareViewResizesContentWhenBehaviorPan()
	{
		var clock = new MockGlobalClock();
		using (UnitTestApplication.Start(TestServices.RealFocus.With(globalClock: clock)))
		{
			var inputPane = new TestInputPane(new Rect(0, 200, 200, 200));

			var border = new Border();

			var paneView = new InputPaneAwareDecorator
			{
				Child = border,
				Height = 500,
				Width = 200,
				Behavior = InputPaneAwareBehavior.Pan
			};

			var impl = CreateMockTopLevelImpl(inputPane);
			var topLevel = new TestTopLevel(impl)
			{
				Template = CreateTopLevelTemplate(),
				Content = paneView
			};

			topLevel.LayoutManager.ExecuteInitialLayoutPass();

			CornerstoneTest.AreEqual(0, border.Bounds.Top);

			inputPane.Open();

			clock.Pulse(TimeSpan.FromSeconds(5));

			topLevel.LayoutManager.ExecuteLayoutPass();

			CornerstoneTest.AreEqual(-200, border.Bounds.Top);
		}
	}

	[PresentationTestMethod]
	public void InputPaneAwareViewResizesContentWhenBehaviorResize()
	{
		var clock = new MockGlobalClock();
		using (UnitTestApplication.Start(TestServices.RealFocus.With(globalClock: clock)))
		{
			var inputPane = new TestInputPane(new Rect(0, 200, 200, 200));

			var border = new Border();

			var paneView = new InputPaneAwareDecorator
			{
				Child = border,
				Height = 500,
				Width = 200,
				Behavior = InputPaneAwareBehavior.Resize
			};

			var impl = CreateMockTopLevelImpl(inputPane);
			var topLevel = new TestTopLevel(impl)
			{
				Template = CreateTopLevelTemplate(),
				Content = paneView
			};

			topLevel.LayoutManager.ExecuteInitialLayoutPass();

			CornerstoneTest.AreEqual(500, border.Bounds.Height);

			inputPane.Open();

			clock.Pulse(TimeSpan.FromSeconds(5));

			topLevel.LayoutManager.ExecuteLayoutPass();

			CornerstoneTest.AreEqual(300, border.Bounds.Height);
		}
	}

	internal static Control CreateScrollViewerTemplate(ScrollViewer control, INameScope scope)
	{
		return new Grid
		{
			Children =
			{
				new Decorator
				{
					Name = "PART_KeyboardAwareDecorator",
					Child = new ScrollContentPresenter
					{
						Name = "PART_ContentPresenter"
					}.RegisterInNameScope(scope)
				}.RegisterInNameScope(scope)
			}
		};
	}

	private static StubWindowImpl CreateMockTopLevelImpl(TestInputPane inputPane)
	{
		var topLevel = new StubWindowImpl();
		topLevel.SetFeature(typeof(IInputPane), inputPane);
		return topLevel;
	}

	private static FuncControlTemplate<TestTopLevel> CreateTopLevelTemplate()
	{
		return new FuncControlTemplate<TestTopLevel>((x, scope) =>
			new ContentPresenter
			{
				Name = "PART_ContentPresenter",
				[!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty]
			}.RegisterInNameScope(scope));
	}

	#endregion

	#region Classes

	private class TestInputPane(Rect openRect) : IInputPane
	{
		#region Properties

		public Rect OccludedRect { get; private set; }
		public InputPaneState State { get; private set; }

		#endregion

		#region Methods

		public void Close()
		{
			var oldRect = OccludedRect;
			OccludedRect = default;

			State = InputPaneState.Closed;

			StateChanged?.Invoke(this, new InputPaneStateEventArgs(State, oldRect, OccludedRect));
		}

		public void Open()
		{
			var oldRect = OccludedRect;
			OccludedRect = openRect;

			State = InputPaneState.Open;

			StateChanged?.Invoke(this, new InputPaneStateEventArgs(State, oldRect, OccludedRect));
		}

		#endregion

		#region Events

		public event EventHandler<InputPaneStateEventArgs> StateChanged;

		#endregion
	}

	private class TestTopLevel(ITopLevelImpl impl) : TopLevel(impl)
	{
	}

	#endregion
}