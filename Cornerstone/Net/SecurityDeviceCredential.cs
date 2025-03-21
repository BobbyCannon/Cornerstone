#region References

using System;
using System.Net.Http.Headers;
using System.Security;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Net;

/// <summary>
/// Represents a security card credential.
/// </summary>
public class SecurityDeviceCredential : Credential
{
	#region Constants

	public const string AuthenticationScheme = "SecurityCard";

	#endregion

	#region Constructors

	/// <summary>
	/// Creates an instance of the credential.
	/// </summary>
	public SecurityDeviceCredential() : this(string.Empty, [])
	{
	}

	/// <summary>
	/// Creates an instance of the credential.
	/// </summary>
	/// <param name="username"> The username of the credential. </param>
	/// <param name="password"> The password of the credential. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public SecurityDeviceCredential(string username, byte[] password, IDispatcher dispatcher = null)
		: base(username, password.ToHexString(), dispatcher)
	{
	}

	#endregion

	#region Methods

	/// <summary>
	/// Gets the credential from an authentication header value.
	/// </summary>
	public static SecurityDeviceCredential FromAuthenticationHeaderValue(AuthenticationHeaderValue headerValue)
	{
		if (!string.Equals(headerValue.Scheme, AuthenticationScheme, StringComparison.OrdinalIgnoreCase))
		{
			throw new SecurityException("The authentication header is incorrect schema.");
		}

		var response = new SecurityDeviceCredential();
		response.Load(headerValue);
		return response;
	}

	/// <inheritdoc />
	public override AuthenticationHeaderValue GetAuthenticationHeaderValue()
	{
		var value = string.Join(":", UserName, Password);
		return new AuthenticationHeaderValue(AuthenticationScheme, value);
	}

	/// <inheritdoc />
	public override void Load(AuthenticationHeaderValue value)
	{
		var values = value?.Parameter?.Split(':') ?? [];
		UserName = values.GetArrayValueOrDefault(0, string.Empty);
		Password = values.GetArrayValueOrDefault(1, string.Empty);
	}

	#endregion
}