#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Cornerstone.Data;
using Cornerstone.Exceptions;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using ICloneable = Cornerstone.Data.ICloneable;

#endregion

namespace Cornerstone.Net;

/// <summary>
/// This class is used for making GET and POST calls to an HTTP endpoint.
/// </summary>
public class WebClient : Bindable, IWebClient, ICloneable<WebClient>
{
	#region Fields

	private Credential _credential;
	private readonly HttpClientHandler _handler;
	private readonly HttpClient _httpClient;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new HTTP helper to point at a specific URI, and with the specified session identifier.
	/// </summary>
	/// <param name="baseUri"> The base URI. </param>
	/// <param name="timeout"> The timeout in milliseconds. </param>
	/// <param name="credential"> The optional credential to authenticate with. </param>
	/// <param name="proxy"> The optional proxy to use. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public WebClient(string baseUri, int timeout = 1000, Credential credential = null, IWebProxy proxy = null, IDispatcher dispatcher = null)
		: this(new Uri(baseUri), TimeSpan.FromMilliseconds(timeout), credential, proxy, dispatcher)
	{
	}

	/// <summary>
	/// Initializes a new HTTP helper to point at a specific URI, and with the specified session identifier.
	/// </summary>
	/// <param name="baseUri"> The base URI. </param>
	/// <param name="timeout"> The timeout in milliseconds. </param>
	/// <param name="credential"> The optional credential to authenticate with. </param>
	/// <param name="proxy"> The optional proxy to use. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public WebClient(Uri baseUri, TimeSpan timeout, Credential credential = null, IWebProxy proxy = null, IDispatcher dispatcher = null) : base(dispatcher)
	{
		_handler = new HttpClientHandler();
		_httpClient = new HttpClient(_handler);

		BaseUri = baseUri;
		Credential = credential;
		Timeout = timeout;
		Proxy = proxy;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the base URI for connecting.
	/// </summary>
	public Uri BaseUri
	{
		get => _httpClient.BaseAddress;
		set => _httpClient.BaseAddress = value;
	}

	/// <summary>
	/// The credentials for the connection.
	/// </summary>
	public Credential Credential
	{
		get => _credential;
		set
		{
			if (_credential != null)
			{
				_credential.PropertyChanged -= CredentialOnPropertyChanged;
			}

			_credential = value;

			if (_credential != null)
			{
				_credential.PropertyChanged += CredentialOnPropertyChanged;
			}
		}
	}

	/// <summary>
	/// Headers for this client.
	/// </summary>
	public HttpHeaders Headers => _httpClient.DefaultRequestHeaders;

	public string IpAddress { get; private set; }

	/// <summary>
	/// Gets or sets an optional proxy for the connection.
	/// </summary>
	public IWebProxy Proxy
	{
		get => _handler.Proxy;
		set => _handler.Proxy = value;
	}

	/// <summary>
	/// Gets or sets the number of milliseconds to wait before the request times out. The default value is 100 seconds.
	/// </summary>
	public TimeSpan Timeout { get; set; }

	/// <summary>
	/// Use raw json with out tracking references.
	/// </summary>
	public bool UseRawJson { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public virtual WebClient DeepClone(int? maxDepth = null, IncludeExcludeSettings settings = null)
	{
		return ShallowClone(settings);
	}

	/// <summary>
	/// Delete request
	/// </summary>
	/// <param name="uri"> The URI to use. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The response from the server. </returns>
	public HttpResponseMessage Delete(string uri, TimeSpan? timeout = null)
	{
		return ProcessResponse(
			_httpClient
				.DeleteAsync(uri)
				.AwaitResults(timeout ?? Timeout)
		);
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> Should be true if managed resources should be disposed. </param>
	public virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		if (_credential != null)
		{
			_credential.PropertyChanged -= CredentialOnPropertyChanged;
			_credential = null;
		}

		_httpClient?.Dispose();
		_handler?.Dispose();
	}

	/// <summary>
	/// Gets a response and deserialize it.
	/// </summary>
	/// <param name="uri"> The URI of the content to deserialize. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The response from the server. </returns>
	public virtual HttpResponseMessage Get(string uri, TimeSpan? timeout = null)
	{
		return InternalGet(uri, timeout);
	}

	/// <summary>
	/// Gets a response and deserialize it.
	/// </summary>
	/// <typeparam name="T"> The type to deserialize into. </typeparam>
	/// <param name="uri"> The URI of the content to deserialize. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The deserialized type. </returns>
	public virtual T Get<T>(string uri, TimeSpan? timeout = null)
	{
		using var response = Get(uri, timeout);
		return Deserialize<T>(response, timeout);
	}

	/// <inheritdoc />
	public virtual void Initialize()
	{
	}

	/// <inheritdoc />
	public HttpResponseMessage Patch(string uri, string content, TimeSpan? timeout = null)
	{
		return InternalPatch(uri, content, timeout);
	}

	/// <summary>
	/// Patch an item on the server with the provide content.
	/// </summary>
	/// <typeparam name="TContent"> The type to update with. </typeparam>
	/// <param name="uri"> The URI to patch to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The response from the server. </returns>
	public virtual HttpResponseMessage Patch<TContent>(string uri, TContent content, TimeSpan? timeout = null)
	{
		return InternalPatch(uri, content, timeout);
	}

	/// <inheritdoc />
	public TResult Patch<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null)
	{
		using var response = InternalPatch(uri, content, timeout);
		return Deserialize<TResult>(response);
	}

	/// <summary>
	/// Post an item on the server with the provide content.
	/// </summary>
	/// <param name="uri"> The URI to post to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The response from the server. </returns>
	public virtual HttpResponseMessage Post(string uri, string content, TimeSpan? timeout = null)
	{
		return InternalPost(uri, content, timeout);
	}

	/// <summary>
	/// Post an item on the server with the provide content.
	/// </summary>
	/// <typeparam name="TContent"> The type to update with. </typeparam>
	/// <param name="uri"> The URI to post to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The response from the server. </returns>
	public virtual HttpResponseMessage Post<TContent>(string uri, TContent content, TimeSpan? timeout = null)
	{
		return InternalPost(uri, content, timeout);
	}

	/// <summary>
	/// Post an item on the server with the provide content.
	/// </summary>
	/// <typeparam name="TContent"> The type to update with. </typeparam>
	/// <typeparam name="TResult"> The type to respond with. </typeparam>
	/// <param name="uri"> The URI to post to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The server result. </returns>
	public virtual TResult Post<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null)
	{
		using var response = InternalPost(uri, content, timeout);
		return Deserialize<TResult>(response);
	}

	/// <inheritdoc />
	public HttpResponseMessage Put(string uri, string content, TimeSpan? timeout = null)
	{
		return InternalPost(uri, content, timeout);
	}

	/// <summary>
	/// Put (update) an item on the server with the provide content.
	/// </summary>
	/// <typeparam name="TContent"> The type to update with. </typeparam>
	/// <param name="uri"> The URI to post to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The response from the server. </returns>
	public virtual HttpResponseMessage Put<TContent>(string uri, TContent content, TimeSpan? timeout = null)
	{
		return InternalPut(uri, content, timeout);
	}

	/// <summary>
	/// Put an item on the server with the provide content.
	/// </summary>
	/// <param name="uri"> The URI to put to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The response from the server. </returns>
	public virtual TResult Put<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null)
	{
		using var response = InternalPut(uri, content, timeout);
		return Deserialize<TResult>(response, timeout);
	}

	/// <summary>
	/// Reset the web client.
	/// </summary>
	public virtual void Reset()
	{
	}

	/// <inheritdoc />
	public virtual WebClient ShallowClone(IncludeExcludeSettings settings = null)
	{
		return new WebClient(BaseUri, Timeout, Credential, Proxy, GetDispatcher());
	}

	/// <summary>
	/// Deserialize the response.
	/// </summary>
	/// <typeparam name="T"> The type to deserialize into. </typeparam>
	/// <param name="result"> The result to deserialize. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The deserialized type. </returns>
	protected virtual T Deserialize<T>(HttpResponseMessage result, TimeSpan? timeout = null)
	{
		using var task = result.Content.ReadAsStringAsync();
		var response = task.Result.FromJson<T>();
		return response;
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(string propertyName = null)
	{
		switch (propertyName)
		{
			case nameof(BaseUri):
			{
				try
				{
					IpAddress = Dns.GetHostAddresses(BaseUri.DnsSafeHost).FirstOrDefault()?.ToString();
				}
				catch (Exception e)
				{
					Console.WriteLine("Error: " + e.Message);
				}
				break;
			}
		}

		base.OnPropertyChanged(propertyName);
	}

	/// <inheritdoc />
	protected override void OnPropertyChangedInDispatcher(string propertyName)
	{
		switch (propertyName)
		{
			case nameof(Credential):
			{
				UpdateCredentials();
				break;
			}
		}

		base.OnPropertyChangedInDispatcher(propertyName);
	}

	protected virtual HttpResponseMessage ProcessResponse(HttpResponseMessage response)
	{
		if (!response.IsSuccessStatusCode)
		{
			throw new WebClientException(response);
		}

		return response;
	}

	/// <summary>
	/// Get the serialized string for the provided object.
	/// </summary>
	/// <param name="content"> The content to be converted to a serialize format. </param>
	/// <returns> The serialized formatted content. </returns>
	protected virtual string Serialize(object content)
	{
		if (content is string sValue && sValue.IsJson())
		{
			return sValue;
		}

		return UseRawJson
			? content.ToRawJson()
			: content.ToJson();
	}

	private void CredentialOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		UpdateCredentials();
	}

	private HttpResponseMessage InternalGet(string uri, TimeSpan? timeout = null)
	{
		var response = _httpClient
			.GetAsync(uri)
			.AwaitResults(timeout ?? Timeout);

		return ProcessResponse(response);
	}

	private HttpResponseMessage InternalPatch<T>(string uri, T content, TimeSpan? timeout = null)
	{
		var serialized = Serialize(content);
		return InternalPatch(uri, serialized, "application/json-patch+json", timeout);
	}

	private HttpResponseMessage InternalPatch(string uri, string serialized, string mediaType, TimeSpan? timeout = null)
	{
		using var objectContent = new StringContent(serialized, Encoding.UTF8, mediaType);
		var method = new HttpMethod("PATCH");
		var request = new HttpRequestMessage(method, uri) { Content = objectContent };
		var response = _httpClient
			.SendAsync(request)
			.AwaitResults(timeout ?? Timeout);

		return ProcessResponse(response);
	}

	private HttpResponseMessage InternalPost<T>(string uri, T content, TimeSpan? timeout = null)
	{
		var serialized = Serialize(content);
		return InternalPost(uri, serialized, "application/json", timeout);
	}

	private HttpResponseMessage InternalPost(string uri, string serialized, string mediaType, TimeSpan? timeout = null)
	{
		using var objectContent = new StringContent(serialized, Encoding.UTF8, mediaType);
		var response = _httpClient
			.PostAsync(uri, objectContent)
			.AwaitResults(timeout ?? Timeout);

		return ProcessResponse(response);
	}

	private HttpResponseMessage InternalPut<T>(string uri, T content, TimeSpan? timeout = null)
	{
		var serialized = Serialize(content);
		return InternalPut(uri, serialized, "application/json", timeout);
	}

	private HttpResponseMessage InternalPut(string uri, string content, string mediaType, TimeSpan? timeout = null)
	{
		using var objectContent = new StringContent(content, Encoding.UTF8, mediaType);
		var response = _httpClient
			.PutAsync(uri, objectContent)
			.AwaitResults(timeout ?? Timeout);

		return ProcessResponse(response);
	}

	private void UpdateCredentials()
	{
		if (_httpClient == null)
		{
			return;
		}

		_httpClient.DefaultRequestHeaders.Authorization = Credential?.GetAuthenticationHeaderValue();
	}

	#endregion
}

/// <summary>
/// Represents a web client contract.
/// </summary>
public interface IWebClient : IDisposable, ICloneable
{
	#region Properties

	/// <summary>
	/// Gets the base URI for connecting.
	/// </summary>
	Uri BaseUri { get; set; }

	/// <summary>
	/// The credentials for the connection.
	/// </summary>
	Credential Credential { get; set; }

	/// <summary>
	/// Headers for this client.
	/// </summary>
	HttpHeaders Headers { get; }

	/// <summary>
	/// The IP address for the base URI.
	/// </summary>
	string IpAddress { get; }

	/// <summary>
	/// Gets or sets an optional proxy for the connection.
	/// </summary>
	IWebProxy Proxy { get; set; }

	/// <summary>
	/// Gets or sets the number of milliseconds to wait before the request times out. The default value is 100 seconds.
	/// </summary>
	TimeSpan Timeout { get; set; }

	/// <summary>
	/// Use raw json with out tracking references.
	/// </summary>
	bool UseRawJson { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Delete request
	/// </summary>
	/// <param name="uri"> The URI to use. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The response from the server. </returns>
	HttpResponseMessage Delete(string uri, TimeSpan? timeout = null);

	/// <summary>
	/// Gets a response and deserialize it.
	/// </summary>
	/// <param name="uri"> The URI of the content to deserialize. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The response from the server. </returns>
	HttpResponseMessage Get(string uri, TimeSpan? timeout = null);

	/// <summary>
	/// Gets a response and deserialize it.
	/// </summary>
	/// <typeparam name="T"> The type to deserialize into. </typeparam>
	/// <param name="uri"> The URI of the content to deserialize. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The deserialized type. </returns>
	T Get<T>(string uri, TimeSpan? timeout = null);

	/// <summary>
	/// Initialize the web client.
	/// </summary>
	void Initialize();

	/// <summary>
	/// Patch an item on the server with the provide content.
	/// </summary>
	/// <param name="uri"> The URI to patch to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The response from the server. </returns>
	HttpResponseMessage Patch(string uri, string content, TimeSpan? timeout = null);

	/// <summary>
	/// Patch an item on the server with the provide content.
	/// </summary>
	/// <typeparam name="TContent"> The type to update with. </typeparam>
	/// <param name="uri"> The URI to patch to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The response from the server. </returns>
	HttpResponseMessage Patch<TContent>(string uri, TContent content, TimeSpan? timeout = null);

	/// <summary>
	/// Patch an item on the server with the provide content.
	/// </summary>
	/// <typeparam name="TContent"> The type to update with. </typeparam>
	/// <typeparam name="TResult"> The type to respond with. </typeparam>
	/// <param name="uri"> The URI to patch to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The response from the server. </returns>
	TResult Patch<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null);

	/// <summary>
	/// Post an item on the server with the provide content.
	/// </summary>
	/// <param name="uri"> The URI to post to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The response from the server. </returns>
	HttpResponseMessage Post(string uri, string content, TimeSpan? timeout = null);

	/// <summary>
	/// Post an item on the server with the provide content.
	/// </summary>
	/// <typeparam name="TContent"> The type to update with. </typeparam>
	/// <param name="uri"> The URI to post to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The response from the server. </returns>
	HttpResponseMessage Post<TContent>(string uri, TContent content, TimeSpan? timeout = null);

	/// <summary>
	/// Post an item on the server with the provide content.
	/// </summary>
	/// <typeparam name="TContent"> The type to update with. </typeparam>
	/// <typeparam name="TResult"> The type to respond with. </typeparam>
	/// <param name="uri"> The URI to post to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The server result. </returns>
	TResult Post<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null);

	/// <summary>
	/// Put (update) an item on the server with the provide content.
	/// </summary>
	/// <param name="uri"> The URI to post to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The response from the server. </returns>
	HttpResponseMessage Put(string uri, string content, TimeSpan? timeout = null);

	/// <summary>
	/// Put (update) an item on the server with the provide content.
	/// </summary>
	/// <typeparam name="TContent"> The type to update with. </typeparam>
	/// <param name="uri"> The URI to post to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The response from the server. </returns>
	HttpResponseMessage Put<TContent>(string uri, TContent content, TimeSpan? timeout = null);

	/// <summary>
	/// Put an item on the server with the provide content.
	/// </summary>
	/// <param name="uri"> The URI to put to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <returns> The response from the server. </returns>
	TResult Put<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null);

	/// <summary>
	/// Reset the web client.
	/// </summary>
	void Reset();

	#endregion
}