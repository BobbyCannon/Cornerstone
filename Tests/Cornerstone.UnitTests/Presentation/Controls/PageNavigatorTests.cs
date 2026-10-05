#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Navigation;
using Cornerstone.Presentation.Controls.Text;
using Cornerstone.Presentation.Interactivity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Controls;

[TestClass]
public class PageNavigatorTests
{
	#region Methods

	[TestMethod]
	public void CrumbClickPopsToIndex()
	{
		var navigator = new PageNavigator { RootTitle = "Controls" };
		var home = new TextBlock { Text = "Home" };
		navigator.Content = home;
		navigator.ApplyTemplate();
		var buttons = new TextBlock { Text = "Buttons" };
		navigator.Navigate("Buttons", buttons);
		navigator.Navigate("Hold", new TextBlock { Text = "Hold" });

		var trail = new BreadcrumbTrail();
		trail.SetItems(navigator.Crumbs);
		var clicked = 0;
		trail.CrumbClicked += (_, e) => { clicked = e.Index; };
		trail.SelectCrumbCommand.Execute(navigator.Crumbs[0]);
		Assert.AreEqual(0, clicked);

		trail.SelectCrumbCommand.Execute(navigator.Crumbs[2]);
		Assert.AreEqual(0, clicked);
	}

	[TestMethod]
	public void GoHomePopsToRoot()
	{
		var navigator = new PageNavigator { RootTitle = "Controls" };
		var home = new TextBlock { Text = "Home" };
		navigator.Content = home;
		navigator.Navigate("Buttons", new TextBlock { Text = "Buttons" });
		navigator.Navigate("Menus", new TextBlock { Text = "Menus" });
		navigator.GoHome();
		Assert.AreSame(home, navigator.Content);
		Assert.IsFalse(navigator.CanGoBack);
	}

	[TestMethod]
	public void NavigateThenGoBackRestoresRoot()
	{
		var navigator = new PageNavigator { RootTitle = "Controls" };
		var home = new TextBlock { Text = "Home" };
		navigator.Content = home;
		navigator.ApplyTemplate();

		var buttons = new TextBlock { Text = "Buttons" };
		navigator.Navigate("Buttons", buttons);
		Assert.AreSame(buttons, navigator.Content);
		Assert.IsTrue(navigator.CanGoBack);
		Assert.AreEqual(2, navigator.Crumbs.Count);
		Assert.AreEqual("Controls", navigator.Crumbs[0].Title);
		Assert.IsTrue(navigator.Crumbs[1].IsCurrent);

		navigator.GoBack();
		Assert.AreSame(home, navigator.Content);
		Assert.IsFalse(navigator.CanGoBack);
		Assert.AreEqual(1, navigator.Crumbs.Count);
	}

	#endregion
}

[TestClass]
public class PageNavigatorSystemBackTests : CornerstoneCornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void BackRequestedPopsTheVisiblePage()
	{
		RunOnUi(() =>
		{
			var navigator = new PageNavigator { RootTitle = "Controls", Width = 400, Height = 300 };
			var home = new TextBlock { Text = "Home" };
			navigator.Content = home;
			var window = new Window { Width = 400, Height = 300, Content = navigator };
			window.Show();
			navigator.Navigate("Buttons", new TextBlock { Text = "Buttons" });
			window.UpdateLayout();

			var args = new RoutedEventArgs(TopLevel.BackRequestedEvent);
			window.RaiseEvent(args);

			Assert.IsTrue(args.Handled);
			Assert.AreSame(home, navigator.Content);
			Assert.IsFalse(navigator.CanGoBack);
			window.Close();
		});
	}

	[TestMethod]
	public void BackRequestedPopsTheInnerNavigator()
	{
		RunOnUi(() =>
		{
			var outer = new PageNavigator { RootTitle = "Outer", Width = 400, Height = 300 };
			var inner = new PageNavigator { RootTitle = "Inner", Width = 400, Height = 280 };
			var outerHome = new TextBlock { Text = "Outer home" };
			var innerHome = new TextBlock { Text = "Inner home" };
			outer.Content = outerHome;
			inner.Content = innerHome;
			var window = new Window { Width = 400, Height = 300, Content = outer };
			window.Show();
			outer.Navigate("Inner", inner);
			inner.Navigate("Buttons", new TextBlock { Text = "Buttons" });
			window.UpdateLayout();

			var args = new RoutedEventArgs(TopLevel.BackRequestedEvent);
			window.RaiseEvent(args);

			Assert.IsTrue(args.Handled);
			Assert.AreSame(innerHome, inner.Content);
			Assert.IsFalse(inner.CanGoBack);
			Assert.IsTrue(outer.CanGoBack);
			window.Close();
		});
	}

	#endregion
}