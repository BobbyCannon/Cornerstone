#region References

using System;
using System.Threading.Tasks;
using Cornerstone.Data;
using Cornerstone.Web.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

#endregion

namespace Cornerstone.Web;

/// <summary>
/// Binder for PagedRequest models.
/// </summary>
public class PagedRequestModelBinder : IModelBinder
{
	#region Methods

	/// <inheritdoc />
	public Task BindModelAsync(ModelBindingContext bindingContext)
	{
		if (bindingContext == null)
		{
			throw new ArgumentNullException(nameof(bindingContext));
		}

		if (bindingContext.ModelType.CreateInstance() is not PagedRequest pagedRequest)
		{
			bindingContext.Result = ModelBindingResult.Failed();
			return Task.CompletedTask;
		}

		var queryString = bindingContext.HttpContext.Request.QueryString.ToString();
		pagedRequest.ParseQueryString(queryString);
		bindingContext.Result = ModelBindingResult.Success(pagedRequest);
		return Task.CompletedTask;
	}

	#endregion
}