#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Sample.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Data;

[TestClass]
[SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
public class NotifiableTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AllPropertiesTracked()
	{
		var account = new Account();
		account.CreatedOn = DateTime.UtcNow;
		account.EmailAddress = "a@b.com";
		account.IsDeleted = true;
		account.LastLoginDate = DateTime.UtcNow;
		account.ModifiedOn = DateTime.UtcNow;
		account.Name = "John";
		account.Picture = "pic";
		account.Roles = "Admin";
		account.Status = AccountStatus.Enabled;
		account.SyncId = Guid.NewGuid();
		account.TimeZoneId = "UTC";

		var changed = account.GetChangedProperties().ToArray();

		// All 11 properties should be tracked
		AreEqual(11, changed.Length);

		// Verify alphabetical (bit) order
		for (var i = 1; i < changed.Length; i++)
		{
			IsTrue(string.Compare(changed[i - 1], changed[i], StringComparison.Ordinal) < 0,
				() => $"Expected '{changed[i - 1]}' < '{changed[i]}' in alphabetical order");
		}
	}

	[TestMethod]
	public void ApplyChangesTo()
	{
		var source = new Account();
		source.Name = "John";
		source.EmailAddress = "john@test.com";

		var destination = new Account
		{
			Name = "Original",
			EmailAddress = "original@test.com",
			Roles = "User"
		};
		destination.ResetHasChanges();

		source.ApplyChangesTo(destination);

		// Only changed properties on source should be applied
		AreEqual("John", destination.Name);
		AreEqual("john@test.com", destination.EmailAddress);

		// [Roles] was not changed on source, so it should remain unchanged
		AreEqual("User", destination.Roles);
	}

	[TestMethod]
	public void ApplyChangesToAfterIncrementalResets()
	{
		var source = new Account { Name = "Original", EmailAddress = "a@b.com", Roles = "User" };
		source.ResetHasChanges();

		// Only change Name after reset
		source.Name = "Updated";

		var destination = new Account { Name = "Dest", EmailAddress = "dest@b.com", Roles = "Admin" };
		destination.ResetHasChanges();

		source.ApplyChangesTo(destination);

		AreEqual("Updated", destination.Name);

		// EmailAddress and Roles should remain unchanged on destination
		AreEqual("dest@b.com", destination.EmailAddress);
		AreEqual("Admin", destination.Roles);
	}

	[TestMethod]
	public void BitIsIdempotentWhenPropertySetMultipleTimes()
	{
		var account = new Account();
		account.Name = "First";
		account.Name = "Second";
		account.Name = "Third";

		// The bit for Name should only appear once regardless of how many times it was set
		var changed = account.GetChangedProperties().ToArray();
		AreEqual(1, changed.Length);
		AreEqual("Name", changed[0]);
	}

	[TestMethod]
	public void BitIsolation()
	{
		var account = new Account();
		account.Name = "John";

		// Only the Name bit should be set; all other properties should not appear
		var changed = account.GetChangedProperties().ToArray();
		AreEqual(1, changed.Length);
		AreEqual("Name", changed[0]);
		IsFalse(changed.Contains("EmailAddress"));
		IsFalse(changed.Contains("Roles"));
		IsFalse(changed.Contains("CreatedOn"));
	}

	[TestMethod]
	public void DisableEnableCyclePreservesPriorBits()
	{
		var account = new Account();
		account.Name = "John";

		// Disable, change another property (won't be tracked), re-enable
		account.DisablePropertyChangeNotifications();
		account.EmailAddress = "john@test.com";
		account.EnablePropertyChangeNotifications();

		// The Name bit from before disable should still be present
		IsTrue(account.HasNotifiableChanges());
		var changed = account.GetChangedProperties().ToArray();
		AreEqual(1, changed.Length);
		AreEqual("Name", changed[0]);
	}

	[TestMethod]
	public void DisableNotificationsPreventsTracking()
	{
		var account = new Account();
		account.DisablePropertyChangeNotifications();

		account.Name = "John";

		// Bit should not be set because notifications are disabled
		IsFalse(account.HasNotifiableChanges());
		AreEqual([], account.GetChangedProperties());
	}

	[TestMethod]
	public void EnableNotificationsResumesTracking()
	{
		var account = new Account();
		account.DisablePropertyChangeNotifications();
		account.Name = "John";

		account.EnablePropertyChangeNotifications();
		account.EmailAddress = "john@test.com";

		IsTrue(account.HasNotifiableChanges());

		// Only EmailAddress should be tracked; Name was set while disabled
		var changed = account.GetChangedProperties().ToArray();
		AreEqual(1, changed.Length);
		AreEqual("EmailAddress", changed[0]);
	}

	[TestMethod]
	public void GetChangedPropertiesBitAssignmentMatchesAlphabeticalOrder()
	{
		// Verify the bit map lines up with the sorted property names
		var sourceType = SourceReflector.GetSourceType<Account>();
		var properties = sourceType.GetProperties()
			.Select(x => x.Name)
			.OrderBy(x => x)
			.ToArray();

		for (var i = 0; i < properties.Length; i++)
		{
			AreEqual(i, sourceType.GetPropertyBit(properties[i]));
			AreEqual(properties[i], sourceType.GetPropertyNameByBit(i));
		}
	}

	[TestMethod]
	public void GetChangedPropertiesIncludesInheritedProperties()
	{
		var account = new Account();

		// SyncModel properties
		account.CreatedOn = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		account.IsDeleted = true;

		// Account property
		account.Name = "John";

		var changed = account.GetChangedProperties().ToArray();
		AreEqual(3, changed.Length);
		IsTrue(changed.Contains("CreatedOn"));
		IsTrue(changed.Contains("IsDeleted"));
		IsTrue(changed.Contains("Name"));
	}

	[TestMethod]
	public void GetChangedPropertiesReturnsInBitOrder()
	{
		// Set properties in non-alphabetical order
		var account = new Account();
		account.TimeZoneId = "UTC";
		account.EmailAddress = "a@b.com";
		account.Name = "John";
		account.CreatedOn = DateTime.UtcNow;

		// GetChangedProperties walks bits low-to-high, which is alphabetical
		var changed = account.GetChangedProperties().ToArray();
		AreEqual(4, changed.Length);
		AreEqual("CreatedOn", changed[0]);
		AreEqual("EmailAddress", changed[1]);
		AreEqual("Name", changed[2]);
		AreEqual("TimeZoneId", changed[3]);
	}

	[TestMethod]
	public void HasNotifiableChanges()
	{
		var account = new Account();
		IsFalse(account.HasNotifiableChanges());

		account.Name = "John";
		IsTrue(account.HasNotifiableChanges());
		AreEqual(["Name"], account.GetChangedProperties());

		account.ResetHasChanges();
		IsFalse(account.HasNotifiableChanges());
		AreEqual([], account.GetChangedProperties());
	}

	[TestMethod]
	public void HasChangesMultipleProperties()
	{
		var account = new Account();
		account.Name = "John";
		account.EmailAddress = "john@test.com";
		account.Status = AccountStatus.Enabled;

		IsTrue(account.HasNotifiableChanges());

		// GetChangedProperties returns names in [bit] order (alphabetical)
		var changed = account.GetChangedProperties().ToArray();
		AreEqual(3, changed.Length);
		IsTrue(changed.Contains("EmailAddress"));
		IsTrue(changed.Contains("Name"));
		IsTrue(changed.Contains("Status"));
	}

	[TestMethod]
	public void HasChangesWithCombinedIncludeAndExcludeSettings()
	{
		var account = new Account();
		account.Name = "John";
		account.EmailAddress = "john@test.com";
		account.Roles = "Admin";

		// Include Name and Roles, but exclude Roles → only Name passes the filter
		var settings = new IncludeExcludeSettings(["Name", "Roles"], ["Roles"]);
		IsTrue(account.HasNotifiableChanges(settings));

		// Now reset and only change Roles (which is both included and excluded)
		account.ResetHasChanges();
		account.Roles = "User";
		IsFalse(account.HasNotifiableChanges(settings));
	}

	[TestMethod]
	public void HasChangesWithEmptySettings()
	{
		var account = new Account();
		account.Name = "John";

		// Empty settings should behave like the parameterless overload
		IsTrue(account.HasNotifiableChanges(IncludeExcludeSettings.Empty));
	}

	[TestMethod]
	public void HasChangesWithExcludeSettings()
	{
		var account = new Account();
		account.Name = "John";

		var excludeName = new IncludeExcludeSettings(null, ["Name"]);
		IsFalse(account.HasNotifiableChanges(excludeName));

		account.EmailAddress = "john@test.com";
		IsTrue(account.HasNotifiableChanges(excludeName));
	}

	[TestMethod]
	public void HasChangesWithIncludeSettings()
	{
		var account = new Account();
		account.Name = "John";
		account.EmailAddress = "john@test.com";

		var settings = new IncludeExcludeSettings(["Name"], null);
		IsTrue(account.HasNotifiableChanges(settings));

		// Only EmailAddress changed, but settings only include "Roles"
		account.ResetHasChanges();
		account.EmailAddress = "new@test.com";
		var rolesOnly = new IncludeExcludeSettings(["Roles"], null);
		IsFalse(account.HasNotifiableChanges(rolesOnly));
	}

	[TestMethod]
	public void HasChangesWithSettingsOnlyMatchesFilteredBits()
	{
		var account = new Account();
		account.Name = "John";
		account.EmailAddress = "john@test.com";
		account.Roles = "Admin";

		// Include only EmailAddress and Roles
		var settings = new IncludeExcludeSettings(["EmailAddress", "Roles"], null);
		IsTrue(account.HasNotifiableChanges(settings));

		// Reset, then change only Name (which is NOT in the include list)
		account.ResetHasChanges();
		account.Name = "Jane";
		IsFalse(account.HasNotifiableChanges(settings));
	}

	[TestMethod]
	public void IncrementalResetCycles()
	{
		var account = new Account();

		// Cycle 1: change Name
		account.Name = "John";
		AreEqual(["Name"], account.GetChangedProperties());
		account.ResetHasChanges();

		// Cycle 2: change EmailAddress only — Name bit must be gone
		account.EmailAddress = "john@test.com";
		var changed = account.GetChangedProperties().ToArray();
		AreEqual(1, changed.Length);
		AreEqual("EmailAddress", changed[0]);
		account.ResetHasChanges();

		// Cycle 3: change both
		account.Name = "Jane";
		account.Roles = "Admin";
		changed = account.GetChangedProperties().ToArray();
		AreEqual(2, changed.Length);
		IsTrue(changed.Contains("Name"));
		IsTrue(changed.Contains("Roles"));
	}

	[TestMethod]
	public void IsPropertyChangeNotificationsEnabled()
	{
		var account = new Account();
		IsTrue(account.IsPropertyChangeNotificationsEnabled());

		account.DisablePropertyChangeNotifications();
		IsFalse(account.IsPropertyChangeNotificationsEnabled());

		account.EnablePropertyChangeNotifications();
		IsTrue(account.IsPropertyChangeNotificationsEnabled());
	}

	[TestMethod]
	public void NotifyOfPropertyChangedDoesNotSetBit()
	{
		var account = new Account();

		// NotifyOfPropertyChanged fires the event but does not set the changed bit
		account.NotifyOfPropertyChanged(nameof(Account.Name));

		IsFalse(account.HasNotifiableChanges());
	}

	[TestMethod]
	public void PropertyChangedEventDoesNotFireWhenDisabled()
	{
		var account = new Account();
		var fired = false;
		account.PropertyChanged += (_, _) => fired = true;

		account.DisablePropertyChangeNotifications();
		account.Name = "John";

		IsFalse(fired);
	}

	[TestMethod]
	public void PropertyChangedEventFires()
	{
		var account = new Account();
		var firedProperties = new List<string>();
		account.PropertyChanged += (_, e) => firedProperties.Add(e.PropertyName);

		account.Name = "John";
		account.EmailAddress = "john@test.com";

		AreEqual(2, firedProperties.Count);
		IsTrue(firedProperties.Contains("Name"));
		IsTrue(firedProperties.Contains("EmailAddress"));
	}

	[TestMethod]
	public void PropertyChangingEventFires()
	{
		var account = new Account();
		var firingProperties = new List<string>();
		account.PropertyChanging += (_, e) => firingProperties.Add(e.PropertyName);

		account.Name = "John";

		AreEqual(1, firingProperties.Count);
		AreEqual("Name", firingProperties[0]);
	}

	[TestMethod]
	public void ResetHasChangesClearsAllBits()
	{
		var account = new Account();
		account.Name = "John";
		account.EmailAddress = "john@test.com";
		account.Status = AccountStatus.Enabled;
		account.Roles = "Admin";

		IsTrue(account.HasNotifiableChanges());
		account.ResetHasChanges();
		IsFalse(account.HasNotifiableChanges());
		AreEqual([], account.GetChangedProperties());
	}

	[TestMethod]
	public void SetSameValueDoesNotTrackChange()
	{
		var account = new Account();
		account.Name = "John";
		account.ResetHasChanges();

		// Setting the same value should not mark as changed
		account.Name = "John";
		IsFalse(account.HasNotifiableChanges());
		AreEqual([], account.GetChangedProperties());
	}

	#endregion
}