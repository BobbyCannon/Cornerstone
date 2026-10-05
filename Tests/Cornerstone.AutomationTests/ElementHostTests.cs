#region References

using System;
using System.Diagnostics;
using Cornerstone.Input;
using Cornerstone.Platforms.Windows;
using Cornerstone.Runtime;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.AutomationTests;

[TestClass]
public class ElementHostTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void FirstOrDefaultDoesNotWaitWhenWaitIsFalse()
	{
		using var host = new TestElementHost();
		var watch = Stopwatch.StartNew();

		var missing = host.FirstOrDefault("missing", wait: false);
		watch.Stop();

		IsNull(missing);
		AreEqual(0, host.RefreshCount);
		IsTrue(watch.ElapsedMilliseconds < 100);
	}

	[TestMethod]
	public void FirstOrDefaultFindsByIdNameAndAutomationId()
	{
		using var host = new TestElementHost();
		host.Children.Add(new TestElement("id1", host, name: "name1", automationId: "auto1"));

		AreEqual("id1", host.FirstOrDefault("id1", wait: false).Id);
		AreEqual("id1", host.FirstOrDefault("name1", wait: false).Id);
		AreEqual("id1", host.FirstOrDefault("auto1", wait: false).Id);
		AreEqual("id1", host.First("auto1", wait: false).Id);
	}

	[TestMethod]
	public void FirstOrDefaultUsesSearchTimeoutWhenWaiting()
	{
		using var host = new TestElementHost { SearchTimeout = TimeSpan.FromMilliseconds(200) };
		var watch = Stopwatch.StartNew();

		var missing = host.FirstOrDefault("missing");
		watch.Stop();

		IsNull(missing);
		IsTrue(host.RefreshCount > 0);
		IsTrue(watch.ElapsedMilliseconds >= 150);
		IsTrue(watch.ElapsedMilliseconds < 800);
	}

	[TestMethod]
	public void HostUsesInjectableTimeAndInput()
	{
		using var host = new TestElementHost();
		var keyboard = new KeyboardStub();
		var mouse = new MouseStub();
		var time = DateTimeProvider.RealTime;

		IsTrue(ReferenceEquals(host.TimeProvider, DateTimeProvider.RealTime));
		IsTrue(ReferenceEquals(host.Keyboard, WindowsInput.Keyboard));
		IsTrue(ReferenceEquals(host.Mouse, WindowsInput.Mouse));

		host.TimeProvider = time;
		host.Keyboard = keyboard;
		host.Mouse = mouse;

		IsTrue(ReferenceEquals(host.TimeProvider, time));
		IsTrue(ReferenceEquals(host.Keyboard, keyboard));
		IsTrue(ReferenceEquals(host.Mouse, mouse));

		var child = new TestElement("ok", host);
		host.Children.Add(child);
		IsTrue(ReferenceEquals(child.Keyboard, keyboard));
		IsTrue(ReferenceEquals(child.Mouse, mouse));
		IsTrue(ReferenceEquals(child.TimeProvider, time));
	}

	[TestMethod]
	public void FirstThrowsWhenMissingAndWaitIsFalse()
	{
		using var host = new TestElementHost();
		ExpectedException<InvalidOperationException>(() => host.First("missing", wait: false));
	}

	[TestMethod]
	public void NativeIdSearchDoesNotRefreshTheTree()
	{
		using var host = new NativeSearchHost();
		host.Children.Add(new TestElement("ok", host));

		AreEqual("ok", host.FirstOrDefault("ok", wait: false).Id);
		AreEqual(0, host.RefreshCount);
		IsTrue(host.NativeSearchCount > 0);

		host.SearchTimeout = TimeSpan.FromMilliseconds(200);
		var watch = Stopwatch.StartNew();
		var missing = host.FirstOrDefault("missing");
		watch.Stop();

		IsNull(missing);
		AreEqual(0, host.RefreshCount);
		IsTrue(watch.ElapsedMilliseconds >= 150);
		IsTrue(watch.ElapsedMilliseconds < 800);
	}

	#endregion

	#region Classes

	private sealed class NativeSearchHost : TestElementHost
	{
		#region Properties

		public int NativeSearchCount { get; private set; }

		protected override bool SupportsNativeIdSearch => true;

		#endregion

		#region Methods

		protected override T FindNativeById<T>(string id, bool includeDescendants)
		{
			NativeSearchCount++;
			return Children.FirstOrDefault<T>(id, includeDescendants);
		}

		#endregion
	}

	#endregion
}
