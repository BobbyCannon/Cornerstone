#region References

using System;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Animation.Easings;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ConnectedAnimationConfigurationTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BasicConfigIsConnectedAnimationConfiguration()
	{
		var config = new BasicConnectedAnimationConfiguration();
		CornerstoneTest.IsAssignableFrom<ConnectedAnimationConfiguration>(config);
	}

	[PresentationTestMethod]
	public void BasicConfigIsInstantiable()
	{
		var config = new BasicConnectedAnimationConfiguration();
		CornerstoneTest.IsNotNull(config);
	}

	[PresentationTestMethod]
	public void DirectConfigDurationCanBeSetToNull()
	{
		var config = new DirectConnectedAnimationConfiguration { Duration = TimeSpan.FromMilliseconds(100) };
		config.Duration = null;
		CornerstoneTest.IsNull(config.Duration);
	}

	[PresentationTestMethod]
	public void DirectConfigDurationDefaultIsNull()
	{
		var config = new DirectConnectedAnimationConfiguration();
		CornerstoneTest.IsNull(config.Duration);
	}

	[PresentationTestMethod]
	public void DirectConfigDurationRoundTrips()
	{
		var d = TimeSpan.FromMilliseconds(200);
		var config = new DirectConnectedAnimationConfiguration { Duration = d };
		CornerstoneTest.AreEqual(d, config.Duration);
	}

	[PresentationTestMethod]
	public void DirectConfigIsConnectedAnimationConfiguration()
	{
		var config = new DirectConnectedAnimationConfiguration();
		CornerstoneTest.IsAssignableFrom<ConnectedAnimationConfiguration>(config);
	}

	[PresentationTestMethod]
	public void GravityConfigIsConnectedAnimationConfiguration()
	{
		var config = new GravityConnectedAnimationConfiguration();
		CornerstoneTest.IsAssignableFrom<ConnectedAnimationConfiguration>(config);
	}

	[PresentationTestMethod]
	public void GravityConfigIsShadowEnabledCanBeSetFalse()
	{
		var config = new GravityConnectedAnimationConfiguration { IsShadowEnabled = false };
		CornerstoneTest.IsFalse(config.IsShadowEnabled);
	}

	[PresentationTestMethod]
	public void GravityConfigIsShadowEnabledDefaultIsTrue()
	{
		var config = new GravityConnectedAnimationConfiguration();
		CornerstoneTest.IsTrue(config.IsShadowEnabled);
	}

	[PresentationTestMethod]
	public void GravityConfigIsShadowEnabledRoundTrips()
	{
		var config = new GravityConnectedAnimationConfiguration { IsShadowEnabled = true };
		CornerstoneTest.IsTrue(config.IsShadowEnabled);
		config.IsShadowEnabled = false;
		CornerstoneTest.IsFalse(config.IsShadowEnabled);
	}

	#endregion
}

[TestClass]
public class ConnectedAnimationServiceTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void DefaultDurationCanBeSet()
	{
		var service = CreateService();
		var d = TimeSpan.FromMilliseconds(500);
		service.DefaultDuration = d;
		CornerstoneTest.AreEqual(d, service.DefaultDuration);
	}

	[PresentationTestMethod]
	public void DefaultDurationInitiallyIs300ms()
	{
		var service = CreateService();
		CornerstoneTest.AreEqual(TimeSpan.FromMilliseconds(300), service.DefaultDuration);
	}

	[PresentationTestMethod]
	public void DefaultEasingFunctionCanBeSet()
	{
		var service = CreateService();
		var easing = new LinearEasing();
		service.DefaultEasingFunction = easing;
		CornerstoneTest.Same(easing, service.DefaultEasingFunction);
	}

	[PresentationTestMethod]
	public void DefaultEasingFunctionInitiallyNull()
	{
		var service = CreateService();
		CornerstoneTest.IsNull(service.DefaultEasingFunction);
	}

	[PresentationTestMethod]
	public void GetAnimationAfterPrepareReturnsAnimation()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		CornerstoneTest.Same(animation, service.GetAnimation("hero"));
	}

	[PresentationTestMethod]
	public void GetAnimationReturnsSameInstanceForSameKey()
	{
		var service = CreateService();
		service.PrepareToAnimate("hero", new Border());
		var a1 = service.GetAnimation("hero");
		var a2 = service.GetAnimation("hero");
		CornerstoneTest.Same(a1, a2);
	}

	[PresentationTestMethod]
	public void GetAnimationUnknownKeyReturnsNull()
	{
		var service = CreateService();
		CornerstoneTest.IsNull(service.GetAnimation("nonexistent"));
	}

	[PresentationTestMethod]
	public void GetForTopLevelNullTopLevelThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() =>
			ConnectedAnimationService.GetForTopLevel(null!));
	}

	[PresentationTestMethod]
	public void PrepareToAnimateAnimationInitiallyNotConsumed()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		CornerstoneTest.IsFalse(animation.IsConsumed);
	}

	[PresentationTestMethod]
	public void PrepareToAnimateDifferentKeysBothInService()
	{
		var service = CreateService();
		var a1 = service.PrepareToAnimate("key1", new Border());
		var a2 = service.PrepareToAnimate("key2", new Border());

		CornerstoneTest.Same(a1, service.GetAnimation("key1"));
		CornerstoneTest.Same(a2, service.GetAnimation("key2"));
	}

	[PresentationTestMethod]
	public void PrepareToAnimateEmptyKeyThrowsArgumentException()
	{
		var service = CreateService();
		var source = new Border();
		Assert.Throws<ArgumentException>(() =>
			service.PrepareToAnimate(string.Empty, source));
	}

	[PresentationTestMethod]
	public void PrepareToAnimateNullKeyThrowsArgumentNullException()
	{
		var service = CreateService();
		var source = new Border();
		Assert.Throws<ArgumentNullException>(() =>
			service.PrepareToAnimate(null!, source));
	}

	[PresentationTestMethod]
	public void PrepareToAnimateNullSourceThrowsArgumentNullException()
	{
		var service = CreateService();
		Assert.Throws<ArgumentNullException>(() =>
			service.PrepareToAnimate("key", null!));
	}

	[PresentationTestMethod]
	public void PrepareToAnimateReturnsAnimationWithMatchingKey()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		CornerstoneTest.AreEqual("hero", animation.Key);
	}

	[PresentationTestMethod]
	public void PrepareToAnimateSameKeyReplacesOldAnimation()
	{
		var service = CreateService();
		var first = service.PrepareToAnimate("hero", new Border());
		var second = service.PrepareToAnimate("hero", new Border());

		CornerstoneTest.NotSame(first, second);
		CornerstoneTest.IsTrue(first.IsConsumed || first.IsDisposed);
		CornerstoneTest.Same(second, service.GetAnimation("hero"));
	}

	private static ConnectedAnimationService CreateService()
	{
		return new();
	}

	#endregion
}

[TestClass]
public class ConnectedAnimationTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ConfigurationBasicRoundTrips()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		var config = new BasicConnectedAnimationConfiguration();
		animation.Configuration = config;
		CornerstoneTest.Same(config, animation.Configuration);
	}

	[PresentationTestMethod]
	public void ConfigurationBasicUseGravityDipIsFalse()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		animation.Configuration = new BasicConnectedAnimationConfiguration();

		animation.ResolveTimingAndEasing(service, out _, out _, out var useGravityDip, out _);

		CornerstoneTest.IsFalse(useGravityDip);
	}

	[PresentationTestMethod]
	public void ConfigurationBasicUsesServiceDefaultDuration()
	{
		var service = CreateService();
		service.DefaultDuration = TimeSpan.FromMilliseconds(400);
		var animation = service.PrepareToAnimate("hero", new Border());
		animation.Configuration = new BasicConnectedAnimationConfiguration();

		animation.ResolveTimingAndEasing(service, out var resolvedDuration, out _, out _, out _);

		CornerstoneTest.AreEqual(TimeSpan.FromMilliseconds(400), resolvedDuration);
	}

	[PresentationTestMethod]
	public void ConfigurationDirectRoundTrips()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		var config = new DirectConnectedAnimationConfiguration { Duration = TimeSpan.FromMilliseconds(100) };
		animation.Configuration = config;
		CornerstoneTest.Same(config, animation.Configuration);
	}

	[PresentationTestMethod]
	public void ConfigurationDirectUseGravityDipIsFalse()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		animation.Configuration = new DirectConnectedAnimationConfiguration();

		animation.ResolveTimingAndEasing(service, out _, out _, out var useGravityDip, out _);

		CornerstoneTest.IsFalse(useGravityDip);
	}

	// ResolveTimingAndEasing tests — no reflection needed, method is internal.

	[PresentationTestMethod]
	public void ConfigurationDirectWithDurationUsesProvidedDuration()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		animation.Configuration = new DirectConnectedAnimationConfiguration
		{
			Duration = TimeSpan.FromMilliseconds(250)
		};

		animation.ResolveTimingAndEasing(service, out var resolvedDuration, out _, out _, out _);

		CornerstoneTest.AreEqual(TimeSpan.FromMilliseconds(250), resolvedDuration);
	}

	[PresentationTestMethod]
	public void ConfigurationDirectWithNullDurationUsesServiceDefaultDuration()
	{
		var service = CreateService();
		service.DefaultDuration = TimeSpan.FromMilliseconds(400);
		var animation = service.PrepareToAnimate("hero", new Border());
		animation.Configuration = new DirectConnectedAnimationConfiguration { Duration = null };

		animation.ResolveTimingAndEasing(service, out var resolvedDuration, out _, out _, out _);

		CornerstoneTest.AreEqual(TimeSpan.FromMilliseconds(400), resolvedDuration);
	}

	[PresentationTestMethod]
	public void ConfigurationGravityIsShadowEnabledFalseUseShadowIsFalse()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		animation.Configuration = new GravityConnectedAnimationConfiguration { IsShadowEnabled = false };

		animation.ResolveTimingAndEasing(service, out _, out _, out _, out var useShadow);

		CornerstoneTest.IsFalse(useShadow);
	}

	[PresentationTestMethod]
	public void ConfigurationGravityIsShadowEnabledTrueUseShadowIsTrue()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		animation.Configuration = new GravityConnectedAnimationConfiguration { IsShadowEnabled = true };

		animation.ResolveTimingAndEasing(service, out _, out _, out _, out var useShadow);

		CornerstoneTest.IsTrue(useShadow);
	}

	[PresentationTestMethod]
	public void ConfigurationGravityRoundTrips()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		var config = new GravityConnectedAnimationConfiguration { IsShadowEnabled = false };
		animation.Configuration = config;
		CornerstoneTest.Same(config, animation.Configuration);
	}

	[PresentationTestMethod]
	public void ConfigurationGravityUseGravityDipIsTrue()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		animation.Configuration = new GravityConnectedAnimationConfiguration();

		animation.ResolveTimingAndEasing(service, out _, out _, out var useGravityDip, out _);

		CornerstoneTest.IsTrue(useGravityDip);
	}

	[PresentationTestMethod]
	public void ConfigurationGravityUsesServiceDefaultDuration()
	{
		var service = CreateService();
		service.DefaultDuration = TimeSpan.FromMilliseconds(350);
		var animation = service.PrepareToAnimate("hero", new Border());
		animation.Configuration = new GravityConnectedAnimationConfiguration();

		animation.ResolveTimingAndEasing(service, out var resolvedDuration, out _, out _, out _);

		CornerstoneTest.AreEqual(TimeSpan.FromMilliseconds(350), resolvedDuration);
	}

	[PresentationTestMethod]
	public void ConfigurationInitiallyNull()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		CornerstoneTest.IsNull(animation.Configuration);
	}

	[PresentationTestMethod]
	public void DisposeCalledTwiceIsNoOp()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());

		animation.Dispose();
		animation.Dispose(); // must not throw
	}

	[PresentationTestMethod]
	public void DisposeRemovesFromService()
	{
		var service = CreateService();
		service.PrepareToAnimate("hero", new Border());
		CornerstoneTest.IsNotNull(service.GetAnimation("hero"));

		service.GetAnimation("hero")!.Dispose();

		CornerstoneTest.IsNull(service.GetAnimation("hero"));
	}

	[PresentationTestMethod]
	public void DisposeWhenNotMidFlightDoesNotFireCompleted()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());

		var fired = false;
		animation.Completed += (_, _) => fired = true;

		animation.Dispose();

		CornerstoneTest.IsFalse(fired);
	}

	[PresentationTestMethod]
	public void GetAnimationReturnsNullAfterDispose()
	{
		var service = CreateService();
		service.PrepareToAnimate("hero", new Border());
		service.GetAnimation("hero")!.Dispose();
		CornerstoneTest.IsNull(service.GetAnimation("hero"));
	}

	[PresentationTestMethod]
	public void GetAnimationReturnsNullAfterTryStart()
	{
		var service = CreateService();
		service.PrepareToAnimate("hero", new Border());
		service.GetAnimation("hero")!.TryStart(new Border());
		CornerstoneTest.IsNull(service.GetAnimation("hero"));
	}

	[PresentationTestMethod]
	public void GetAnimationReturnsNullWhenAnimationIsConsumed()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		animation.TryStart(new Border());
		CornerstoneTest.IsNull(service.GetAnimation("hero"));
	}

	[PresentationTestMethod]
	public void IsConsumedInitiallyFalse()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		CornerstoneTest.IsFalse(animation.IsConsumed);
	}

	[PresentationTestMethod]
	public void KeyMatchesPreparedKey()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("myKey", new Border());
		CornerstoneTest.AreEqual("myKey", animation.Key);
	}

	[PresentationTestMethod]
	public void MultipleAnimationsIndependentLifecycles()
	{
		var service = CreateService();
		var a1 = service.PrepareToAnimate("anim1", new Border());
		var a2 = service.PrepareToAnimate("anim2", new Border());

		a1.TryStart(new Border());

		CornerstoneTest.IsTrue(a1.IsConsumed);
		CornerstoneTest.IsFalse(a2.IsConsumed);
		CornerstoneTest.IsNull(service.GetAnimation("anim1"));
		CornerstoneTest.IsNotNull(service.GetAnimation("anim2"));
	}

	[PresentationTestMethod]
	public void PrepareToAnimateAfterDisposeCanPrepareAgain()
	{
		var service = CreateService();
		var first = service.PrepareToAnimate("hero", new Border());
		first.Dispose();

		var second = service.PrepareToAnimate("hero", new Border());
		CornerstoneTest.NotSame(first, second);
		CornerstoneTest.Same(second, service.GetAnimation("hero"));
	}

	[PresentationTestMethod]
	public void TryStartConsumesAnimation()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		animation.TryStart(new Border());
		CornerstoneTest.IsTrue(animation.IsConsumed);
	}

	[PresentationTestMethod]
	public void TryStartReturnsFalseWhenDisposed()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		animation.Dispose();
		var result = animation.TryStart(new Border());
		CornerstoneTest.IsFalse(result);
	}

	[PresentationTestMethod]
	public void TryStartReturnsTrueWhenNotConsumed()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		var result = animation.TryStart(new Border());
		CornerstoneTest.IsTrue(result);
	}

	[PresentationTestMethod]
	public void TryStartSecondCallReturnsFalse()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		animation.TryStart(new Border());
		var second = animation.TryStart(new Border());
		CornerstoneTest.IsFalse(second);
	}

	[PresentationTestMethod]
	public void TryStartWhenNoTopLevelCompletedNotCancelled()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());

		bool? cancelled = null;
		animation.Completed += (_, e) => cancelled = e.Cancelled;

		animation.TryStart(new Border());

		CornerstoneTest.IsFalse(cancelled);
	}

	[PresentationTestMethod]
	public void TryStartWhenNoTopLevelFiresCompleted()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());

		ConnectedAnimationCompletedEventArgs received = null;
		animation.Completed += (_, e) => received = e;

		animation.TryStart(new Border()); // no TopLevel ancestor

		CornerstoneTest.IsNotNull(received);
	}

	[PresentationTestMethod]
	public void TryStartWhenNoTopLevelRemovesFromService()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());

		animation.TryStart(new Border());

		CornerstoneTest.IsNull(service.GetAnimation("hero"));
	}

	[PresentationTestMethod]
	public void TryStartWithCoordinatedElementsWhenNoTopLevelFiresCompleted()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		var coordinated = new Visual[] { new Border(), new Border() };

		var fired = false;
		animation.Completed += (_, _) => fired = true;

		animation.TryStart(new Border(), coordinated);

		CornerstoneTest.IsTrue(fired);
	}

	[PresentationTestMethod]
	public void TryStartWithEmptyCoordinatedElementsReturnsTrue()
	{
		var service = CreateService();
		var animation = service.PrepareToAnimate("hero", new Border());
		var result = animation.TryStart(new Border(), Array.Empty<Visual>());
		CornerstoneTest.IsTrue(result);
	}

	private static ConnectedAnimationService CreateService()
	{
		return new();
	}

	#endregion
}