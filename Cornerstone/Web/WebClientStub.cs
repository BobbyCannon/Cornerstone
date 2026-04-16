#region References

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Web;

public class WebClientStub : Notifiable, IWebClient
{
	#region Properties

	public Uri BaseUri { get; set; }

	public Credential Credential { get; set; }

	public HttpHeaders Headers { get; set; }

	public string IpAddress { get; set; }

	public JsonSerializerOptions SerializerOptions { get; set; }

	public TimeSpan Timeout { get; set; }

	#endregion

	#region Methods

	public HttpResponseMessage Delete(string uri, TimeSpan? timeout = null)
	{
		return new HttpResponseMessage(HttpStatusCode.OK);
	}

	public Task<HttpResponseMessage> DeleteAsync(string uri, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
	}

	public void Dispose()
	{
	}

	public HttpResponseMessage Get(string uri, TimeSpan? timeout = null)
	{
		return new HttpResponseMessage(HttpStatusCode.OK);
	}

	public T Get<T>(string uri, TimeSpan? timeout = null)
	{
		return default;
	}

	public Task<HttpResponseMessage> GetAsync(string uri, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
	}

	public Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(default(T));
	}

	public void Initialize()
	{
	}

	public HttpResponseMessage Patch(string uri, string content, TimeSpan? timeout = null)
	{
		return new HttpResponseMessage(HttpStatusCode.OK);
	}

	public HttpResponseMessage Patch<TContent>(string uri, TContent content, TimeSpan? timeout = null)
	{
		return new HttpResponseMessage(HttpStatusCode.OK);
	}

	public TResult Patch<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null)
	{
		return default;
	}

	public Task<HttpResponseMessage> PatchAsync(string uri, string content, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
	}

	public Task<HttpResponseMessage> PatchAsync<TContent>(string uri, TContent content, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
	}

	public Task<TResult> PatchAsync<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(default(TResult));
	}

	public HttpResponseMessage Post(string uri, string content, TimeSpan? timeout = null)
	{
		return new HttpResponseMessage(HttpStatusCode.OK);
	}

	public HttpResponseMessage Post<TContent>(string uri, TContent content, TimeSpan? timeout = null)
	{
		return new HttpResponseMessage(HttpStatusCode.OK);
	}

	public TResult Post<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null)
	{
		return default;
	}

	public Task<HttpResponseMessage> PostAsync(string uri, string content, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
	}

	public Task<HttpResponseMessage> PostAsync<TContent>(string uri, TContent content, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
	}

	public Task<TResult> PostAsync<TContent, TResult>(string uri, TContent content, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(default(TResult));
	}

	public HttpResponseMessage Put(string uri, string content, TimeSpan? timeout = null)
	{
		return new HttpResponseMessage(HttpStatusCode.OK);
	}

	public TResult Put<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null)
	{
		return default;
	}

	public HttpResponseMessage Put<TContent>(string uri, TContent content, TimeSpan? timeout = null)
	{
		return new HttpResponseMessage(HttpStatusCode.OK);
	}

	public Task<HttpResponseMessage> PutAsync(string uri, string content, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
	}

	public Task<HttpResponseMessage> PutAsync<TContent>(string uri, TContent content, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
	}

	public Task<TResult> PutAsync<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(default(TResult));
	}

	public virtual void RefreshHeaders()
	{
	}

	public void Reset()
	{
	}

	public void Uninitialize()
	{
	}

	#endregion
}