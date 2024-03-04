#region References

using System;
using System.Text;
using System.Threading.Tasks;
using Cornerstone.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

#endregion

namespace Cornerstone.Web.AspNetCore;

/// <summary>
/// A <see cref="TextOutputFormatter" /> for JSON content.
/// </summary>
public class JsonOutputFormatter : TextOutputFormatter
{
	#region Fields

	private JsonOptions _jsonOptions;
	private readonly MvcOptions _mvcOptions;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new <see cref="JsonOutputFormatter" /> instance.
	/// </summary>
	/// <param name="mvcOptions"> The <see cref="MvcOptions" />. </param>
	public JsonOutputFormatter(MvcOptions mvcOptions)
	{
		_mvcOptions = mvcOptions;

		SupportedEncodings.Add(Encoding.UTF8);
		SupportedEncodings.Add(Encoding.Unicode);
		SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJson);
		SupportedMediaTypes.Add(MediaTypeHeaderValues.TextJson);
		SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationAnyJsonSyntax);
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
	{
		if (context == null)
		{
			throw new ArgumentNullException(nameof(context));
		}

		if (selectedEncoding == null)
		{
			throw new ArgumentNullException(nameof(selectedEncoding));
		}

		// Compat mode for derived options
		_jsonOptions ??= context.HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;

		var response = context.HttpContext.Response;
		var responseStream = response.Body;

		FileBufferingWriteStream fileBufferingWriteStream = null;

		if (!_mvcOptions.SuppressOutputFormatterBuffering)
		{
			fileBufferingWriteStream = new FileBufferingWriteStream();
			responseStream = fileBufferingWriteStream;
		}

		var value = context.Object;

		try
		{
			await using (var writer = context.WriterFactory(responseStream, selectedEncoding))
			{
				//using var jsonWriter = CreateJsonWriter(writer);
				//var jsonSerializer = CreateJsonSerializer(context);
				//jsonSerializer.Serialize(jsonWriter, value);
				await writer.WriteAsync(value.ToJson());
			}

			if (fileBufferingWriteStream != null)
			{
				response.ContentLength = fileBufferingWriteStream.Length;
				await fileBufferingWriteStream.DrainBufferAsync(response.BodyWriter);
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

	#endregion
}