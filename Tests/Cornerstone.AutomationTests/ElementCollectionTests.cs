#region References

using System;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.AutomationTests;

[TestClass]
public class ElementCollectionTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ContainsUsesLookupAndDoesNotThrowWhenMissing()
	{
		using var host = new TestElementHost();
		host.Children.Add(new TestElement("ok", host));

		IsTrue(host.Children.Contains("ok"));
		IsFalse(host.Children.Contains("missing"));
	}

	[TestMethod]
	public void IndexerReturnsNullWhenMissing()
	{
		using var host = new TestElementHost();
		host.Children.Add(new TestElement("ok", host, name: "named", automationId: "auto"));

		AreEqual("ok", host.Children["ok"].Id);
		AreEqual("ok", host.Children["named"].Id);
		AreEqual("ok", host.Children["auto"].Id);
		IsNull(host.Children["missing"]);
	}

	[TestMethod]
	public void RemoveDoesNotSearchDescendantsWhenDisabled()
	{
		using var host = new TestElementHost();
		var parent = new TestElement("parent", host);
		var child = new TestElement("child", parent);
		host.Children.Add(parent);
		parent.Children.Add(child);

		IsFalse(host.Children.Remove(child, false));
		AreEqual(1, parent.Children.Count);
	}

	[TestMethod]
	public void RemoveMutatesTheCollection()
	{
		using var host = new TestElementHost();
		var first = new TestElement("first", host);
		var second = new TestElement("second", host);
		host.Children.Add(first, second);

		IsTrue(host.Children.Remove(first));
		AreEqual(1, host.Children.Count);
		AreEqual(second, host.Children[0]);
		IsFalse(host.Children.Remove(first));
	}

	[TestMethod]
	public void RemoveSearchesDescendants()
	{
		using var host = new TestElementHost();
		var parent = new TestElement("parent", host);
		var child = new TestElement("child", parent);
		host.Children.Add(parent);
		parent.Children.Add(child);

		IsTrue(host.Children.Remove(child));
		AreEqual(0, parent.Children.Count);
		AreEqual(1, host.Children.Count);
		AreEqual(parent, host.Children[0]);
	}

	#endregion
}
