#region References

using System;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Data;
using Cornerstone.Presentation;
#if !BROWSER
using System.Linq;
using System.Net;
#endif

#endregion

namespace Cornerstone.Web;

/// <summary>
/// This class is used for making GET and POST calls to an HTTP endpoint.
/// </summary>
public partial class WebClient : Bindable, IWebClient
{
	#region Fields

	private Credential _credential;
	private HttpClientHandler _handler;
	private HttpClient _httpClient;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new HTTP helper to point at a specific URI, and with the specified session identifier.
	/// </summary>
	/// <param name="baseUri"> The base URI. </param>
	/// <param name="timeout"> The timeout in milliseconds. </param>
	/// <param name="credential"> The optional credential to authenticate with. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public WebClient(string baseUri, int timeout = 1000, Credential credential = null, IDispatcher dispatcher = null)
		: this(new Uri(baseUri), TimeSpan.FromMilliseconds(timeout), credential, dispatcher)
	{
	}

	/// <summary>
	/// Initializes a new HTTP helper to point at a specific URI, and with the specified session identifier.
	/// </summary>
	/// <param name="baseUri"> The base URI. </param>
	/// <param name="timeout"> The timeout in milliseconds. </param>
	/// <param name="credential"> The optional credential to authenticate with. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public WebClient(Uri baseUri, TimeSpan timeout, Credential credential = null, IDispatcher dispatcher = null) : base(dispatcher)
	{
		_handler = new HttpClientHandler();
		_httpClient = new HttpClient(_handler) { BaseAddress = baseUri };

		BaseUri = baseUri;
		Credential = credential;
		Timeout = timeout;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the base URI for connecting.
	/// </summary>
	public Uri BaseUri
	{
		get => _httpClient.BaseAddress;
		set
		{
			// Check to see if the URI has changed
			if (value == _httpClient.BaseAddress)
			{
				// Nope, so just bounce
				return;
			}

			var timeout = _httpClient.Timeout;
			var credential = Credential;

			try
			{
				_httpClient?.Dispose();
				_handler?.Dispose();
			}
			catch
			{
				// We ignore the disposing...
			}

			_handler = new HttpClientHandler();
			_httpClient = new HttpClient(_handler) { BaseAddress = value };

			Timeout = timeout;
			Credential = credential;

			OnPropertyChanged();
		}
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

			OnPropertyChanged();
		}
	}

	/// <summary>
	/// Headers for this client.
	/// </summary>
	public HttpHeaders Headers => _httpClient.DefaultRequestHeaders;

	[Notify]
	public partial string IpAddress { get; private set; }

	/// <summary>
	/// Options for serialization.
	/// </summary>
	public JsonSerializerOptions SerializerOptions { get; set; }

	/// <summary>
	/// Gets or sets the number of milliseconds to wait before the request times out. The default value is 100 seconds.
	/// </summary>
	public TimeSpan Timeout
	{
		get => _httpClient.Timeout;
		set
		{
			_httpClient.Timeout = value;
			OnPropertyChanged();
		}
	}

	#endregion

	#region Methods

	public virtual WebClient DeepClone(int? maxDepth = null, IncludeExcludeSettings settings = null)
	{
		return ShallowClone(settings);
	}

	public HttpResponseMessage Delete(string uri, TimeSpan? timeout = null)
	{
		return InternalDeleteAsync(uri)
			.WaitAsync(timeout ?? Timeout, CancellationToken.None)
			.GetAwaiter()
			.GetResult();
	}

	public Task<HttpResponseMessage> DeleteAsync(string uri, CancellationToken cancellationToken = default)
	{
		return InternalDeleteAsync(uri, cancellationToken);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public virtual HttpResponseMessage Get(string uri, TimeSpan? timeout = null)
	{
		return InternalGetAsync(uri)
			.WaitAsync(timeout ?? Timeout, CancellationToken.None)
			.GetAwaiter()
			.GetResult();
	}

	public virtual T Get<T>(string uri, TimeSpan? timeout = null)
	{
		using var response = Get(uri, timeout);
		return DeserializeAsync<T>(response)
			.WaitAsync(timeout ?? Timeout, CancellationToken.None)
			.GetAwaiter()
			.GetResult();
	}

	public Task<HttpResponseMessage> GetAsync(string uri, CancellationToken cancellationToken = default)
	{
		return InternalGetAsync(uri, cancellationToken);
	}

	public async Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken = default)
	{
		using var result = await InternalGetAsync(uri, cancellationToken);
		return await DeserializeAsync<T>(result);
	}

	public virtual void Initialize()
	{
		OnPropertyChanged(nameof(BaseUri));
	}

	public HttpResponseMessage Patch(string uri, string content, TimeSpan? timeout = null)
	{
		return InternalPatchAsync(uri, content)
			.WaitAsync(timeout ?? Timeout, CancellationToken.None)
			.GetAwaiter()
			.GetResult();
	}

	public virtual HttpResponseMessage Patch<TContent>(string uri, TContent content, TimeSpan? timeout = null)
	{
		return InternalPatchAsync(uri, content)
			.WaitAsync(timeout ?? Timeout, CancellationToken.None)
			.GetAwaiter()
			.GetResult();
	}

	public TResult Patch<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null)
	{
		using var response = InternalPatchAsync(uri, content)
			.WaitAsync(timeout ?? Timeout, CancellationToken.None)
			.GetAwaiter()
			.GetResult();
		return DeserializeAsync<TResult>(response)
			.WaitAsync(timeout ?? Timeout, CancellationToken.None)
			.GetAwaiter()
			.GetResult();
	}

	public Task<HttpResponseMessage> PatchAsync(string uri, string content, CancellationToken cancellationToken = default)
	{
		return InternalPatchAsync(uri, content, cancellationToken);
	}

	public Task<HttpResponseMessage> PatchAsync<TContent>(string uri, TContent content, CancellationToken cancellationToken = default)
	{
		return InternalPatchAsync(uri, content, cancellationToken);
	}

	public async Task<TResult> PatchAsync<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
	{
		using var response = await InternalPatchAsync(uri, content, cancellationToken);
		return await DeserializeAsync<TResult>(response);
	}

	public virtual HttpResponseMessage Post(string uri, string content, TimeSpan? timeout = null)
	{
		return InternalPostAsync(uri, content)
			.WaitAsync(timeout ?? Timeout, CancellationToken.None)
			.GetAwaiter()
			.GetResult();
	}

	public virtual HttpResponseMessage Post<TContent>(string uri, TContent content, TimeSpan? timeout = null)
	{
		return InternalPostAsync(uri, content)
			.WaitAsync(timeout ?? Timeout, CancellationToken.None)
			.GetAwaiter()
			.GetResult();
	}

	public virtual TResult Post<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null)
	{
		using var response = InternalPostAsync(uri, content)
			.WaitAsync(timeout ?? Timeout, CancellationToken.None)
			.GetAwaiter()
			.GetResult();
		return DeserializeAsync<TResult>(response)
			.WaitAsync(timeout ?? Timeout, CancellationToken.None)
			.GetAwaiter()
			.GetResult();
	}

	public Task<HttpResponseMessage> PostAsync(string uri, string content, CancellationToken cancellationToken = default)
	{
		return InternalPostAsync(uri, content, cancellationToken);
	}

	public Task<HttpResponseMessage> PostAsync<TContent>(string uri, TContent content, CancellationToken cancellationToken = default)
	{
		return InternalPostAsync(uri, content, cancellationToken);
	}

	public async Task<TResult> PostAsync<TContent, TResult>(string uri, TContent content, CancellationToken cancellationToken = default)
	{
		using var response = await InternalPostAsync(uri, content, cancellationToken);
		return await DeserializeAsync<TResult>(response);
	}

	public HttpResponseMessage Put(string uri, string content, TimeSpan? timeout = null)
	{
		return InternalPutAsync(uri, content)
			.WaitAsync(timeout ?? Timeout, CancellationToken.None)
			.GetAwaiter()
			.GetResult();
	}

	public virtual HttpResponseMessage Put<TContent>(string uri, TContent content, TimeSpan? timeout = null)
	{
		return InternalPutAsync(uri, content)
			.WaitAsync(timeout ?? Timeout, CancellationToken.None)
			.GetAwaiter()
			.GetResult();
	}

	public virtual TResult Put<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null)
	{
		using var response = InternalPutAsync(uri, content)
			.WaitAsync(timeout ?? Timeout, CancellationToken.None)
			.GetAwaiter()
			.GetResult();
		return DeserializeAsync<TResult>(response)
			.WaitAsync(timeout ?? Timeout, CancellationToken.None)
			.GetAwaiter()
			.GetResult();
	}

	public Task<HttpResponseMessage> PutAsync(string uri, string content, CancellationToken cancellationToken = default)
	{
		return InternalPutAsync(uri, content, cancellationToken);
	}

	public Task<HttpResponseMessage> PutAsync<TContent>(string uri, TContent content, CancellationToken cancellationToken = default)
	{
		return InternalPutAsync(uri, content, cancellationToken);
	}

	public async Task<TResult> PutAsync<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
	{
		using var response = await InternalPutAsync(uri, content, cancellationToken);
		return await DeserializeAsync<TResult>(response);
	}

	public virtual void RefreshHeaders()
	{
	}

	public virtual void Reset()
	{
	}

	public virtual WebClient ShallowClone(IncludeExcludeSettings settings = null)
	{
		return new WebClient(BaseUri, Timeout, Credential, GetDispatcher());
	}

	public virtual void Uninitialize()
	{
	}

	/// <summary>
	/// Deserialize the response.
	/// </summary>
	/// <typeparam name="T"> The type to deserialize into. </typeparam>
	/// <param name="result"> The result to deserialize. </param>
	/// <returns> The deserialized type. </returns>
	protected virtual async Task<T> DeserializeAsync<T>(HttpResponseMessage result)
	{
		var content = await result.Content.ReadAsStringAsync();
		var response = JsonSerializer.Deserialize<T>(content, SerializerOptions);
		return response;
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> Should be true if managed resources should be disposed. </param>
	protected virtual void Dispose(bool disposing)
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

	protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		switch (propertyName)
		{
			case nameof(BaseUri):
			{
				#if !BROWSER
				try
				{
					IpAddress = Dns
							.GetHostAddressesAsync(BaseUri.DnsSafeHost)
							.WaitAsync(Timeout, CancellationToken.None)
							.GetAwaiter()
							.GetResult()
							.FirstOrDefault()?
							.ToString()
						?? "0.0.0.0";
				}
				catch (Exception e)
				{
					Console.WriteLine("Error: " + e.Message);
				}
				#endif

				RefreshHeaders();
				break;
			}
			case nameof(Credential):
			{
				UpdateCredentials();
				break;
			}
		}

		base.OnPropertyChanged(propertyName);
	}

	protected virtual HttpResponseMessage ProcessResponse(HttpResponseMessage response)
	{
		if (!response.IsSuccessStatusCode)
		{
			throw new WebClientException(response.StatusCode, response.ReasonPhrase);
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
		return JsonSerializer.Serialize(content, SerializerOptions);
	}

	private void CredentialOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		UpdateCredentials();
	}

	private async Task<HttpResponseMessage> InternalDeleteAsync(string uri, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.DeleteAsync(uri, cancellationToken);
		return ProcessResponse(response);
	}

	private async Task<HttpResponseMessage> InternalGetAsync(string uri, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync(uri, cancellationToken);
		return ProcessResponse(response);
	}

	private Task<HttpResponseMessage> InternalPatchAsync<T>(string uri, T content, CancellationToken cancellationToken = default)
	{
		var serialized = Serialize(content);
		return InternalPatchAsync(uri, serialized, "application/json-patch+json", cancellationToken);
	}

	private async Task<HttpResponseMessage> InternalPatchAsync(string uri, string serialized, string mediaType, CancellationToken cancellationToken = default)
	{
		using var objectContent = new StringContent(serialized, Encoding.UTF8, mediaType);
		var method = new HttpMethod("PATCH");
		var request = new HttpRequestMessage(method, uri) { Content = objectContent };
		var response = await _httpClient.SendAsync(request, cancellationToken);

		return ProcessResponse(response);
	}

	private Task<HttpResponseMessage> InternalPostAsync<T>(string uri, T content, CancellationToken cancellationToken = default)
	{
		var serialized = Serialize(content);
		return InternalPostAsync(uri, serialized, "application/json", cancellationToken);
	}

	private async Task<HttpResponseMessage> InternalPostAsync(string uri, string serialized, string mediaType, CancellationToken cancellationToken = default)
	{
		using var objectContent = new StringContent(serialized, Encoding.UTF8, mediaType);
		var response = await _httpClient.PostAsync(uri, objectContent, cancellationToken);
		return ProcessResponse(response);
	}

	private Task<HttpResponseMessage> InternalPutAsync<T>(string uri, T content, CancellationToken cancellationToken = default)
	{
		var serialized = Serialize(content);
		return InternalPutAsync(uri, serialized, "application/json", cancellationToken);
	}

	private async Task<HttpResponseMessage> InternalPutAsync(string uri, string content, string mediaType, CancellationToken cancellationToken = default)
	{
		using var objectContent = new StringContent(content, Encoding.UTF8, mediaType);
		var response = await _httpClient.PutAsync(uri, objectContent, cancellationToken);
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
public interface IWebClient : IDisposable, INotifyPropertyChanged
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
	/// Options for serialization.
	/// </summary>
	JsonSerializerOptions SerializerOptions { get; set; }

	/// <summary>
	/// Gets or sets the number of milliseconds to wait before the request times out. The default value is 100 seconds.
	/// </summary>
	TimeSpan Timeout { get; set; }

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
	/// Delete request
	/// </summary>
	/// <param name="uri"> The URI to use. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the request. </param>
	/// <returns> The response from the server. </returns>
	Task<HttpResponseMessage> DeleteAsync(string uri, CancellationToken cancellationToken = default);

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
	/// Gets a response and deserialize it.
	/// </summary>
	/// <param name="uri"> The URI of the content to deserialize. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the request. </param>
	/// <returns> The response from the server. </returns>
	Task<HttpResponseMessage> GetAsync(string uri, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a response and deserialize it.
	/// </summary>
	/// <typeparam name="T"> The type to deserialize into. </typeparam>
	/// <param name="uri"> The URI of the content to deserialize. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the request. </param>
	/// <returns> The deserialized type. </returns>
	Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken = default);

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
	/// Patch an item on the server with the provide content.
	/// </summary>
	/// <param name="uri"> The URI to patch to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the request. </param>
	/// <returns> The response from the server. </returns>
	Task<HttpResponseMessage> PatchAsync(string uri, string content, CancellationToken cancellationToken = default);

	/// <summary>
	/// Patch an item on the server with the provide content.
	/// </summary>
	/// <typeparam name="TContent"> The type to update with. </typeparam>
	/// <param name="uri"> The URI to patch to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the request. </param>
	/// <returns> The response from the server. </returns>
	Task<HttpResponseMessage> PatchAsync<TContent>(string uri, TContent content, CancellationToken cancellationToken = default);

	/// <summary>
	/// Patch an item on the server with the provide content.
	/// </summary>
	/// <typeparam name="TContent"> The type to update with. </typeparam>
	/// <typeparam name="TResult"> The type to respond with. </typeparam>
	/// <param name="uri"> The URI to patch to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the request. </param>
	/// <returns> The response from the server. </returns>
	Task<TResult> PatchAsync<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

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
	/// Post an item on the server with the provide content.
	/// </summary>
	/// <param name="uri"> The URI to post to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the request. </param>
	/// <returns> The response from the server. </returns>
	Task<HttpResponseMessage> PostAsync(string uri, string content, CancellationToken cancellationToken = default);

	/// <summary>
	/// Post an item on the server with the provide content.
	/// </summary>
	/// <typeparam name="TContent"> The type to update with. </typeparam>
	/// <param name="uri"> The URI to post to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the request. </param>
	/// <returns> The response from the server. </returns>
	Task<HttpResponseMessage> PostAsync<TContent>(string uri, TContent content, CancellationToken cancellationToken = default);

	/// <summary>
	/// Post an item on the server with the provide content.
	/// </summary>
	/// <typeparam name="TContent"> The type to update with. </typeparam>
	/// <typeparam name="TResult"> The type to respond with. </typeparam>
	/// <param name="uri"> The URI to post to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the request. </param>
	/// <returns> The server result. </returns>
	Task<TResult> PostAsync<TContent, TResult>(string uri, TContent content, CancellationToken cancellationToken = default);

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
	/// Put (update) an item on the server with the provide content.
	/// </summary>
	/// <param name="uri"> The URI to post to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the request. </param>
	/// <returns> The response from the server. </returns>
	Task<HttpResponseMessage> PutAsync(string uri, string content, CancellationToken cancellationToken = default);

	/// <summary>
	/// Put (update) an item on the server with the provide content.
	/// </summary>
	/// <typeparam name="TContent"> The type to update with. </typeparam>
	/// <param name="uri"> The URI to post to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the request. </param>
	/// <returns> The response from the server. </returns>
	Task<HttpResponseMessage> PutAsync<TContent>(string uri, TContent content, CancellationToken cancellationToken = default);

	/// <summary>
	/// Put an item on the server with the provide content.
	/// </summary>
	/// <param name="uri"> The URI to put to. </param>
	/// <param name="content"> The content to update with. </param>
	/// <param name="timeout"> An optional timeout to override the default Timeout value. </param>
	/// <param name="cancellationToken"> A cancellation token that can be used to cancel the request. </param>
	/// <returns> The response from the server. </returns>
	Task<TResult> PutAsync<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Refresh the headers.
	/// </summary>
	void RefreshHeaders();

	/// <summary>
	/// Reset the web client.
	/// </summary>
	void Reset();

	/// <summary>
	/// Uninitialize the web client.
	/// </summary>
	void Uninitialize();

	#endregion
}