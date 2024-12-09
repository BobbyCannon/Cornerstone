#region References

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Net;

public class WebClientProxy : IWebClient
{
	#region Properties

	/// <inheritdoc />
	public Uri BaseUri { get; set; }

	/// <inheritdoc />
	public Credential Credential { get; set; }

	/// <inheritdoc />
	public HttpHeaders Headers { get; set; }

	/// <inheritdoc />
	public IWebProxy Proxy { get; set; }

	/// <inheritdoc />
	public TimeSpan Timeout { get; set; }

	/// <inheritdoc />
	public bool UseRawJson { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public object DeepCloneObject(int? maxDepth = null, IncludeExcludeSettings settings = null)
	{
		return this;
	}

	/// <inheritdoc />
	public HttpResponseMessage Delete(string uri, TimeSpan? timeout = null)
	{
		return new HttpResponseMessage(HttpStatusCode.OK);
	}

	/// <inheritdoc />
	public T Deserialize<T>(HttpResponseMessage result, TimeSpan? timeout = null)
	{
		return default;
	}

	/// <inheritdoc />
	public void Dispose()
	{
	}

	/// <inheritdoc />
	public T Get<T>(string uri, TimeSpan? timeout = null)
	{
		return default;
	}

	/// <inheritdoc />
	public HttpResponseMessage Get(string uri, TimeSpan? timeout = null)
	{
		return new HttpResponseMessage(HttpStatusCode.OK);
	}

	/// <inheritdoc />
	public void Initialize()
	{
	}

	/// <inheritdoc />
	public HttpResponseMessage Patch<TContent>(string uri, TContent content, TimeSpan? timeout = null)
	{
		return new HttpResponseMessage(HttpStatusCode.OK);
	}

	/// <inheritdoc />
	public TResult Post<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null)
	{
		return default;
	}

	/// <inheritdoc />
	public HttpResponseMessage Post<TContent>(string uri, TContent content, TimeSpan? timeout = null)
	{
		return new HttpResponseMessage(HttpStatusCode.OK);
	}

	/// <inheritdoc />
	public HttpResponseMessage Post(string uri, string content, TimeSpan? timeout = null)
	{
		return new HttpResponseMessage(HttpStatusCode.OK);
	}

	/// <inheritdoc />
	public TResult Put<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null)
	{
		return default;
	}

	/// <inheritdoc />
	public HttpResponseMessage Put<TContent>(string uri, TContent content, TimeSpan? timeout = null)
	{
		return new HttpResponseMessage(HttpStatusCode.OK);
	}

	/// <inheritdoc />
	public void Reset()
	{
	}

	/// <inheritdoc />
	public object ShallowCloneObject(IncludeExcludeSettings settings = null)
	{
		return this;
	}

	#endregion
}