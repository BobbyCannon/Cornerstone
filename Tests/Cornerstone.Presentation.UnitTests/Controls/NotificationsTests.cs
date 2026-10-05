#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Notifications;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notification = Cornerstone.Presentation.Controls.Notifications.Notification;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class WindowNotificationManagerTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ShowAndCloseAllNotifications()
	{
		WindowNotificationManager manager = new();

		manager.Show("Notification 1");
		manager.Show("Notification 2");

		CornerstoneTest.AreEqual(2, manager.Notifications.Count());

		manager.CloseAll();

		CornerstoneTest.IsTrue(!manager.Notifications.Any(x => !x.IsClosing));
	}

	[PresentationTestMethod]
	public void ShowAndCloseNotification()
	{
		WindowNotificationManager manager = new();

		manager.Show("Notification text");

		CornerstoneTest.AreEqual(1, manager.Notifications.Count());

		manager.Close("Notification text");

		CornerstoneTest.IsTrue(!manager.Notifications.Any(x => !x.IsClosing));
	}

	[PresentationTestMethod]
	public void ShowNotificationsWithSameString()
	{
		WindowNotificationManager manager = new();

		manager.Show("Notification text");
		manager.Show("Notification text");
		manager.Show("Notification text");

		CornerstoneTest.AreEqual(3, manager.Notifications.Count());
	}

	#endregion
}

[TestClass]
public class INotificationManagerTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ShowAndCloseAllNotifications()
	{
		INotificationManager manager = new WindowNotificationManager();

		Notification notification1 = new()
		{
			Message = "Notification text"
		};

		Notification notification2 = new()
		{
			Message = "Notification text"
		};

		manager.Show(notification1);
		manager.Show(notification2);

		CornerstoneTest.AreEqual(2, ((WindowNotificationManager) manager).Notifications.Count());

		manager.CloseAll();

		CornerstoneTest.IsTrue(!((WindowNotificationManager) manager).Notifications.Any(x => !x.IsClosing));
	}

	[PresentationTestMethod]
	public void ShowAndCloseNotification()
	{
		INotificationManager manager = new WindowNotificationManager();

		Notification notification = new()
		{
			Message = "Notification text"
		};

		manager.Show(notification);

		CornerstoneTest.AreEqual(1, ((WindowNotificationManager) manager).Notifications.Count());

		manager.Close(notification);

		CornerstoneTest.IsTrue(!((WindowNotificationManager) manager).Notifications.Any(x => !x.IsClosing));
	}

	[PresentationTestMethod]
	public void ShowNotificationsWithSameContent()
	{
		INotificationManager manager = new WindowNotificationManager();

		Notification notification = new()
		{
			Message = "Notification text"
		};

		manager.Show(notification);
		manager.Show(notification);
		manager.Show(notification);

		CornerstoneTest.AreEqual(3, ((WindowNotificationManager) manager).Notifications.Count());
	}

	#endregion
}

[TestClass]
public class NotificationCardTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldUpdatePseudoclassesWhenNotificationTypeChanges()
	{
		var target = new NotificationCard();

		target.NotificationType = NotificationType.Error;
		AssertPseudoClasses(target, ":error");

		target.NotificationType = NotificationType.Information;
		AssertPseudoClasses(target, ":information");

		target.NotificationType = NotificationType.Success;
		AssertPseudoClasses(target, ":success");

		target.NotificationType = NotificationType.Warning;
		AssertPseudoClasses(target, ":warning");

		static void AssertPseudoClasses(NotificationCard target, string expected)
		{
			CornerstoneTest.Contains(target.Classes, expected);

			foreach (var pseudoclass in new[] { ":error", ":information", ":success", ":warning" }.Where(x => x != expected))
			{
				CornerstoneTest.DoesNotContain(target.Classes, pseudoclass);
			}
		}
	}

	#endregion
}