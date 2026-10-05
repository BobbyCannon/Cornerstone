#region References

using System;
using Cornerstone.Automation.Web;
using Cornerstone.Automation.Web.Browsers;
using Cornerstone.Automation.Web.Elements;
using Cornerstone.UnitTests;
using InputElement = Cornerstone.Automation.Web.Elements.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.AutomationTests;

[TestClass]
public class ChromiumHeadlessTests : CornerstoneUnitTest
{
	#region Fields

	private static Browser _chrome;

	#endregion

	#region Methods

	[ClassCleanup]
	public static void ClassCleanup()
	{
		_chrome?.Dispose();
		_chrome = null;
	}

	[ClassInitialize]
	public static void ClassInitialize(TestContext context)
	{
		_chrome = Chrome.CreateHeadless();
	}

	[TestMethod]
	[Timeout(30000)]
	public void ChecksBoxAndRadio()
	{
		LoadSamplePage(_chrome);

		var box = _chrome.FirstOrDefaultDescendants<CheckBox>(x => x.Id == "agree");
		var radio = _chrome.FirstOrDefaultDescendants<RadioButton>(x => x.Id == "choice");
		IsNotNull(box);
		IsNotNull(radio);

		IsFalse(box.Checked);
		box.Click();
		AreEqual("true", box.GetAttributeValue("checked", true)?.ToLowerInvariant());

		radio.Click();
		AreEqual("true", radio.GetAttributeValue("checked", true)?.ToLowerInvariant());
	}

	[TestMethod]
	[Timeout(30000)]
	public void ClicksButton()
	{
		LoadSamplePage(_chrome);

		var button = _chrome.FirstOrDefaultDescendants<Button>(x => x.Id == "go");
		IsNotNull(button);
		button.Click();

		var result = _chrome.ExecuteScript("document.getElementById('clicked').value");
		IsTrue(result.Contains("1"), () => result);
	}

	[TestMethod]
	[Timeout(30000)]
	public void DetectsCredentialFields()
	{
		LoadSamplePage(_chrome);

		_chrome.DetectCredentialFields();
		IsNotNull(_chrome.CredentialElements.UserName);
		IsNotNull(_chrome.CredentialElements.Password);
		AreEqual("user", _chrome.CredentialElements.UserName.Id);
		AreEqual("pass", _chrome.CredentialElements.Password.Id);
	}

	[TestMethod]
	[Timeout(30000)]
	public void LoadsTypedElementsFromHtml()
	{
		LoadSamplePage(_chrome);
		AssertTypedElements(_chrome);
	}

	[TestMethod]
	[Timeout(30000)]
	public void NavigatesToDataUri()
	{
		_chrome.NavigateTo("data:text/html,<html><body><p id='hi'>hello</p></body></html>", "data:");
		_chrome.Refresh();

		var paragraph = _chrome.FirstOrDefaultDescendants(x => x.Id == "hi");
		IsNotNull(paragraph);
		IsTrue(paragraph.Text.Contains("hello"), () => paragraph.Text);
	}

	[TestMethod]
	[Timeout(30000)]
	public void NavigatesAcrossFixturePages()
	{
		using var server = HtmlFixtureServer.Start(s =>
		{
			s.Map("/home.html", """
				<html>
				<body>
					<a id="go" href="/next.html">Next</a>
				</body>
				</html>
				""");
			s.Map("/next.html", """
				<html>
				<body>
					<p id="arrived">done</p>
				</body>
				</html>
				""");
		});

		var home = server.GetUri("/home.html").ToString();
		var next = server.GetUri("/next.html").ToString();

		_chrome.NavigateTo(home);
		IsTrue(_chrome.Uri.StartsWith(home, StringComparison.OrdinalIgnoreCase), () => _chrome.Uri);

		var link = _chrome.FirstOrDefaultDescendants<Link>(x => x.Id == "go");
		IsNotNull(link);
		link.Click();
		_chrome.WaitForNavigation(next);

		var arrived = _chrome.FirstOrDefaultDescendants(x => x.Id == "arrived");
		IsNotNull(arrived);
		IsTrue(arrived.Text.Contains("done"), () => arrived.Text);
	}

	[TestMethod]
	[Timeout(30000)]
	public void FollowsFixtureRedirect()
	{
		using var server = HtmlFixtureServer.Start(s =>
		{
			s.Map("/landed.html", """
				<html>
				<body>
					<p id="landed">ok</p>
				</body>
				</html>
				""");
			s.MapRedirect("/go.html", "/landed.html");
		});

		var landed = server.GetUri("/landed.html").ToString();
		_chrome.NavigateTo(server.GetUri("/go.html").ToString(), landed);

		var paragraph = _chrome.FirstOrDefaultDescendants(x => x.Id == "landed");
		IsNotNull(paragraph);
		IsTrue(paragraph.Text.Contains("ok"), () => paragraph.Text);
	}

	[TestMethod]
	[Timeout(30000)]
	public void SelectsOption()
	{
		LoadSamplePage(_chrome);

		var select = _chrome.FirstOrDefaultDescendants<Select>(x => x.Id == "color");
		IsNotNull(select);
		select.Value = "g";
		AreEqual("g", select.GetAttributeValue("value", true));
	}

	[TestMethod]
	[Timeout(30000)]
	public void TypesIntoInputAndTextArea()
	{
		LoadSamplePage(_chrome);

		var input = _chrome.FirstOrDefaultDescendants<InputElement>(x => x.Id == "user");
		var area = _chrome.FirstOrDefaultDescendants<TextArea>(x => x.Id == "notes");
		IsNotNull(input);
		IsNotNull(area);

		input.SendInput("pat", true);
		AreEqual("pat", input.GetAttributeValue("value", true));

		area.SendInput("memo", true);
		AreEqual("memo", area.GetAttributeValue("value", true));
	}

	private void AssertTypedElements(Browser browser)
	{
		IsNotNull(browser.FirstOrDefaultDescendants<Form>(x => x.Id == "login"));
		IsNotNull(browser.FirstOrDefaultDescendants<InputElement>(x => x.Id == "user"));
		IsNotNull(browser.FirstOrDefaultDescendants<InputElement>(x => x.Id == "pass"));
		IsNotNull(browser.FirstOrDefaultDescendants<Button>(x => x.Id == "go"));
		IsNotNull(browser.FirstOrDefaultDescendants<Select>(x => x.Id == "color"));
		IsNotNull(browser.FirstOrDefaultDescendants<Option>(x => x.Id == "opt-red"));
		IsNotNull(browser.FirstOrDefaultDescendants<Table>(x => x.Id == "grid"));
		IsNotNull(browser.FirstOrDefaultDescendants<TextArea>(x => x.Id == "notes"));
		IsNotNull(browser.FirstOrDefaultDescendants<Link>(x => x.Id == "home"));
		IsNotNull(browser.FirstOrDefaultDescendants<CheckBox>(x => x.Id == "agree"));
		IsNotNull(browser.FirstOrDefaultDescendants<RadioButton>(x => x.Id == "choice"));
	}

	private static void LoadSamplePage(Browser browser)
	{
		browser.SetHtml("""
			<html>
			<body>
			<form id="login">
				<input id="user" name="username" autocomplete="username" />
				<input id="pass" name="password" type="password" />
				<button id="go" type="button" onclick="document.getElementById('clicked').value='1'">Go</button>
				<input id="clicked" value="0" />
				<select id="color">
					<option id="opt-red" value="r">Red</option>
					<option id="opt-green" value="g">Green</option>
				</select>
				<table id="grid"><tr><td>a</td></tr></table>
				<textarea id="notes"></textarea>
				<a id="home" href="#home">Home</a>
				<input id="agree" type="checkbox" />
				<input id="choice" type="radio" name="group" />
			</form>
			</body>
			</html>
			""");
		browser.Refresh();
	}

	#endregion
}
