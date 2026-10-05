#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ContentPageTests
{
	#region Classes

	[TestClass]
	public class CommandBarPropertyTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void BottomCommandBarDefaultIsNull()
		{
			var page = new ContentPage();
			CornerstoneTest.IsNull(page.BottomCommandBar);
		}

		[PresentationTestMethod]
		public void BottomCommandBarRoundTrips()
		{
			var bar = new Button();
			var page = new ContentPage { BottomCommandBar = bar };
			CornerstoneTest.Same(bar, page.BottomCommandBar);
		}

		[PresentationTestMethod]
		public void TopCommandBarAndBottomCommandBarAreIndependent()
		{
			var top = new Button();
			var bottom = new TextBlock();
			var page = new ContentPage { TopCommandBar = top, BottomCommandBar = bottom };
			CornerstoneTest.Same(top, page.TopCommandBar);
			CornerstoneTest.Same(bottom, page.BottomCommandBar);
		}

		[PresentationTestMethod]
		public void TopCommandBarDefaultIsNull()
		{
			var page = new ContentPage();
			CornerstoneTest.IsNull(page.TopCommandBar);
		}

		[PresentationTestMethod]
		public void TopCommandBarRoundTrips()
		{
			var bar = new Button();
			var page = new ContentPage { TopCommandBar = bar };
			CornerstoneTest.Same(bar, page.TopCommandBar);
		}

		#endregion
	}

	[TestClass]
	public class ContentPropertyTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void AutomaticallyApplySafeAreaPaddingDefaultIsTrue()
		{
			var page = new ContentPage();
			CornerstoneTest.IsTrue(page.AutomaticallyApplySafeAreaPadding);
		}

		[PresentationTestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public void AutomaticallyApplySafeAreaPaddingRoundTrips(bool value)
		{
			var page = new ContentPage { AutomaticallyApplySafeAreaPadding = value };
			CornerstoneTest.AreEqual(value, page.AutomaticallyApplySafeAreaPadding);
		}

		[PresentationTestMethod]
		public void ContentDefaultIsNull()
		{
			var page = new ContentPage();
			CornerstoneTest.IsNull(page.Content);
		}

		[PresentationTestMethod]
		[DataRow(typeof(ContentPage))]
		[DataRow(typeof(NavigationPage))]
		[DataRow(typeof(TabbedPage))]
		public void ContentSetAnyPageSubtypeThrowsInvalidOperationException(Type pageType)
		{
			var host = new ContentPage();
			var child = (Page) Activator.CreateInstance(pageType)!;
			Assert.Throws<InvalidOperationException>(() => host.Content = child);
		}

		[PresentationTestMethod]
		public void ContentSetControlRoundTrips()
		{
			var ctrl = new Button();
			var page = new ContentPage { Content = ctrl };
			CornerstoneTest.Same(ctrl, page.Content);
		}

		[PresentationTestMethod]
		public void ContentSetStringRoundTrips()
		{
			var page = new ContentPage { Content = "Hello" };
			CornerstoneTest.AreEqual("Hello", page.Content);
		}

		[PresentationTestMethod]
		public void ContentTemplateDefaultIsNull()
		{
			var page = new ContentPage();
			CornerstoneTest.IsNull(page.ContentTemplate);
		}

		[PresentationTestMethod]
		public void ContentTemplateRoundTrips()
		{
			var template = new FuncDataTemplate<object>((_, _) => null);
			var page = new ContentPage { ContentTemplate = template };
			CornerstoneTest.Same(template, page.ContentTemplate);
		}

		[PresentationTestMethod]
		public void HorizontalContentAlignmentDefaultIsStretch()
		{
			var page = new ContentPage();
			CornerstoneTest.AreEqual(HorizontalAlignment.Stretch, page.HorizontalContentAlignment);
		}

		[PresentationTestMethod]
		[DataRow(HorizontalAlignment.Left)]
		[DataRow(HorizontalAlignment.Center)]
		[DataRow(HorizontalAlignment.Right)]
		[DataRow(HorizontalAlignment.Stretch)]
		public void HorizontalContentAlignmentRoundTrips(HorizontalAlignment value)
		{
			var page = new ContentPage { HorizontalContentAlignment = value };
			CornerstoneTest.AreEqual(value, page.HorizontalContentAlignment);
		}

		[PresentationTestMethod]
		public void VerticalContentAlignmentDefaultIsStretch()
		{
			var page = new ContentPage();
			CornerstoneTest.AreEqual(VerticalAlignment.Stretch, page.VerticalContentAlignment);
		}

		[PresentationTestMethod]
		[DataRow(VerticalAlignment.Top)]
		[DataRow(VerticalAlignment.Center)]
		[DataRow(VerticalAlignment.Bottom)]
		[DataRow(VerticalAlignment.Stretch)]
		public void VerticalContentAlignmentRoundTrips(VerticalAlignment value)
		{
			var page = new ContentPage { VerticalContentAlignment = value };
			CornerstoneTest.AreEqual(value, page.VerticalContentAlignment);
		}

		#endregion
	}

	[TestClass]
	public class LogicalChildrenTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void ContentReplacedOldChildRemovedNewChildAdded()
		{
			var page = new ContentPage();
			var first = new Button();
			var second = new TextBlock();
			page.Content = first;
			page.Content = second;

			CornerstoneTest.DoesNotContain(((ILogical) page).LogicalChildren, first);
			CornerstoneTest.Contains(((ILogical) page).LogicalChildren, second);
		}

		[PresentationTestMethod]
		public void ContentSetControlAddsToLogicalChildren()
		{
			var page = new ContentPage();
			var child = new Button();
			page.Content = child;
			CornerstoneTest.Contains(((ILogical) page).LogicalChildren, child);
		}

		[PresentationTestMethod]
		public void ContentSetToNullRemovesOldChild()
		{
			var page = new ContentPage();
			var child = new Button();
			page.Content = child;
			page.Content = null;

			CornerstoneTest.DoesNotContain(((ILogical) page).LogicalChildren, child);
		}

		[PresentationTestMethod]
		public void ContentSetToStringDoesNotAddToLogicalChildren()
		{
			var page = new ContentPage { Content = "hello" };
			CornerstoneTest.Empty(((ILogical) page).LogicalChildren);
		}

		#endregion
	}

	[TestClass]
	public class PageDefaults : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CurrentPageDefaultIsNull()
		{
			var page = new ContentPage();
			CornerstoneTest.IsNull(page.CurrentPage);
		}

		[PresentationTestMethod]
		public void HeaderDefaultIsNull()
		{
			var page = new ContentPage();
			CornerstoneTest.IsNull(page.Header);
		}

		[PresentationTestMethod]
		public void HeaderSetControlRoundTrips()
		{
			var label = new TextBlock { Text = "Custom Title" };
			var page = new ContentPage { Header = label };
			CornerstoneTest.Same(label, page.Header);
		}

		[PresentationTestMethod]
		[DataRow("Home")]
		[DataRow("Settings")]
		[DataRow("")]
		public void HeaderSetStringRoundTrips(string title)
		{
			var page = new ContentPage { Header = title };
			CornerstoneTest.AreEqual(title, page.Header);
		}

		[PresentationTestMethod]
		public void IconDefaultIsNull()
		{
			var page = new ContentPage();
			CornerstoneTest.IsNull(page.Icon);
		}

		[PresentationTestMethod]
		public void IconRoundTrips()
		{
			var icon = new Image();
			var page = new ContentPage { Icon = icon };
			CornerstoneTest.Same(icon, page.Icon);
		}

		[PresentationTestMethod]
		public void IsInNavigationPageDefaultIsFalse()
		{
			var page = new ContentPage();
			CornerstoneTest.IsFalse(page.IsInNavigationPage);
		}

		[PresentationTestMethod]
		public void NavigationDefaultIsNull()
		{
			var page = new ContentPage();
			CornerstoneTest.IsNull(page.Navigation);
		}

		[PresentationTestMethod]
		public void SafeAreaPaddingDefaultIsZero()
		{
			var page = new ContentPage();
			CornerstoneTest.AreEqual(default, page.SafeAreaPadding);
		}

		[PresentationTestMethod]
		public void SafeAreaPaddingRoundTrips()
		{
			var page = new ContentPage();
			var padding = new Thickness(10, 20, 10, 30);
			page.SafeAreaPadding = padding;
			CornerstoneTest.AreEqual(padding, page.SafeAreaPadding);
		}

		#endregion
	}

	[TestClass]
	public class SystemBackButtonTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void OnSystemBackButtonPressedDefaultReturnsFalse()
		{
			var page = new TestableContentPage();
			CornerstoneTest.IsFalse(page.CallOnSystemBackButtonPressed());
		}

		[PresentationTestMethod]
		public void OnSystemBackButtonPressedOverrideReturnsTrue()
		{
			var page = new BackButtonHandlingPage();
			CornerstoneTest.IsTrue(page.CallOnSystemBackButtonPressed());
		}

		#endregion

		#region Classes

		private class BackButtonHandlingPage : ContentPage
		{
			#region Methods

			public bool CallOnSystemBackButtonPressed()
			{
				return OnSystemBackButtonPressed();
			}

			protected override bool OnSystemBackButtonPressed()
			{
				return true;
			}

			#endregion
		}

		private class TestableContentPage : ContentPage
		{
			#region Methods

			public bool CallOnSystemBackButtonPressed()
			{
				return OnSystemBackButtonPressed();
			}

			#endregion
		}

		#endregion
	}

	#endregion
}