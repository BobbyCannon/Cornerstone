#region References

using System;
using System.Buffers;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

#endregion

namespace Cornerstone.Web.AspNetCore;

/// <summary>
/// Sets up JSON formatter options for <see cref="MvcOptions" />.
/// </summary>
internal sealed class JsonMvcOptionsSetup : IConfigureOptions<MvcOptions>
{
	#region Fields

	private readonly ArrayPool<char> _charPool;
	private readonly JsonOptions _jsonOptions;
	private readonly ILoggerFactory _loggerFactory;
	private readonly ObjectPoolProvider _objectPoolProvider;

	#endregion

	#region Constructors

	public JsonMvcOptionsSetup(
		ILoggerFactory loggerFactory,
		IOptions<JsonOptions> jsonOptions,
		ArrayPool<char> charPool,
		ObjectPoolProvider objectPoolProvider)
	{
		if (loggerFactory == null)
		{
			throw new ArgumentNullException(nameof(loggerFactory));
		}

		if (jsonOptions == null)
		{
			throw new ArgumentNullException(nameof(jsonOptions));
		}

		if (charPool == null)
		{
			throw new ArgumentNullException(nameof(charPool));
		}

		if (objectPoolProvider == null)
		{
			throw new ArgumentNullException(nameof(objectPoolProvider));
		}

		_loggerFactory = loggerFactory;
		_jsonOptions = jsonOptions.Value;
		_charPool = charPool;
		_objectPoolProvider = objectPoolProvider;
	}

	#endregion

	#region Methods

	public void Configure(MvcOptions options)
	{
		options.OutputFormatters.RemoveType<SystemTextJsonOutputFormatter>();
		options.OutputFormatters.Add(new JsonOutputFormatter(options));
		options.InputFormatters.RemoveType<SystemTextJsonInputFormatter>();

		// Register JsonPatchInputFormatter before JsonInputFormatter, otherwise
		// JsonInputFormatter would consume "application/json-patch+json" requests
		// before JsonPatchInputFormatter gets to see them.
		options.InputFormatters.Add(new JsonPatchInputFormatter());
		options.InputFormatters.Add(new JsonInputFormatter());

		options.FormatterMappings.SetMediaTypeMappingForFormat("json", MediaTypeHeaderValues.ApplicationJson);

		options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(IJsonPatchDocument)));
		options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(JToken)));
	}

	#endregion
}