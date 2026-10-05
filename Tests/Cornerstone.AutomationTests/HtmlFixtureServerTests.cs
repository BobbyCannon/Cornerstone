#region References

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Cornerstone.Automation.Web;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.AutomationTests;

[TestClass]
public class HtmlFixtureServerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public async Task ServesMappedHtmlAndJavascript()
	{
		using var server = HtmlFixtureServer.Start(s =>
		{
			s.Map("/index.html", "<html><body>home</body></html>");
			s.Map("/app.js", "window.ready = true;");
		});

		using var client = new HttpClient();
		var html = await client.GetStringAsync(server.GetUri("/index.html"));
		var script = await client.GetStringAsync(server.GetUri("/app.js"));
		var root = await client.GetStringAsync(server.BaseUri);

		AreEqual("<html><body>home</body></html>", html);
		AreEqual("window.ready = true;", script);
		AreEqual(html, root);
	}

	[TestMethod]
	public async Task RedirectsAndReturnsNotFound()
	{
		using var server = HtmlFixtureServer.Start(s =>
		{
			s.Map("/next.html", "<html><body>next</body></html>");
			s.MapRedirect("/old.html", "/next.html");
		});

		using var handler = new HttpClientHandler { AllowAutoRedirect = false };
		using var client = new HttpClient(handler);

		var redirect = await client.GetAsync(server.GetUri("/old.html"));
		AreEqual(HttpStatusCode.Redirect, redirect.StatusCode);
		AreEqual(server.GetUri("/next.html").ToString(), redirect.Headers.Location?.ToString());

		var missing = await client.GetAsync(server.GetUri("/missing.html"));
		AreEqual(HttpStatusCode.NotFound, missing.StatusCode);
	}

	#endregion
}
