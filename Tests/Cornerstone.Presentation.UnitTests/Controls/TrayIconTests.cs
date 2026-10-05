#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class TrayIconTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void Platform_Impl_Should_Be_Created_Only_After_Icon_Is_Attached_To_Application()
	{
		var impl = new RecordingTrayIcon();
		var createCount = 0;
		var platform = new MockWindowingPlatform(trayIconImpl: () =>
		{
			createCount++;
			return impl;
		});

		using (UnitTestApplication.Start(new TestServices(windowingPlatform: platform)))
		{
			var target = new TrayIcon { ToolTipText = "Test icon" };
			var icons = new TrayIcons { target };

			CornerstoneTest.AreEqual(0, createCount);

			TrayIcon.SetIcons(UnitTestApplication.Current, icons);

			CornerstoneTest.AreEqual(1, createCount);
			CornerstoneTest.AreEqual(1, impl.ToolTipCount);
			CornerstoneTest.AreEqual("Test icon", impl.LastToolTip);
			CornerstoneTest.AreEqual(1, impl.VisibleTrueCount);

			TrayIcon.SetIcons(UnitTestApplication.Current, null);

			CornerstoneTest.AreEqual(1, impl.DisposeCount);
		}
	}

	[PresentationTestMethod]
	public void Collection_Changes_Should_Attach_And_Detach_Icons()
	{
		var implementations = new List<RecordingTrayIcon>();
		var platform = new MockWindowingPlatform(trayIconImpl: () =>
		{
			var impl = new RecordingTrayIcon();
			implementations.Add(impl);
			return impl;
		});

		using (UnitTestApplication.Start(new TestServices(windowingPlatform: platform)))
		{
			var target = new TrayIcon();
			var icons = new TrayIcons();
			TrayIcon.SetIcons(UnitTestApplication.Current, icons);

			icons.Add(target);

			CornerstoneTest.AreEqual(1, implementations.Count);

			icons.Clear();

			CornerstoneTest.AreEqual(1, implementations[0].DisposeCount);

			icons.Add(target);

			CornerstoneTest.AreEqual(2, implementations.Count);
		}
	}

	[PresentationTestMethod]
	public void Replaced_Collection_Should_No_Longer_Attach_Icons()
	{
		var createCount = 0;
		var platform = new MockWindowingPlatform(trayIconImpl: () =>
		{
			++createCount;
			return new RecordingTrayIcon();
		});

		using (UnitTestApplication.Start(new TestServices(windowingPlatform: platform)))
		{
			var oldIcons = new TrayIcons();
			var newIcons = new TrayIcons();

			TrayIcon.SetIcons(UnitTestApplication.Current, oldIcons);
			TrayIcon.SetIcons(UnitTestApplication.Current, newIcons);
			oldIcons.Add(new TrayIcon());

			CornerstoneTest.AreEqual(0, createCount);

			newIcons.Add(new TrayIcon());

			CornerstoneTest.AreEqual(1, createCount);
		}
	}

	#endregion

	#region Classes

	private sealed class RecordingTrayIcon : ITrayIconImpl
	{
		public int DisposeCount { get; private set; }
		public string LastToolTip { get; private set; }
		public int ToolTipCount { get; private set; }
		public int VisibleTrueCount { get; private set; }

		public INativeMenuExporter MenuExporter => null;

		public Action OnClicked { get; set; }

		public void Dispose()
		{
			DisposeCount++;
		}

		public void SetIcon(IWindowIconImpl icon)
		{
		}

		public void SetIsVisible(bool visible)
		{
			if (visible)
				VisibleTrueCount++;
		}

		public void SetToolTipText(string text)
		{
			ToolTipCount++;
			LastToolTip = text;
		}
	}

	#endregion
}
