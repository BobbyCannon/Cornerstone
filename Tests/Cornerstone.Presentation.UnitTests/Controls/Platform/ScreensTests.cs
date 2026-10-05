#region References

using System;
using System.Collections.Generic;
using System.Threading;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Platform;

[TestClass]
public class ScreensTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldPreserveOldScreensOnChanges()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

		var screens = new TestScreens();
		var totalScreens = new HashSet<TestScreen>();

		CornerstoneTest.AreEqual(0, screens.ScreenCount);
		CornerstoneTest.Empty(screens.AllScreens);

		// Push 2 screens.
		screens.PushNewScreens([1, 2]);
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

		CornerstoneTest.AreEqual(2, screens.ScreenCount);
		totalScreens.Add(CornerstoneTest.IsType<TestScreen>(screens.GetScreen(1)));
		totalScreens.Add(CornerstoneTest.IsType<TestScreen>(screens.GetScreen(2)));

		// Push 3 screens, while removing one old.
		screens.PushNewScreens([2, 3, 4]);
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

		CornerstoneTest.AreEqual(3, screens.ScreenCount);
		CornerstoneTest.IsNull(screens.GetScreen(1));
		totalScreens.Add(CornerstoneTest.IsType<TestScreen>(screens.GetScreen(2)));
		totalScreens.Add(CornerstoneTest.IsType<TestScreen>(screens.GetScreen(3)));
		totalScreens.Add(CornerstoneTest.IsType<TestScreen>(screens.GetScreen(4)));

		CornerstoneTest.AreEqual(3, screens.AllScreens.Count);
		CornerstoneTest.AreEqual(3, screens.ScreenCount);
		CornerstoneTest.AreEqual(4, totalScreens.Count);

		CornerstoneTest.Collection(totalScreens, s1 => CornerstoneTest.IsTrue(s1.Generation < 0), // this screen was removed.
			s2 => CornerstoneTest.AreEqual(2, s2.Generation), // this screen survived first OnChange event, instance should be preserved.
			s3 => CornerstoneTest.AreEqual(1, s3.Generation), s4 => CornerstoneTest.AreEqual(1, s4.Generation));
	}

	[PresentationTestMethod]
	public void ShouldPreserveOldScreensOnChangesSameInstance()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

		var screens = new TestScreens();

		CornerstoneTest.AreEqual(0, screens.ScreenCount);
		CornerstoneTest.Empty(screens.AllScreens);

		screens.PushNewScreens([1]);
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

		var screen = screens.GetScreen(1);

		CornerstoneTest.IsNotNull(screen);
		CornerstoneTest.AreEqual(1, screen.Generation);
		CornerstoneTest.AreEqual(new IntPtr(1), screen.TryGetPlatformHandle()!.Handle);

		screens.PushNewScreens([1]);
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

		CornerstoneTest.AreEqual(2, screen.Generation);
		CornerstoneTest.AreEqual(new IntPtr(1), screen.TryGetPlatformHandle()!.Handle);
		CornerstoneTest.Same(screens.GetScreen(1), screen);
	}

	[PresentationTestMethod]
	public void ShouldRaiseEventAndUpdateScreensOnChanged()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

		var hasChangedTimes = 0;
		var screens = new TestScreens();
		screens.Changed = () => hasChangedTimes += 1;

		CornerstoneTest.AreEqual(0, screens.ScreenCount);
		CornerstoneTest.Empty(screens.AllScreens);

		screens.PushNewScreens([1, 2]);
		screens.PushNewScreens([1, 2]); // OnChanged can be triggered multiple times by different events
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

		CornerstoneTest.AreEqual(2, screens.ScreenCount);
		CornerstoneTest.NotEmpty(screens.AllScreens);

		CornerstoneTest.AreEqual(1, hasChangedTimes);
	}

	[PresentationTestMethod]
	public void ShouldRaiseEventWhenScreenChangedFromAnotherThread()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

		Dispatcher.UIThread.VerifyAccess();
		var hasChangedTimes = 0;
		var screens = new TestScreens();
		screens.Changed = () =>
		{
			Dispatcher.UIThread.VerifyAccess();
			hasChangedTimes += 1;
		};

		ThreadRunHelper.RunOnDedicatedThread(() => screens.PushNewScreens([1, 2])).GetAwaiter().GetResult();
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

		CornerstoneTest.AreEqual(1, hasChangedTimes);
	}

	[PresentationTestMethod]
	public void ShouldTriggerChangedWhenScreenRemoved()
	{
		using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

		var screens = new TestScreens();
		screens.PushNewScreens([1, 2]);
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

		var hasChangedTimes = 0;
		var screen = screens.GetScreen(2);
		CornerstoneTest.IsNotNull(screen);

		screens.Changed = () =>
		{
			CornerstoneTest.IsTrue(screen.Generation < 0);
			hasChangedTimes += 1;
		};

		screens.PushNewScreens([1]);
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

		CornerstoneTest.AreEqual(1, hasChangedTimes);
	}

	#endregion

	#region Classes

	public class TestScreen(int key) : PlatformScreen(new PlatformHandle(new IntPtr(key), "TestHandle"))
	{
		#region Properties

		public int Generation { get; set; }

		#endregion
	}

	private class TestScreens : ScreensBase<int, TestScreen>
	{
		#region Fields

		private int _count;
		private IReadOnlyList<int> _keys = [];

		#endregion

		#region Methods

		public TestScreen GetScreen(int key)
		{
			return TryGetScreen(key, out var screen) ? screen : null;
		}

		public void PushNewScreens(IReadOnlyList<int> keys)
		{
			_count = keys.Count;
			_keys = keys;
			OnChanged();
		}

		protected override TestScreen CreateScreenFromKey(int key)
		{
			return new(key);
		}

		protected override IReadOnlyList<int> GetAllScreenKeys()
		{
			return _keys;
		}

		protected override int GetScreenCount()
		{
			return _count;
		}

		protected override void ScreenChanged(TestScreen screen)
		{
			screen.Generation++;
		}

		protected override void ScreenRemoved(TestScreen screen)
		{
			screen.Generation = -1000;
		}

		#endregion
	}

	#endregion
}