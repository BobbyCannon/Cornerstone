#region References

using System.Text.Json.Nodes;

#endregion

namespace Cornerstone.Automation.Web.Elements;

public partial class Table : WebElement
{
	#region Constants

	public const string HtmlTagName = "table";

	#endregion

	#region Constructors

	public Table(JsonNode element, WebAutomation automation, WebElement parent)
		: base(element, automation, parent)
	{
	}

	#endregion

	#region Properties

	public string Sortable
	{
		get => this["sortable"];
		set => this["sortable"] = value;
	}

	#endregion
}
