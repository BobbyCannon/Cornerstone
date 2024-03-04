#region References

using System;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc.Formatters;

#endregion

namespace Cornerstone.Web.AspNetCore;

/// <summary>
/// Initializes a new <see cref="JsonPatchInputFormatter" /> instance.
/// </summary>
internal class JsonPatchInputFormatter : JsonInputFormatter
{
	#region Constructors

	public JsonPatchInputFormatter()
	{
		// Clear all values and only include json-patch+json value.
		SupportedMediaTypes.Clear();
		SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJsonPatch);
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override InputFormatterExceptionPolicy ExceptionPolicy =>
		GetType() == typeof(JsonPatchInputFormatter)
			? InputFormatterExceptionPolicy.MalformedInputExceptions
			: InputFormatterExceptionPolicy.AllExceptions;

	#endregion

	#region Methods

	/// <inheritdoc />
	public override bool CanRead(InputFormatterContext context)
	{
		if (context == null)
		{
			throw new ArgumentNullException(nameof(context));
		}

		var modelType = context.ModelType;
		if (!typeof(IJsonPatchDocument).IsAssignableFrom(modelType) 
			|| !modelType.IsGenericType)
		{
			return false;
		}

		var response =  base.CanRead(context);
		return response;
	}

	#endregion
}