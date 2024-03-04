#region References

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

#endregion

namespace Cornerstone.Web.AspNetCore;

/// <summary>
/// Extensions for the IMvcBuilder.
/// </summary>
public static class MvcBuilderExtensions
{
	#region Methods

	/// <summary>
	/// Configures Cornerstone Json specific features such as input and output formatters.
	/// </summary>
	/// <param name="builder"> The <see cref="IMvcBuilder" />. </param>
	/// <param name="setupAction"> Callback to configure <see cref="JsonOptions" />. </param>
	/// <returns> The <see cref="IMvcBuilder" />. </returns>
	public static IMvcBuilder AddJsonSerializer(this IMvcBuilder builder, Action<JsonOptions> setupAction = null)
	{
		if (builder == null)
		{
			throw new ArgumentNullException(nameof(builder));
		}

		var services = builder.Services;

		services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, JsonMvcOptionsSetup>());
		services.TryAddEnumerable(ServiceDescriptor.Transient<IApiDescriptionProvider, JsonPatchOperationsArrayProvider>());

		var jsonResultExecutor = services.FirstOrDefault(f =>
			(f.ServiceType == typeof(IActionResultExecutor<JsonResult>)) &&
			(f.ImplementationType?.Assembly == typeof(JsonResult).Assembly));

		if (jsonResultExecutor != null)
		{
			services.Remove(jsonResultExecutor);
		}
		services.AddSingleton<IActionResultExecutor<JsonResult>, JsonResultExecutor>();

		var viewFeaturesAssembly = typeof(IHtmlHelper).Assembly;
		var jsonHelper = services.FirstOrDefault(f =>
			(f.ServiceType == typeof(IJsonHelper))
			&& (f.ImplementationType?.Assembly == viewFeaturesAssembly)
		);

		if (jsonHelper != null)
		{
			services.Remove(jsonHelper);
		}

		services.AddSingleton<IJsonHelper>(new JsonSerializerHelper());

		if (setupAction != null)
		{
			builder.Services.Configure(setupAction);
		}

		return builder;
	}

	#endregion
}