#region References

using System;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ApplicationTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void CanBindToDataContext()
	{
		using (UnitTestApplication.Start())
		{
			var application = Application.Current!;

			application.DataContext = "Test";

			application.Bind(Application.NameProperty, new Binding("."));

			CornerstoneTest.AreEqual("Test", application.Name);
		}
	}

	[PresentationTestMethod]
	public void RaisesResourcesChangedWhenEventHandlerAddedAfterResourcesHasBeenAccessed()
	{
		// Test for #1765.
		using (UnitTestApplication.Start())
		{
			var resources = Application.Current!.Resources;
			var raised = false;

			Application.Current.ResourcesChanged += (s, e) => raised = true;
			resources["foo"] = "bar";

			CornerstoneTest.IsTrue(raised);
		}
	}

	[PresentationTestMethod]
	public void ThrowsArgumentNullExceptionOnRunIfMainWindowIsNull()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			Assert.Throws<ArgumentNullException>(() => { Application.Current!.Run(null!); });
		}
	}

	#endregion
}