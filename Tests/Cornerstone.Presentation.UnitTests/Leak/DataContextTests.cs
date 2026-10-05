#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Leak;

internal class ViewModelForDisposingTest
{
	#region Constructors

	[SuppressMessage("ReSharper", "EmptyDestructor", Justification = "Needed for test")]
	~ViewModelForDisposingTest()
	{
	}

	#endregion
}

[TestClass]
public class DataContextTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void WindowDataContextDisposedAfterWindowCloseWithLifetime()
	{
		static IDisposable Run(out WeakReference weakDataContext)
		{
			var unitTestApp = UnitTestApplication.Start(TestServices.StyledWindow);
			var lifetime = new ClassicDesktopStyleApplicationLifetime();
			lifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;
			var viewModel = new ViewModelForDisposingTest();
			var window = new Window { DataContext = viewModel };
			window.Show();
			window.Close();

			var disposable = Disposable.Create(lifetime, lt => lt.Shutdown())
				.DisposeWith(new CompositeDisposable(lifetime, unitTestApp));

			weakDataContext = new WeakReference(viewModel);
			return disposable;
		}

		using var _ = Run(out var weakDataContext);
		CornerstoneTest.IsTrue(weakDataContext.IsAlive);

		CollectGarbage();

		CornerstoneTest.IsFalse(weakDataContext.IsAlive);
	}

	[PresentationTestMethod]
	public void WindowDataContextDisposedAfterWindowCloseWithoutLifetime()
	{
		static void Run(out WeakReference weakDataContext)
		{
			using var _ = UnitTestApplication.Start(TestServices.StyledWindow);
			var viewModel = new ViewModelForDisposingTest();
			var window = new Window { DataContext = viewModel };
			window.Show();
			window.Close();

			weakDataContext = new WeakReference(viewModel);
		}

		Run(out var weakDataContext);
		CornerstoneTest.IsTrue(weakDataContext.IsAlive);

		CollectGarbage();

		CornerstoneTest.IsFalse(weakDataContext.IsAlive);
	}

	private static void CollectGarbage()
	{
		// Process all Loaded events to free control reference(s)
		Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
		GC.Collect();
	}

	#endregion
}