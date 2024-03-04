#region References

using System;
using System.Reflection;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;

#endregion

namespace Cornerstone.Web.AspNetCore;

/// <summary>
/// Implements a provider of <see cref="ApiDescription" /> to change parameters of
/// type <see cref="IJsonPatchDocument" /> to an array of <see cref="Operation" />.
/// </summary>
internal sealed class JsonPatchOperationsArrayProvider : IApiDescriptionProvider
{
	#region Fields

	private readonly IModelMetadataProvider _modelMetadataProvider;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new instance of <see cref="JsonPatchOperationsArrayProvider" />.
	/// </summary>
	/// <param name="modelMetadataProvider"> The <see cref="IModelMetadataProvider" />. </param>
	public JsonPatchOperationsArrayProvider(IModelMetadataProvider modelMetadataProvider)
	{
		_modelMetadataProvider = modelMetadataProvider;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	/// <remarks>
	/// The order -999 ensures that this provider is executed right after the <c> Microsoft.AspNetCore.Mvc.ApiExplorer.DefaultApiDescriptionProvider </c>.
	/// </remarks>
	public int Order => -999;

	#endregion

	#region Methods

	/// <inheritdoc />
	public void OnProvidersExecuted(ApiDescriptionProviderContext context)
	{
	}

	/// <inheritdoc />
	public void OnProvidersExecuting(ApiDescriptionProviderContext context)
	{
		if (context == null)
		{
			throw new ArgumentNullException(nameof(context));
		}

		foreach (var result in context.Results)
		{
			foreach (var parameterDescription in result.ParameterDescriptions)
			{
				if (typeof(IJsonPatchDocument).GetTypeInfo().IsAssignableFrom(parameterDescription.Type))
				{
					parameterDescription.Type = typeof(Operation[]);
					parameterDescription.ModelMetadata = _modelMetadataProvider.GetMetadataForType(typeof(Operation[]));
				}
			}
		}
	}

	#endregion
}