#region References

using System;
using System.Net;
using System.Net.Http;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Exceptions;

/// <summary>
/// Represents a web client exception.
/// </summary>
[Serializable]
public class WebClientException : CornerstoneException
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public WebClientException()
	{
		Code = HttpStatusCode.OK;
	}

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public WebClientException(HttpResponseMessage result)
		: this(result.StatusCode, result.Content.ReadAsStringAsync().AwaitResults())
	{
	}

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public WebClientException(HttpStatusCode code, string message) : base(message)
	{
		Code = code;
	}

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public WebClientException(HttpStatusCode code, string message, Exception inner) : base(message, inner)
	{
		Code = code;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The HTTP status code for this exception.
	/// </summary>
	public HttpStatusCode Code { get; }

	#endregion
}