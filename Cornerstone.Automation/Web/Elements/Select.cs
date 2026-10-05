#region References

using System.Linq;
using System.Text.Json.Nodes;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Automation.Web.Elements;

public partial class Select : WebElement
{
	#region Constants

	public const string HtmlTagName = "select";

	#endregion

	#region Constructors

	public Select(JsonNode element, WebAutomation automation, WebElement parent)
		: base(element, automation, parent)
	{
	}

	#endregion

	#region Properties

	public string AutoFocus
	{
		get => this["autofocus"];
		set => this["autofocus"] = value;
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

	public string Multiple
	{
		get => this["multiple"];
		set => this["multiple"] = value;
	}

	public string OptionCount
	{
		get => this["size"];
		set => this["size"] = value;
	}

	public string Required
	{
		get => this["required"];
		set => this["required"] = value;
	}

	public Option SelectedOption => Children.OfType<Option>().FirstOrDefault(x => x.Value == Value);

	public override string Text
	{
		get => Automation.ExecuteJavaScriptAsync($"Cornerstone.getSelectText(\'{Id}\',{GetFrameIdInsert()})").AwaitResults();
		set => Automation.ExecuteJavaScriptAsync($"Cornerstone.setSelectText(\'{Id}\',{GetFrameIdInsert()},\'{value}\')").AwaitResults();
	}

	public string Value
	{
		get => this["value"];
		set => this["value"] = value;
	}

	#endregion

	#region Methods

	public WebElement SendInput(string value)
	{
		try
		{
			Click();
			FocusAsync().AwaitResults();
			Highlight(true);
			Text = value;
			return this;
		}
		finally
		{
			Highlight(false);
		}
	}

	#endregion
}
