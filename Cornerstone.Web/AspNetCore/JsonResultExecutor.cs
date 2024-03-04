#region References

using System;
using System.Text;
using System.Threading.Tasks;
using Cornerstone.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

#endregion

namespace Cornerstone.Web.AspNetCore;

/// <summary>
/// Executes a <see cref="JsonResult" /> to write to the response.
/// </summary>
internal sealed class JsonResultExecutor : IActionResultExecutor<JsonResult>
{
	#region Fields

	private static readonly string _defaultContentType;
	private readonly MvcOptions _mvcOptions;
	private readonly IHttpResponseStreamWriterFactory _writerFactory;

	#endregion

	#region Constructors

	public JsonResultExecutor(IHttpResponseStreamWriterFactory writerFactory, IOptions<MvcOptions> mvcOptions)
	{
		_writerFactory = writerFactory;
		_mvcOptions = mvcOptions?.Value ?? throw new ArgumentNullException(nameof(mvcOptions));
	}

	static JsonResultExecutor()
	{
		_defaultContentType = new MediaTypeHeaderValue("application/json") { Encoding = Encoding.UTF8 }.ToString();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Executes the <see cref="JsonResult" /> and writes the response.
	/// </summary>
	/// <param name="context"> The <see cref="ActionContext" />. </param>
	/// <param name="result"> The <see cref="JsonResult" />. </param>
	/// <returns> A <see cref="Task" /> which will complete when writing has completed. </returns>
	public async Task ExecuteAsync(ActionContext context, JsonResult result)
	{
		var response = context.HttpContext.Response;

		ResolveContentTypeAndEncoding(
			result.ContentType,
			response.ContentType,
			(_defaultContentType, Encoding.UTF8),
			MediaType.GetEncoding,
			out var resolvedContentType,
			out var resolvedContentTypeEncoding);

		response.ContentType = resolvedContentType;

		if (result.StatusCode != null)
		{
			response.StatusCode = result.StatusCode.Value;
		}

		var responseStream = response.Body;
		FileBufferingWriteStream fileBufferingWriteStream = null;

		if (!_mvcOptions.SuppressOutputFormatterBuffering)
		{
			fileBufferingWriteStream = new FileBufferingWriteStream();
			responseStream = fileBufferingWriteStream;
		}

		try
		{
			await using (var writer = _writerFactory.CreateWriter(responseStream, resolvedContentTypeEncoding))
			{
				writer.Write(result.Value.ToJson());
			}

			if (fileBufferingWriteStream != null)
			{
				await fileBufferingWriteStream.DrainBufferAsync(response.Body);
			}
		}
		finally
		{
			if (fileBufferingWriteStream != null)
			{
				await fileBufferingWriteStream.DisposeAsync();
			}
		}
	}

	/// <summary>
	/// Gets the content type and encoding that need to be used for the response.
	/// The priority for selecting the content type is:
	/// 1. ContentType property set on the action result
	/// 2. <see cref="HttpResponse.ContentType" /> property set on <see cref="HttpResponse" />
	/// 3. Default content type set on the action result
	/// </summary>
	/// <remarks>
	/// The user supplied content type is not modified and is used as is. For example, if user
	/// sets the content type to be "text/plain" without any encoding, then the default content type's
	/// encoding is used to write the response and the ContentType header is set to be "text/plain" without any
	/// "charset" information.
	/// Copy of ResponseContentTypeHelper.ResolveContentTypeAndEncoding because it's internal.
	/// </remarks>
	public static void ResolveContentTypeAndEncoding(
		string actionResultContentType,
		string httpResponseContentType,
		(string defaultContentType, Encoding defaultEncoding) @default,
		Func<string, Encoding> getEncoding,
		out string resolvedContentType,
		out Encoding resolvedContentTypeEncoding)
	{
		var (defaultContentType, defaultContentTypeEncoding) = @default;

		// 1. User sets the ContentType property on the action result
		if (actionResultContentType != null)
		{
			resolvedContentType = actionResultContentType;
			var actionResultEncoding = getEncoding(actionResultContentType);
			resolvedContentTypeEncoding = actionResultEncoding ?? defaultContentTypeEncoding;
			return;
		}

		// 2. User sets the ContentType property on the http response directly
		if (!string.IsNullOrEmpty(httpResponseContentType))
		{
			var mediaTypeEncoding = getEncoding(httpResponseContentType);
			if (mediaTypeEncoding != null)
			{
				resolvedContentType = httpResponseContentType;
				resolvedContentTypeEncoding = mediaTypeEncoding;
			}
			else
			{
				resolvedContentType = httpResponseContentType;
				resolvedContentTypeEncoding = defaultContentTypeEncoding;
			}

			return;
		}

		// 3. Fall-back to the default content type
		resolvedContentType = defaultContentType;
		resolvedContentTypeEncoding = defaultContentTypeEncoding;
	}

	#endregion
}