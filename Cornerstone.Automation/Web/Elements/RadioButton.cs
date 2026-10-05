#region References

using System.Text.Json.Nodes;

#endregion

namespace Cornerstone.Automation.Web.Elements;

public partial class RadioButton : WebElement
{
	#region Constants

	public const string HtmlTagName = "input";

	#endregion

	#region Constructors

	public RadioButton(JsonNode element, WebAutomation automation, WebElement parent)
		: base(element, automation, parent)
	{
	}

	#endregion

	#region Properties

	public bool Checked
	{
		get => this["checked"] == "true";
		set => this["checked"] = value.ToString();
	}

	public string Disabled
	{
		get => this["disabled"];
		set => this["disabled"] = value;
	}

	public string Value
	{
		get => this["value"];
		set => this["value"] = value;
	}

	#endregion
}
