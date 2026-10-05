#region References

using System;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Platform.Storage;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class HyperlinkButtonTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ClickWithoutNavigateUriDoesNotVisit()
	{
		var target = new HyperlinkButton();
		var clicks = 0;
		target.Click += (_, _) => clicks++;

		((IClickableControl) target).RaiseClick();

		CornerstoneTest.AreEqual(1, clicks);
		CornerstoneTest.IsFalse(target.IsVisited);
		CornerstoneTest.IsFalse(target.Classes.Contains(":visited"));
	}

	[PresentationTestMethod]
	public void DeclaredPropertiesRoundTrip()
	{
		var target = new HyperlinkButton();
		var uri = new Uri("https://example.com/start");

		CornerstoneTest.IsFalse(target.IsVisited);
		CornerstoneTest.IsNull(target.NavigateUri);
		CornerstoneTest.IsFalse(target.Classes.Contains(":visited"));

		target.NavigateUri = uri;
		target.IsVisited = true;

		CornerstoneTest.AreEqual(uri, target.NavigateUri);
		CornerstoneTest.IsTrue(target.IsVisited);
		CornerstoneTest.IsTrue(target.Classes.Contains(":visited"));

		target.IsVisited = false;

		CornerstoneTest.IsFalse(target.Classes.Contains(":visited"));
	}

	[PresentationTestMethod]
	public void FailedLaunchDoesNotMarkVisited()
	{
		var launcher = new TestLauncher { Result = false };
		var target = Show(launcher, new Uri("https://example.com/missing"));

		((IClickableControl) target).RaiseClick();
		Dispatcher.UIThread.RunJobs();

		CornerstoneTest.AreEqual(1, launcher.Calls);
		CornerstoneTest.IsFalse(target.IsVisited);
		CornerstoneTest.IsFalse(target.Classes.Contains(":visited"));
	}

	[PresentationTestMethod]
	public void SuccessfulLaunchMarksVisited()
	{
		var uri = new Uri("https://example.com/docs");
		var launcher = new TestLauncher();
		var target = Show(launcher, uri);

		((IClickableControl) target).RaiseClick();
		Dispatcher.UIThread.RunJobs();

		CornerstoneTest.AreEqual(1, launcher.Calls);
		CornerstoneTest.AreEqual(uri, launcher.Uri);
		CornerstoneTest.IsTrue(target.IsVisited);
		CornerstoneTest.IsTrue(target.Classes.Contains(":visited"));
	}

	private static HyperlinkButton Show(TestLauncher launcher, Uri uri)
	{
		var impl = new StubWindowImpl();
		impl.SetFeature(typeof(ILauncher), launcher);
		var target = new HyperlinkButton { NavigateUri = uri };
		var window = new Window(impl) { Content = target };
		window.Show();
		return target;
	}

	#endregion

	#region Classes

	private sealed class TestLauncher : ILauncher
	{
		#region Properties

		public int Calls { get; private set; }

		public bool Result { get; set; } = true;

		public Uri Uri { get; private set; }

		#endregion

		#region Methods

		public Task<bool> LaunchFileAsync(IStorageItem storageItem)
		{
			return Task.FromResult(false);
		}

		public Task<bool> LaunchUriAsync(Uri uri)
		{
			Calls++;
			Uri = uri;
			return Task.FromResult(Result);
		}

		#endregion
	}

	#endregion
}