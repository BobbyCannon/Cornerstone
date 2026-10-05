#region References

using System.Text.Json.Nodes;

#endregion

namespace Cornerstone.Automation.Web.Elements;

public partial class Option : WebElement
{
	#region Constants

	public const string HtmlTagName = "option";

	#endregion

	#region Constructors

	public Option(JsonNode element, WebAutomation automation, WebElement parent)
		: base(element, automation, parent)
	{
	}

	#endregion

	#region Properties

	public string Disabled
	{
		get => this["disabled"];
		set => this["disabled"] = value;
	}

	public string Label
	{
		get => this["label"];
		set => this["label"] = value;
	}

	public string Selected
	{
		get => this["selected"];
		set => this["selected"] = value;
	}

	public string Value
	{
		get => this["value"];
		set
		{
			this["value"] = value;
			TriggerElement();
		}
	}

	#endregion
}
