#region References

using Cornerstone.PowerShell.Cmdlets;
using Cornerstone.Testing;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.IntegrationTests.PowerShell.Cmdlets;

[TestClass]
public class NewWebsiteCmdletTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddNewWebsite()
	{
		NewWebsiteCmdlet.RemoveWebsite("UnitTest");
		
		var cmdlet = new NewWebsiteCmdlet
		{
			Name = "UnitTest",
			Bindings = "*:443:test.com:DEDF47565FA4FC8CF2B04E53BD71DDE24F4118FD"
		};
		cmdlet.AddOrUpdateWebsite();
	}

	[TestMethod]
	public void GetInetpubPath()
	{
		var actual = NewWebsiteCmdlet.GetInetpubPath();
		actual.Dump();
	}

	[TestMethod]
	public void ParseBindings()
	{
		var scenarios = new (string binding, string ip, string port, string host, string ssl)[]
		{
			("*:80:", "*", "80", "", null),
			("*:443:domain.com:DEDF47565FA4FC8CF2B04E53BD71DDE24F4118FD", "*", "443", "domain.com", "DEDF47565FA4FC8CF2B04E53BD71DDE24F4118FD")
		};

		foreach (var scenario in scenarios)
		{
			IsTrue(NewWebsiteCmdlet.TryParseBinding(scenario.binding, out var actual));
			AreEqual(scenario.ip, actual["ip"]);
			AreEqual(scenario.port, actual["port"]);
			AreEqual(scenario.host, actual["host"]);
			AreEqual(scenario.ssl, actual.TryGetValue("ssl", out var value) ? value : null);
		}
	}

	#endregion
}