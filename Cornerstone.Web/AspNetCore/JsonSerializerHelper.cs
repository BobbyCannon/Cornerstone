#region References

using Cornerstone.Extensions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

#endregion

namespace Cornerstone.Web.AspNetCore;

internal class JsonSerializerHelper : IJsonHelper
{
	#region Methods

	public IHtmlContent Serialize(object value)
	{
		return new HtmlString(value.ToJson());
	}

	#endregion
}