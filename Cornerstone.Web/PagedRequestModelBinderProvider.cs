#region References

using System;
using Cornerstone.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding;

#endregion

namespace Cornerstone.Web;

/// <inheritdoc />
public class PagedRequestModelBinderProvider : IModelBinderProvider
{
	#region Methods

	/// <inheritdoc />
	public IModelBinder GetBinder(ModelBinderProviderContext context)
	{
		if (context == null)
		{
			throw new ArgumentNullException(nameof(context));
		}

		return typeof(PagedRequest).IsAssignableFrom(context.Metadata.ModelType)
			? new PagedRequestModelBinder()
			: null;
	}

	#endregion
}