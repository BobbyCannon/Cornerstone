#region References

using Newtonsoft.Json.Linq;

#endregion

namespace Cornerstone.Automation.Web.Elements;

/// <summary>
/// Represents a browser Frame element.
/// </summary>
public class Frame : WebElement
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of a browser element.
	/// </summary>
	/// <param name="element"> The browser element this is for. </param>
	/// <param name="browser"> The browser this element is associated with. </param>
	/// <param name="parent"> The parent host for this element. </param>
	public Frame(JToken element, Browser browser, ElementHost parent)
		: base(element, browser, parent)
	{
	}

	#endregion
}