#region References

using System;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Net;

/// <summary>
/// Represents a bearer credential for a client.
/// </summary>
public class TokenCredential : Credential
{
	#region Constructors

	/// <summary>
	/// Creates an instance of the credential.
	/// </summary>
	public TokenCredential() : this(string.Empty)
	{
	}

	/// <summary>
	/// Creates an instance of the credential.
	/// </summary>
	/// <param name="password"> The token of the credential. </param>
	public TokenCredential(string password)
		: this(password?.ToSecureString())
	{
	}

	/// <summary>
	/// Creates an instance of the credential.
	/// </summary>
	/// <param name="password"> The token of the credential. </param>
	public TokenCredential(SecureString password)
		: base(string.Empty, password)
	{
	}

	#endregion

	#region Methods

	/// <summary>
	/// Gets the credential from an authentication header value.
	/// </summary>
	public static TokenCredential FromAuthenticationHeaderValue(AuthenticationHeaderValue headerValue)
	{
		if (!string.Equals(headerValue.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
		{
			throw new SecurityException("The authentication header is incorrect schema.");
		}

		var response = new TokenCredential();
		response.Load(headerValue);
		return response;
	}

	/// <summary>
	/// Gets the credential as an authentication header value.
	/// </summary>
	public override AuthenticationHeaderValue GetAuthenticationHeaderValue()
	{
		return new AuthenticationHeaderValue("Bearer", System.Convert.ToBase64String(Encoding.UTF8.GetBytes(Password)));
	}

	/// <summary>
	/// Determines if the credentials have been provided. Token credential only requires a password (aka token).
	/// </summary>
	/// <returns> Returns true if the Password is not null or whitespace. </returns>
	public override bool HasCredentials()
	{
		return SecurePassword is { Length: > 0 };
	}

	/// <inheritdoc />
	public override void Load(AuthenticationHeaderValue value)
	{
		Password = value?.Parameter.FromBase64String();
	}

	#endregion
}