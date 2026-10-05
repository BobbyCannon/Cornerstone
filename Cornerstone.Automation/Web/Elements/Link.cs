#region References

using System.Text.Json.Nodes;

#endregion

namespace Cornerstone.Automation.Web.Elements;

public partial class Link : WebElement
{
	#region Constants

	public const string HtmlTagName = "a";

	#endregion

	#region Constructors

	public Link(JsonNode element, WebAutomation automation, WebElement parent)
		: base(element, automation, parent)
	{
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or set the download attribute.
	/// </summary>
	/// <remarks>
	/// HTML5: Specifies that the target will be downloaded when a user clicks on the hyperlink.
	/// </remarks>
	public string Download
	{
		get => this["download"];
		set => this["download"] = value;
	}

	/// <summary>
	/// Gets or set the hypertext reference (href) attribute.
	/// </summary>
	/// <remarks>
	/// Specifies the URL of the page the link goes to.
	/// </remarks>
	public string Href
	{
		get => this["href"];
		set => this["href"] = value;
	}

	/// <summary>
	/// Gets or set the media attribute.
	/// </summary>
	/// <remarks>
	/// HTML5: Specifies what media/device the linked document is optimized for.
	/// </remarks>
	public string Media
	{
		get => this["media"];
		set => this["media"] = value;
	}

	/// <summary>
	/// Gets or set the hypertext reference of this link.
	/// </summary>
	/// <remarks>
	/// The rel attribute specifies the relationship between the current document and the linked
	/// document. Only used if the href attribute is present.
	/// </remarks>
	public string Rel
	{
		get => this["rel"];
		set => this["rel"] = value;
	}

	/// <summary>
	/// Gets or set the target of this link.
	/// </summary>
	/// <remarks>
	/// Specifies where to open the linked document.
	/// </remarks>
	public string Target
	{
		get => this["target"];
		set => this["target"] = value;
	}

	/// <summary>
	/// Gets or set the media type of this link.
	/// </summary>
	/// <remarks>
	/// The Internet media type of the linked document. Look at IANA Media Types for a complete
	/// list of standard media types.
	/// </remarks>
	public string Type
	{
		get => this["type"];
		set => this["type"] = value;
	}

	#endregion
}