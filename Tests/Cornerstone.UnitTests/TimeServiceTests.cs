#region References

using System;
using System.Collections.Generic;
using System.Reflection;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests;

[TestClass]
public class TimeServiceTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Reset()
	{
		TimeService.PushProvider(this);
		AreEqual(StartDateTime, TimeService.CurrentTime.UtcNow);
		AreNotEqual(StartDateTime, TimeService.RealTime.UtcNow);

		TimeService.Reset();
		AreNotEqual(StartDateTime, TimeService.CurrentTime.UtcNow);
		AreNotEqual(StartDateTime, TimeService.RealTime.UtcNow);
	}

	[TestMethod]
	public void CanPopAndRemoveProviders()
	{
		TimeService.Reset();

		var start = TimeService.CurrentTime.UtcNow;
		var provider1 = new DateTimeProvider(
			Guid.Parse("2B5ED0B5-7ED5-4613-88F2-389F6835958C"),
			new DateTime(2022, 08, 26, 02, 44, 00, DateTimeKind.Utc)
		);
		var provider2 = new DateTimeProvider(
			Guid.Parse("16F0EE1E-2EEF-41D7-AA2C-102439A0CCD9"),
			new DateTime(2022, 08, 26, 02, 44, 01, DateTimeKind.Utc)
		);
		var provider3 = new DateTimeProvider(
			Guid.Parse("E5F864BB-1578-4D21-9475-C166F876485F"),
			new DateTime(2022, 08, 26, 02, 44, 02, DateTimeKind.Utc)
		);

		TimeService.PushProvider(provider1);
		var expected = new DateTime(2022, 08, 26, 02, 44, 00, DateTimeKind.Utc);
		AreEqual(provider1.GetProviderId(), TimeService.CurrentTime.GetProviderId());
		AreEqual(expected.ToLocalTime(), TimeService.CurrentTime.Now);
		AreEqual(expected, TimeService.CurrentTime.UtcNow);

		TimeService.PushProvider(provider2);
		expected = new DateTime(2022, 08, 26, 02, 44, 01, DateTimeKind.Utc);
		AreEqual(provider2.GetProviderId(), TimeService.CurrentTime.GetProviderId());
		AreEqual(expected.ToLocalTime(), TimeService.CurrentTime.Now);
		AreEqual(expected, TimeService.CurrentTime.UtcNow);

		TimeService.PushProvider(provider3);
		expected = new DateTime(2022, 08, 26, 02, 44, 02, DateTimeKind.Utc);
		AreEqual(provider3.GetProviderId(), TimeService.CurrentTime.GetProviderId());
		AreEqual(expected.ToLocalTime(), TimeService.CurrentTime.Now);
		AreEqual(expected, TimeService.CurrentTime.UtcNow);
		
		expected = new DateTime(2022, 08, 26, 02, 44, 03, DateTimeKind.Utc);
		TimeService.PushProvider(() => expected);
		AreNotEqual(provider3.GetProviderId(), TimeService.CurrentTime.GetProviderId());
		AreEqual(expected.ToLocalTime(), TimeService.CurrentTime.Now);
		AreEqual(expected, TimeService.CurrentTime.UtcNow);
		TimeService.PopProvider();

		AreEqual(provider3.UtcNow, TimeService.CurrentTime.UtcNow);
		TimeService.PopProvider();

		// Now that provider 3 should be off the stack, we should fall back to provider 2
		AreEqual(provider2.UtcNow, TimeService.CurrentTime.UtcNow);

		// Now that provider 1 should be off the stack, we should still be on provider 2
		TimeService.RemoveProvider(provider1.GetProviderId());
		AreEqual(provider2.GetProviderId(), TimeService.CurrentTime.GetProviderId());
		AreEqual(provider2.UtcNow, TimeService.CurrentTime.UtcNow);

		// Remove the last provider
		TimeService.RemoveProvider(provider2);
		Assert.IsTrue(TimeService.CurrentTime.UtcNow > start);
		AreEqual(Guid.Parse("48E21BDA-9E7A-4767-8E3B-B218203C9A71"), TimeService.CurrentTime.GetProviderId());
	}

	[TestMethod]
	public void LockInitialize()
	{
		try
		{
			// Lock the service
			TimeService.LockService();

			var scenarios = new Action[]
			{
				() => TimeService.PushProvider(() => new DateTime(2022, 12, 16)),
				() => TimeService.PushProvider(() => new DateTime(2022, 12, 16)),
				() => TimeService.PushProvider(new DateTimeProvider(Guid.NewGuid(), StartDateTime)),
				() => TimeService.RemoveProvider(Guid.NewGuid()),
				() => TimeService.RemoveProvider(new DateTimeProvider(Guid.NewGuid(), StartDateTime)),
				TimeService.PopProvider,
				TimeService.PopProvider,
				() => TimeService.Reset()
			};

			foreach (var scenario in scenarios)
			{
				ExpectedException<InvalidOperationException>(scenario, Babel.Tower[BabelKeys.TimeServiceLocked]);
			}
		}
		finally
		{
			// Reach in and reset the lock, yes this is possible...
			var field = typeof(TimeService).GetField("_locked", BindingFlags.Static | BindingFlags.NonPublic);
			field?.SetValue(null, false);
		}
	}

	[TestMethod]
	public void RandomRemoves()
	{
		var current = TimeService.CurrentTime.UtcNow;
		var start = new DateTime(2022, 08, 26, 02, 44, 00, DateTimeKind.Utc);
		var ids = new List<Guid>(1000);

		for (var i = 0; i < ids.Count; i++)
		{
			var providerDateTime = start.AddSeconds(i);
			var p = TimeService.PushProvider(() => providerDateTime);
			ids.Add(p.GetProviderId());
		}

		var random = new Random();

		while (ids.Count > 0)
		{
			var actual = TimeService.CurrentTime.UtcNow;
			Assert.IsTrue(actual < current);

			var randomIndex = random.Next(0, ids.Count);
			var providerId = ids[randomIndex];

			TimeService.RemoveProvider(providerId);
		}
	}

	#endregion
}