#region References

using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

#endregion

namespace Cornerstone.Web.Extensions;

/// <summary>
/// Extensions for IHtmlContent
/// </summary>
public static class HtmlContentExtensions
{
	#region Methods

	/// <summary>
	/// Convert the HTML content into a string.
	/// </summary>
	/// <param name="htmlContent"> The content to convert. </param>
	/// <returns> The HTML content in string format. </returns>
	public static string ToHtmlString(this IHtmlContent htmlContent)
	{
		if (htmlContent is HtmlString htmlString)
		{
			return htmlString.Value;
		}

		using (var writer = new StringWriter())
		{
			htmlContent.WriteTo(writer, HtmlEncoder.Default);
			return writer.ToString();
		}
	}

	#endregion
}