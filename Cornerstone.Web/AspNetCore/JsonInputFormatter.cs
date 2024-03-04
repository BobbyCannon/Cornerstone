#region References

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Cornerstone.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

#endregion

namespace Cornerstone.Web.AspNetCore;

/// <summary>
/// A <see cref="TextInputFormatter" /> for JSON content.
/// </summary>
public class JsonInputFormatter : TextInputFormatter, IInputFormatterExceptionPolicy
{
	#region Constructors

	/// <inheritdoc />
	public JsonInputFormatter()
	{
		// Clear all values and only include json-patch+json value.
		SupportedMediaTypes.Clear();
		SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJson);

		SupportedEncodings.Clear();
		SupportedEncodings.Add(Encoding.UTF8);
		SupportedEncodings.Add(Encoding.Unicode);
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public virtual InputFormatterExceptionPolicy ExceptionPolicy =>
		GetType() == typeof(JsonInputFormatter)
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

		if (context.HttpContext.Request.ContentType == MediaTypeHeaderValues.ApplicationJson.MediaType)
		{
			return true;
		}

		var response = base.CanRead(context);
		return response;
	}

	/// <inheritdoc />
	public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
	{
		context.HttpContext.Request.EnableBuffering();
		using var reader = new StreamReader(context.HttpContext.Request.Body, Encoding.UTF8, false, 1024, true);
		var body = await reader.ReadToEndAsync();
		context.HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
		var model = body.FromJson(context.ModelType);
		return await InputFormatterResult.SuccessAsync(model);
	}

	#endregion
}