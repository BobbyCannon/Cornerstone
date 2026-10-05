#region References

using System.Collections.Generic;
using System.Text.Json.Nodes;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Automation.Web.Elements;

public partial class TextArea : WebElement
{
	#region Constants

	public const string HtmlTagName = "textarea";

	#endregion

	#region Constructors

	public TextArea(JsonNode element, WebAutomation automation, WebElement parent)
		: base(element, automation, parent)
	{
		TypingDelay = automation.SlowMotion ? 50 : 0;
	}

	#endregion

	#region Properties

	public string AutoFocus
	{
		get => this["autofocus"];
		set => this["autofocus"] = value;
	}

	public string Cols
	{
		get => this["cols"];
		set => this["cols"] = value;
	}

	public string Disabled
	{
		get => this["disabled"];
		set => this["disabled"] = value;
	}

	public string Form
	{
		get => this["form"];
		set => this["form"] = value;
	}

	public string MaxLength
	{
		get => this["maxlength"];
		set => this["maxlength"] = value;
	}

	public string PlaceHolder
	{
		get => this["placeholder"];
		set => this["placeholder"] = value;
	}

	public string ReadOnly
	{
		get => this["readonly"];
		set => this["readonly"] = value;
	}

	public string Required
	{
		get => this["required"];
		set => this["required"] = value;
	}

	public string Rows
	{
		get => this["rows"];
		set => this["rows"] = value;
	}

	public override string Text
	{
		get => this["value"];
		set => SendInput(value, true);
	}

	public int TypingDelay { get; set; }

	public string Value
	{
		get => this["value"];
		set => this["value"] = value;
	}

	public string Wrap
	{
		get => this["wrap"];
		set => this["wrap"] = value;
	}

	#endregion

	#region Methods

	public WebElement SendInput(string value)
	{
		return SendInput(value, false);
	}

	public WebElement SendInput(string text, bool reset)
	{
		Click();
		FocusAsync().AwaitResults();
		Highlight(true);

		var value = reset ? text : GetAttributeValue("value") + text;
		SetAttributeValue("value", value);
		Delay(TypingDelay);

		Highlight(false);
		TriggerElement();
		return this;
	}

	public WebElement SendKeys(string value, bool reset)
	{
		Click();
		FocusAsync().AwaitResults();
		Highlight(true);

		var newValue = reset ? string.Empty : Text;

		foreach (var character in value)
		{
			var eventProperty = GetKeyCodeEventProperty(character);
			newValue += character;
			FireEventAsync("keyDown", eventProperty);
			SetAttributeValue("value", newValue);
			FireEventAsync("keyPress", eventProperty);
			FireEventAsync("keyUp", eventProperty);
			Delay(TypingDelay);
		}

		Delay(TypingDelay);

		Highlight(false);
		TriggerElement();
		return this;
	}

	#endregion
}
