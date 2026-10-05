#region References

using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Automation.Web.Elements;

public partial class Input : WebElement
{
	#region Constants

	public const string HtmlTagName = "input";

	#endregion

	#region Constructors

	public Input(JsonNode element, WebAutomation automation, WebElement parent)
		: base(element, automation, parent)
	{
		TypingDelay = automation.SlowMotion ? 50 : 0;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the autofocus attribute.
	/// </summary>
	/// <remarks>
	/// HTML5: Specifies that an input element should automatically get focus when the page loads.
	/// </remarks>
	public string AutoFocus
	{
		get => this["autofocus"];
		set => this["autofocus"] = value;
	}

	/// <summary>
	/// Gets or sets the disabled attribute.
	/// </summary>
	/// <remarks>
	/// Specifies that a button should be disabled.
	/// </remarks>
	public string Disabled
	{
		get => this["disabled"];
		set => this["disabled"] = value;
	}

	/// <summary>
	/// Gets or sets the pattern attribute.
	/// </summary>
	/// <remarks>
	/// HTML5: Specifies a regular expression that an input element's value is checked against.
	/// </remarks>
	public string Pattern
	{
		get => this["pattern"];
		set => this["pattern"] = value;
	}

	/// <summary>
	/// Gets or sets the placeholder attribute.
	/// </summary>
	/// <remarks>
	/// HTML5: Specifies a short hint that describes the expected value of an input element.
	/// </remarks>
	public string PlaceHolder
	{
		get => this["placeholder"];
		set => this["placeholder"] = value;
	}

	/// <summary>
	/// Gets or sets the read only attribute.
	/// </summary>
	/// <remarks>
	/// Specifies that an input field is read-only
	/// </remarks>
	public string ReadOnly
	{
		get => this["readonly"];
		set => this["readonly"] = value;
	}

	/// <summary>
	/// Gets or sets the step attribute.
	/// </summary>
	/// <remarks>
	/// HTML5: Specifies the legal number intervals for an input field.
	/// </remarks>
	public string Step
	{
		get => this["step"];
		set => this["step"] = value;
	}

	/// <summary>
	/// Gets or sets the value attribute.
	/// </summary>
	/// <remarks>
	/// Specifies the value of an input element.
	/// </remarks>
	public override string Text
	{
		get => this["value"];
		set => SendInput(value, false);
	}

	/// <summary>
	/// Gets the delay (in milliseconds) between each character.
	/// </summary>
	public int TypingDelay { get; set; }

	/// <summary>
	/// Gets or sets the value attribute.
	/// </summary>
	/// <remarks>
	/// Specifies the value of an input element.
	/// </remarks>
	public string Value
	{
		get => this["value"];
		set => this["value"] = value;
	}

	#endregion

	#region Methods

	public WebElement SendInput(string value)
	{
		return SendInput(value, false);
	}

	/// <summary>
	/// Set the text of the element to the value provided.
	/// </summary>
	/// <param name="text"> The text to set as the value. </param>
	/// <param name="reset"> Clear the input before setting the text. </param>
	public WebElement SendInput(string text, bool reset)
	{
		SendInputAsync(text, reset).AwaitResults();
		return this;
	}

	/// <summary>
	/// Set the text of the element without blocking the UI thread.
	/// WebView script execution posts back to the dispatcher; awaiting that from the UI thread deadlocks.
	/// </summary>
	public async Task SendInputAsync(string text, bool reset)
	{
		var value = reset ? text : GetAttributeValue("value", false) + text;
		SetAttributeValue("value", value);
		Delay(TypingDelay);
		await FireEventAsync("input", new Dictionary<string, string> { { "bubbles", "true" } }).ConfigureAwait(false);
	}

	#endregion
}